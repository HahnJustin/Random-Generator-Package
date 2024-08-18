using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Generators;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Databases;
using System.Linq;
using System.Collections.Concurrent;
using UnityEngine.Tilemaps;
using System.Threading.Tasks;

namespace Dalichrome.RandomGenerator
{
    public class GenerationManager : MonoBehaviour, ITileInfoGrabber, ILayerInfoGrabber
    {
        [SerializeField] private GenerationParams generationParameters;

        [SerializeField] private TilemapCreator tilemapCreator;

        [SerializeField] private TileInfoDatabase tileDatabase;
        [SerializeField] private LayerDatabase layerDatabase;
        [SerializeField] private NumberSpriteDatabase numberSpriteDatabase;

        [SerializeField] private bool generateOnStart = true;

        private TileInfoGrabber tileGrabber = new();
        private LayerInfoGrabber layerGrabber = new();

        private List<AbstractGeneratorConfig> lastGeneratedConfigs;
        private List<AbstractGeneratorConfig> generatingConfigs;
        private BlockingCollection<AbstractGeneratorConfig> blockingConfigs;

        private GenerationEvents events = new();

        private CancellationTokenSource manualCancellationSource;

        private GenerationInfo lastGeneration = new();

        private static int seedsGenerated = 0;

        //Static Properties
        public static GenerationEvents LastEvents
        {
            get { return Last.events; }
        }

        public static TileGrid LastGrid
        {
            get { return Last.lastGeneration.Grid; }
        }

        public static int LastWidth
        {
            get { return Last.generationParameters.Width; }
        }

        public static int LastHeight
        {
            get { return Last.generationParameters.Height; }
        }

        public static List<AbstractGeneratorConfig> LastConfigs
        {
            get { return Last.generationParameters.Configs; }
        }

        public static GenerationInfo LastGeneration
        {
            get { return Last.lastGeneration; }
        }

        public static uint Seed
        {
            get { return Last.generationParameters.Seed; }
        }

        //Properties
        public GenerationEvents Events
        {
            get { return events; }
        }

        public TileGrid Grid
        {
            get { return lastGeneration.Grid; }
        }

        public int Width
        {
            get { return generationParameters.Width; }
        }

        public int Height
        {
            get { return generationParameters.Height; }
        }

        public List<AbstractGeneratorConfig> Configs
        {
            get { return generationParameters.Configs; }
        }

        public static GenerationManager Last
        {
            get { return last; }
        }
        private static GenerationManager last = null;

        private static uint GetRandomSeed()
        {
            Interlocked.Increment(ref seedsGenerated);
            uint value = ThreadSafeRandom.NextUInt();
            return value;
        }

        private void Awake()
        {
            last = this;
            if (tilemapCreator != null) tilemapCreator.SetRandomGenerator(this);
            ThreadSafeRandom.InitState();

            tileGrabber.SetDatabase(tileDatabase, numberSpriteDatabase);
            layerGrabber.SetDatabase(layerDatabase);
        }

        private void Start()
        {
            if (generateOnStart) GenerateAsync();
        }

        private async void Generate(CancellationToken token)
        {
            SetGeneratingConfigs();

            GenerationInfo generationInfo = CreateGenerationInfo(token);
            events.RaiseGenerationStart(generationInfo);

            //Background Thread Generating the TileGrid and Calculating Time
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            //Await thread syncing on each strategy config, this is done to allow UI like the loader to function
            int count = 0;
            foreach (AbstractGeneratorConfig config in generatingConfigs)
            {
                if (config == null || config.Type == GeneratorType.NA || !config.Enabled) continue;

                count += 1;
                events.RaiseConfigGenerated(config, count / (float)generationParameters.Configs.Count);

                Func<GenerationInfo> func = () =>
                {
                    AbstractGenerator strategy = GeneratorTypeConversions.GetGeneratorFromConfig(config);
                    return strategy.Do(generationInfo);
                };
                Task<GenerationInfo> task = Task.Run(func, token);

                try
                {
                    await task;
                    generationInfo = task.Result;
                }
                catch (OperationCanceledException exception)
                {
                    events.RaiseGenerationCancel();
                    Debug.Log("Generation Got Cancelled!" + exception.ToString());
                    return;
                }
                catch (Exception exception)
                {
                    events.RaiseGenerationError(exception.ToString());
                    return;
                }
            }

            watch.Stop();

            generationInfo.OverallOperationMilliseconds = watch.ElapsedMilliseconds;
            lastGeneration = generationInfo;
            lastGeneratedConfigs = generatingConfigs.DeepClone();
            CheckUngeneratedChanges();

            if (tilemapCreator != null)
            {
                tilemapCreator.SetTileGrid(lastGeneration.Grid);
            }
            events.RaiseGenerationEnd(lastGeneration);
        }

        private GenerationInfo CreateGenerationInfo(CancellationToken token)
        {
            GenerationInfo generationInfo = new(generationParameters);
            generationInfo.Token = token;
            return generationInfo;
        }

        private GenerationInfo CreateGenerationInfo(CancellationToken token, uint seed)
        {
            GenerationInfo generationInfo = CreateGenerationInfo(token);
            generationInfo.Seed = seed;
            return generationInfo;
        }

        public Vector2Int GetGridDimension()
        {
            return new Vector2Int(Width, Height);
        }

        public void SetEvents(GenerationEvents genEvents)
        {
            events = genEvents;
        }

        public void SetParams(GenerationParams genParams)
        {
            this.generationParameters = genParams;
            lastGeneratedConfigs = Configs.DeepClone();

            CheckUngeneratedChanges();
        }

        public GenerationParams GetParams()
        {
            return generationParameters;
        }

        public void SetGeneratingConfigs()
        {
            generatingConfigs = generationParameters.Configs.DeepClone();
            blockingConfigs = new(new ConcurrentQueue<AbstractGeneratorConfig>(generatingConfigs));
        }

        public bool CannotGenerate()
        {
            return generationParameters.Configs == null || generationParameters.Configs.Count == 0 || LastWidth == 0 || LastHeight == 0;
        }

        //Make clear this version lacks callbacks
        public GenerationInfo GenerateThreadSafe(CancellationToken token = default)
        {
            if (CannotGenerate()) return null;
            last = this;

            uint seed = 0;
            if (!generationParameters.IsSeeded || generationParameters.Seed == 0)
            {
                seed = GetRandomSeed();
            }

            GenerationInfo generationInfo = CreateGenerationInfo(token, seed);

            if (generatingConfigs == null || Height == 0 || Width == 0) return generationInfo;

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            foreach (AbstractGeneratorConfig config in blockingConfigs)
            {
                if (config == null || config.Type == GeneratorType.NA || !config.Enabled) continue;

                AbstractGenerator strategy = GeneratorTypeConversions.GetGeneratorFromConfig(config);
                generationInfo = strategy.Do(generationInfo);
            }

            watch.Stop();

            generationInfo.OverallOperationMilliseconds = watch.ElapsedMilliseconds;

            return generationInfo;
        }

        //TODO have callback and return generationInfo also maybe turn into generationResult
        public void GenerateAsync(GenerationParams overrideParams = null)
        {
            if (CannotGenerate()) return;
            last = this;

            if (overrideParams != null) generationParameters = overrideParams;
            if (!generationParameters.IsSeeded || generationParameters.Seed == 0) generationParameters.Seed = GetRandomSeed();

            CancelAsyncGeneration();
            CancellationTokenSource combinationSource = CancellationTokenSource.CreateLinkedTokenSource(manualCancellationSource.Token, Application.exitCancellationToken);

            Generate(combinationSource.Token);
        }

        public void CancelAsyncGeneration()
        {
            if (manualCancellationSource != null)
            {
                manualCancellationSource.Cancel();
            }
            manualCancellationSource = new();
        }

        public void SetGenerationResult(GenerationInfo result)
        {
            generationParameters.Seed = result.Seed;
            lastGeneration = result;
            events.RaiseGenerationEnd(lastGeneration);
        }

        public void SetConfigs(List<AbstractGeneratorConfig> configs)
        {
            generationParameters.Configs = configs.DeepClone();
            lastGeneratedConfigs = configs.DeepClone();

            CheckUngeneratedChanges();
        }

        public void RevertToLastConfig()
        {
            if (!GetUngeneratedChanges()) return;

            generationParameters.Configs = lastGeneratedConfigs.DeepClone();
            CheckUngeneratedChanges();
        }

        public void AddConfig(GeneratorType type)
        {
            AbstractGeneratorConfig config = GeneratorTypeConversions.GetConfig(type);
            if (config == null) return;

            Configs.Add(config);
            CheckUngeneratedChanges();
        }

        public void RemoveConfig(AbstractGeneratorConfig config)
        {
            Configs.Remove(config);
            CheckUngeneratedChanges();
        }

        public void RemoveAllConfigs()
        {
            Configs.Clear();
            CheckUngeneratedChanges();
        }

        public void MoveConfig(int index, AbstractGeneratorConfig config)
        {
            Configs.Remove(config);
            Configs.Insert(index, config);

            CheckUngeneratedChanges();
        }

        public void CheckUngeneratedChanges()
        {
            events.RaiseUngeneratedChangesCheck(GetUngeneratedChanges());
        }

        public bool GetUngeneratedChanges()
        {
            if (Configs == null || lastGeneratedConfigs == null) return Configs == lastGeneratedConfigs;
            return !lastGeneratedConfigs.SequenceEqual(Configs);
        }

        public GameObject GetGameObject(TileType type)
        {
            return tileGrabber.GetGameObject(type);
        }

        public Sprite GetTileSprite(TileType type)
        {
            return tileGrabber.GetTileSprite(type);
        }

        public Color GetTileColor(TileType type)
        {
            return tileGrabber.GetTileColor(type);
        }

        public TileBase GetTileBase(TileType type)
        {
            return tileGrabber.GetTileBase(type);
        }

        public TileBase GetNumberTileBase(int value)
        {
            return tileGrabber.GetNumberTileBase(value);
        }

        public int GetSortingOrder(LayerType layer)
        {
            return layerGrabber.GetSortingOrder(layer);
        }

        public int GetSortingLayerID(LayerType layer)
        {
            return layerGrabber.GetSortingLayerID(layer);
        }

        public Texture2D CreateTexture(TileGrid grid)
        {
            // Create a new x by y texture ARGB32 (32 bit with alpha) and no mipmaps
            var texture = new Texture2D(grid.width, grid.height, TextureFormat.RGB24, false);

            // set the pixel values
            for (int x = 0; x < grid.width; x++)
            {
                for (int y = 0; y < grid.height; y++)
                {
                    Tile tile = grid.GetTile(x, y);

                    Color color;
                    if (!Enum.GetName(typeof(TileType), tile.Object).Contains("NA"))
                    {
                        color = GetTileColor(tile.Object);
                    }
                    else if (!Enum.GetName(typeof(TileType), tile.Wall).Contains("NA"))
                    {
                        color = GetTileColor(tile.Wall);
                    }
                    else
                    {
                        color = GetTileColor(tile.Ground);
                    }
                    texture.SetPixel(x, y, color);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.Apply();
            return texture;
        }

        public Texture2D CreateTexture()
        {
            return CreateTexture(Grid);
        }
    }
}