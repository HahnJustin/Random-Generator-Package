using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Generators;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Databases;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Nodes;
using Dalichrome.RandomGenerator.UserData;
using Sirenix.OdinInspector;
using System.Security.Cryptography;

namespace Dalichrome.RandomGenerator
{
    public class RandomGenerator : MonoBehaviour
    {
        [SerializeField] private GenerationParams generationParameters;
        [SerializeField] private GeneratorGraph graph;

        [SerializeField] private TilemapCreator tilemapCreator;

        [SerializeField] private TileInfoDatabase tileDatabase;
        [SerializeField] private LayerDatabase layerDatabase;
        [SerializeField] private NumberSpriteDatabase numberSpriteDatabase;

        [SerializeField] private bool generateOnStart = true;

        private TileInfoGrabber tileGrabber = new();
        private LayerInfoGrabber layerGrabber = new();

        private Dictionary<int, LayerType> tileObjectLayerLookup = new();
        private List<int> ids = new();

        private List<AbstractGeneratorConfig> lastGeneratedConfigs;
        private List<AbstractGeneratorConfig> generatingConfigs;
        private BlockingCollection<AbstractGeneratorConfig> blockingConfigs;

        private GenerationEvents events = new();

        private CancellationTokenSource manualCancellationSource;

        private Generation lastGeneration = new();

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

        public static Generation LastGeneration
        {
            get { return Last.lastGeneration; }
        }

        public static uint Seed
        {
            get { return Last.generationParameters.Seed; }
        }

        //Properties
        public TileInfoGrabber TileInfoGrabber
        {
            get { return tileGrabber; }
        }

        public LayerInfoGrabber LayerInfoGrabber
        {
            get { return layerGrabber; }
        }

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

        public static RandomGenerator Last
        {
            get { return last; }
        }
        private static RandomGenerator last = null;

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

            ids.Clear();
            foreach (TileType tile in Enum.GetValues(typeof(TileType)))
            {
                ids.Add((int)tile);
            }

            Dictionary<int, TileObject> tileObjects = new();
            tileObjectLayerLookup.Clear();
            TileObject[] tileObjectArray = Resources.LoadAll<TileObject>("TileObjects/");
            foreach (TileObject tileObject in tileObjectArray)
            {
                tileObjects[tileObject.tileId] = tileObject;
                tileObjectLayerLookup[tileObject.tileId] = tileObject.layer;
                ids.Add(tileObject.tileId);
            }

            tileGrabber.SetDatabase(tileDatabase, numberSpriteDatabase);
            tileGrabber.SetTileObjects(tileObjects);

            layerGrabber.SetDatabase(layerDatabase);
        }

        private void Start()
        {
            if (generateOnStart) GenerateAsync();
        }

        private void OnApplicationQuit()
        {
            Dispose();
        }

        private void Dispose()
        {
            if (lastGeneration != null)
            {
                lastGeneration.Dispose();
            }
        }

        private async void Generate(CancellationToken token)
        {
            SetGeneratingConfigs();

            Generation generation = CreateGeneration(token);
            events.RaiseGenerationStart(generationParameters);

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

                Debug.Log("Generating Config of " + config.Type);
                IGenerator generator = OperationFactory.CreateGenerator(config);
                try
                {
                    await Task.Run(() => generator.Do(generation));
                }
                catch (OperationCanceledException exception)
                {
                    events.RaiseGenerationCancel();
                    Debug.Log("Generation Got Cancelled!" + exception.ToString());
                    generation.Dispose();
                    return;
                }
                catch (Exception exception)
                {
                    events.RaiseGenerationError(exception.ToString());
                    generation.Dispose();
                    return;
                }
            }

            watch.Stop();

            Dispose();
            generation.OverallOperationMilliseconds = watch.ElapsedMilliseconds;
            lastGeneration = generation;
            lastGeneratedConfigs = generatingConfigs.DeepClone();
            CheckUngeneratedChanges();

            if (tilemapCreator != null)
            {
                tilemapCreator.CreateTilemaps(lastGeneration.Grid);
            }
            events.RaiseGenerationEnd(lastGeneration);
        }

        private async void Generate(CancellationToken token, GeneratorGraph generatorGraph)
        {
            Debug.Log("==== Starting Generation via Graph " + generatorGraph.name);
            AbstractGridOperationData data = CreateGeneration(token);
            events.RaiseGenerationStart(generationParameters);

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            ConfigGraphNode current = generatorGraph.ToConfigGraphRoot();
            bool forwards = true;
            var usedSplitters = new HashSet<ISplitter>();

            int count = 0;

            while (current.Role != NodeRole.End)
            {

                // If going forward and node can still operate
                if (!current.Done && forwards && current.Operation != null)
                {
                    try
                    {
                        // Run the node operation
                        if (current.Config != null)
                        {
                            Debug.Log("Generating Config of " + current.Config.ToString());
                            Debug.Log("Inputting Data " + data.ToString());
                            if( data.Grid == null) Debug.Log("Data Grid is null");
                        }
                        events.RaiseConfigGenerated(current.Config, count / (float)generationParameters.Configs.Count);
                        AbstractGridOperationData temp = null;
                        await Task.Run(() => temp = current.Operate(data));

                        // Reverse Traversal Condition - Only happens for undone joiners
                        if (temp == null) forwards = false;
                        else data = temp;

                        if (current.Done)
                            count += 1;

                        // Track used splitters
                        if (current.Operation is ISplitter splitter)
                            usedSplitters.Add(splitter);
                    }
                    catch (OperationCanceledException ex)
                    {
                        events.RaiseGenerationCancel();
                        Debug.Log($"Generation Got Cancelled! {ex}");
                        data?.Dispose();
                        return;
                    }
                    catch (Exception ex)
                    {
                        events.RaiseGenerationError(ex.ToString());
                        data?.Dispose();
                        return;
                    }
                }

                bool moved = false;
                var connectedNodes = forwards ? current.Children : current.Parents;

                // Move inside the graph
                foreach (var node in connectedNodes)
                {
                    if (node.Done && connectedNodes.Count > 1) continue;

                    if (!node.Done && !forwards && node.Role == NodeRole.Splitter)
                        forwards = true;

                    current = node;
                    moved = true;
                    break;
                }

                // Error at graph deadends, as end node should be the only dead end
                if (!moved)
                {
                    events.RaiseGenerationError("XNode Graph is Malformed - Hit an unexpected deadend");
                    data?.Dispose();
                    return;
                }
            }

            // Dispose used splitters since they store native data structs
            foreach (ISplitter splitter in usedSplitters) splitter.ParallelDispose();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[GridOpData #{data._id}] final data");
#endif

            watch.Stop();
            data.OverallOperationMilliseconds = watch.ElapsedMilliseconds;

            Dispose(); // Clears previous generation

            lastGeneration = (Generation)data;
            lastGeneratedConfigs = generatingConfigs.DeepClone();
            CheckUngeneratedChanges();

            tilemapCreator?.CreateTilemaps(lastGeneration.Grid);
            events.RaiseGenerationEnd(lastGeneration);

            Debug.Log("Ended Generation of Graph " + generatorGraph.name);
        }

        private Generation CreateGeneration(CancellationToken token)
        {
            Generation generationInput = new (generationParameters);
            generationInput.Token = token;
            generationInput.AddLayersLookups(tileObjectLayerLookup);
            return generationInput;
        }

        private Generation CreateGeneration(CancellationToken token, uint seed)
        {
            Generation generationInfo = CreateGeneration(token);
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
        public Generation GenerateThreadSafe(CancellationToken token = default, uint seed = 0)
        {
            if (CannotGenerate()) return null;
            last = this;

            if ( seed == 0) seed = generationParameters.Seed;
            if (!generationParameters.IsSeeded || seed == 0)
            {
                seed = GetRandomSeed();
            }

            Generation generationOutput = CreateGeneration(token, seed);

            if (generatingConfigs == null || Height == 0 || Width == 0) return generationOutput;

            try
            {
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                foreach (AbstractGeneratorConfig config in blockingConfigs)
                {
                    if (config == null || config.Type == GeneratorType.NA || !config.Enabled) continue;

                    IGenerator generator = OperationFactory.CreateGenerator(config);
                    generator.Do(generationOutput);
                }

                watch.Stop();

                generationOutput.OverallOperationMilliseconds = watch.ElapsedMilliseconds;

            }
            catch (Exception)
            {
                generationOutput?.Dispose();
            }
            return generationOutput;
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

        [Button]
        public void GenerateGraphAsync()
        {
            last = this;

            if (!generationParameters.IsSeeded || generationParameters.Seed == 0) generationParameters.Seed = GetRandomSeed();

            CancelAsyncGeneration();
            CancellationTokenSource combinationSource = CancellationTokenSource.CreateLinkedTokenSource(manualCancellationSource.Token, Application.exitCancellationToken);

            Generate(combinationSource.Token, graph);
        }

        public void CancelAsyncGeneration()
        {
            if (manualCancellationSource != null)
            {
                manualCancellationSource.Cancel();
            }
            manualCancellationSource = new();
        }

        public void SetGenerationResult(Generation result)
        {
            Dispose();
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

        public List<int> GetTileIds() 
        { 
            return ids; 
        }
    }
}