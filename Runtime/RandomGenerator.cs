using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using Dalichrome.RandomGenerator.Generators;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Databases;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Nodes;

namespace Dalichrome.RandomGenerator
{
    public class RandomGenerator : MonoBehaviour
    {
        [SerializeField] private GenerationParams generationParameters;

        [SerializeField] private TilemapInteractor tilemapInteractor;

        [SerializeField] private NumberSpriteDatabase numberSpriteDatabase;

        [SerializeField] private bool generateOnStart = true;

        [SerializeField] private bool toSerialAfter = true;

        private GeneratorGraph lastGeneratedGraph;

        private GenerationEvents events = new();

        private CancellationTokenSource manualCancellationSource;

        private Generation lastGeneration = new();

        private static int seedsGenerated = 0;

        //Static Properties
        public static GenerationEvents LastEvents
        {
            get { return Last.events; }
        }

        public static GeneratorGraph LastGraph
        {
            get { return Last.Graph; }
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

        public static Generation LastGeneration
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

        public GeneratorGraph Graph
        {
            get { return generationParameters.Graph; }
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
            if (tilemapInteractor != null) tilemapInteractor.SetRandomGenerator(this);
            ThreadSafeRandom.InitState();

            // Change this later - probably remove this with metadata changes
            TileObjectInfo.SetNumberSpriteDatabase(numberSpriteDatabase);
        }

        private void Start()
        {
            if (generateOnStart) Generate();
        }

        public void Dispose()
        {
            if (lastGeneration != null)
            {
                lastGeneration.Dispose();
            }
        }

        public void GenerationCleanup(AbstractGridOperationData data)
        {
            data?.Dispose();
            LookupBundleBuilder.DisposeCachedNativeBundle();
        }

        public static void GenerationCleanup()
        {
            LastGeneration?.Dispose();
            LookupBundleBuilder.DisposeCachedNativeBundle();
        }

        public void Generate()
        {
            if (CannotGenerate()) return;
            last = this;

            CancelAsyncGeneration();
            if (!generationParameters.IsSeeded || generationParameters.Seed == 0) generationParameters.Seed = GetRandomSeed();

            Debug.Log($"==== Starting Generation via Graph '{Graph.name}' and seed {generationParameters.Seed}");
            Generation data = CreateGeneration();
            events.RaiseGenerationStart(generationParameters);

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            Generation result = RunGraphTraversalLoop(
                Graph,
                data,
                (node, input) => Task.FromResult(node.Operate(input))
            );

            watch.Stop();
            FinalizeGeneration(result, watch.ElapsedMilliseconds);
        }

        public void GenerateAsync()
        {
            CancelAsyncGeneration();
            CancellationTokenSource combinationSource = CancellationTokenSource.CreateLinkedTokenSource(manualCancellationSource.Token, Application.exitCancellationToken);

            GenerateAsync(combinationSource.Token);
        }

        private async Task GenerateAsync(CancellationToken token)
        {
            if (CannotGenerate()) return;
            last = this;


            if (!generationParameters.IsSeeded || generationParameters.Seed == 0) generationParameters.Seed = GetRandomSeed();

            Debug.Log($"==== Starting Async Generation via Graph '{Graph.name}' and seed {generationParameters.Seed}");
            AbstractGridOperationData data = CreateGeneration(token);
            events.RaiseGenerationStart(generationParameters);

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            ConfigGraphNode current = Graph.ToConfigGraphRoot();
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
                            if (data.Grid == null) Debug.Log("Data Grid is null");
                        }
                        events.RaiseConfigGenerated(current.Config, count / (float)generationParameters.Graph.GetNodeCount());
                        var node = current;
                        var input = data;
                        AbstractGridOperationData temp = null;
                        temp = await Task.Run(() => node.Operate(input));

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
                        GenerationCleanup(data);
                        return;
                    }
                    catch (Exception ex)
                    {
                        events.RaiseGenerationError(ex.ToString());
                        GenerationCleanup(data);
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
                    GenerationCleanup(data);
                    return;
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[GridOpData #{data._id}] final data");
#endif

            if (!data.Valid || data.Grid == null)
            {
                foreach (var splitter in usedSplitters) splitter.ForceDispose();
                events.RaiseGenerationError("Generator returned a null generation - Check if some chokepoint filter may be failing");
                GenerationCleanup(data);
                return;
            }

            // Dispose used splitters since they store native data structs
            foreach (ISplitter splitter in usedSplitters) splitter.ParallelDispose();

            watch.Stop();
            FinalizeGeneration((Generation)data, watch.ElapsedMilliseconds);
        }

        public void GenerateCoroutine()
        {
            StartCoroutine(DoGenerationCoroutine());
        }

        private IEnumerator DoGenerationCoroutine()
        {
            if (CannotGenerate()) yield break;
            last = this;

            CancelAsyncGeneration();

            if (!generationParameters.IsSeeded || generationParameters.Seed == 0)
                generationParameters.Seed = GetRandomSeed();

            Debug.Log($"==== Starting Coroutine Generation via Graph '{Graph.name}' and seed {generationParameters.Seed}");
            AbstractGridOperationData data = CreateGeneration();
            events.RaiseGenerationStart(generationParameters);

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            GeneratorGraph graph = Graph;
            ConfigGraphNode current = graph.ToConfigGraphRoot();
            bool forwards = true;
            int count = 0;
            var usedSplitters = new HashSet<ISplitter>();
            int total = graph.GetNodeCount();

            while (current.Role != NodeRole.End)
            {
                if (!current.Done && forwards && current.Operation != null)
                {
                    try
                    {
                        if (current.Config != null)
                        {
                            Debug.Log($"[Coroutine] Generating Config of {current.Config}");
                        }

                        events.RaiseConfigGenerated(current.Config, total == 0 ? 0 : count / (float)total);
                        var result = current.Operate(data);

                        if (result == null)
                        {
                            forwards = false;
                        }
                        else
                        {
                            data = result;
                        }

                        if (current.Done) count++;
                        if (current.Operation is ISplitter splitter) usedSplitters.Add(splitter);
                    }
                    catch (Exception ex)
                    {
                        events.RaiseGenerationError($"Coroutine Generation Error: {ex}");
                        data?.Dispose();
                        yield break;
                    }
                }

                bool moved = false;
                var connected = forwards ? current.Children : current.Parents;
                foreach (var node in connected)
                {
                    if (node.Done && connected.Count > 1) continue;
                    if (!node.Done && !forwards && node.Role == NodeRole.Splitter)
                        forwards = true;

                    current = node;
                    moved = true;
                    break;
                }

                if (!moved)
                {
                    events.RaiseGenerationError("XNode Graph is Malformed - Hit an unexpected deadend");
                    data?.Dispose();
                    yield break;
                }

                yield return new WaitForEndOfFrame();
            }

            if (!data.Valid || data.Grid == null)
            {
                foreach (var splitter in usedSplitters) splitter.ForceDispose();
                events.RaiseGenerationError("Generator returned a null generation - Check if some chokepoint filter may be failing");
                GenerationCleanup(data);
                yield break;
            }

            foreach (var splitter in usedSplitters) splitter.ParallelDispose();

            watch.Stop();
            FinalizeGeneration((Generation) data, watch.ElapsedMilliseconds);
        }


        private Generation RunGraphTraversalLoop(
            GeneratorGraph graph,
            AbstractGridOperationData data,
            Func<ConfigGraphNode, AbstractGridOperationData, Task<AbstractGridOperationData>> runOperation)
        {
            var current = graph.ToConfigGraphRoot();
            bool forwards = true;
            var usedSplitters = new HashSet<ISplitter>();
            int count = 0;

            while (current.Role != NodeRole.End)
            {
                if (!current.Done && forwards && current.Operation != null)
                {
                    try
                    {
                        if (current.Config != null)
                        {
                            Debug.Log($"Generating Config of {current.Config}");
                            if (data.Grid == null) Debug.Log("Data Grid is null");
                        }

                        int totalConfigNodes = graph.GetNodeCount();
                        events.RaiseConfigGenerated(current.Config, totalConfigNodes == 0 ? 0 : count / (float)totalConfigNodes);
                        var result = runOperation(current, data).Result;

                        if (result == null) forwards = false;
                        else data = result;

                        if (current.Done) count++;

                        if (current.Operation is ISplitter splitter)
                            usedSplitters.Add(splitter);
                    }
                    catch (Exception ex)
                    {
                        events.RaiseGenerationError(ex.ToString());
                        GenerationCleanup(data);
                        return null;
                    }
                }

                bool moved = false;
                var connected = forwards ? current.Children : current.Parents;
                foreach (var node in connected)
                {
                    if (node.Done && connected.Count > 1) continue;
                    if (!node.Done && !forwards && node.Role == NodeRole.Splitter)
                        forwards = true;

                    current = node;
                    moved = true;
                    break;
                }

                if (!moved)
                {
                    events.RaiseGenerationError("XNode Graph is Malformed - Hit an unexpected deadend");
                    GenerationCleanup(data);
                    return null;
                }
            }

            foreach (ISplitter splitter in usedSplitters)
                splitter.ParallelDispose();

            if (!data.Valid || data.Grid == null)
            {
                events.RaiseGenerationError("Generator returned a null generation");
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[GridOpData #{data._id}] final data");
#endif

            return (Generation)data;
        }

        private void FinalizeGeneration(Generation data, long elapsedMs)
        {
            if (data == null)
            {
                return;
            }

            data.OverallOperationMilliseconds = elapsedMs;
            Dispose(); // dispose previous generation
            lastGeneratedGraph = Graph;
            lastGeneration = data;
            CheckUngeneratedChanges();

            LookupBundleBuilder.DisposeCachedNativeBundle();
            if (toSerialAfter) data.ToSerial();
            tilemapInteractor?.CreateWithTilegrid(data.Grid);
            events.RaiseGenerationEnd(data);

            Debug.Log("Ended Generation of Graph " + Graph.name);
        }

        private Generation CreateGeneration()
        {
            Generation generationInput = generationParameters.ToGeneration(TileLayerInfo.LayerCount);
            generationInput.SetLookupBundle(LookupBundleBuilder.GetNative());
            return generationInput;
        }

        private Generation CreateGeneration(CancellationToken token)
        {
            Generation generationInput = generationParameters.ToGeneration(TileLayerInfo.LayerCount);
            generationInput.Token = token;
            generationInput.SetLookupBundle(LookupBundleBuilder.GetNative());
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
            if( Graph != null) lastGeneratedGraph = Graph;
            generationParameters = (GenerationParams) genParams.Clone();

            CheckUngeneratedChanges();
        }

        public GenerationParams GetParams()
        {
            return generationParameters;
        }

        public bool CannotGenerate()
        {
            string cannotGenerate = "Cannot Generate -";
            if (generationParameters.Graph == null)
            {
                Debug.LogWarning($"{cannotGenerate} The Generator Graph is Null");
                return true;
            }
            else if (generationParameters.Graph.GetNodeCount() == 0)
            {
                Debug.LogWarning($"{cannotGenerate} The Generator Graph contains no nodes");
                return true;
            }
            else if (LastWidth <= 0 || LastHeight <= 0)
            {
                Debug.LogWarning($"{cannotGenerate} Either width or height is set to zero or below");
                return true;
            }

            return false;
        }

        //TODO - This needs a complete overhaul with graphs and etc
        public Generation GenerateThreadSafe(CancellationToken token = default, uint seed = 0)
        {
            if (Width == 0 || Height == 0) return null;

            if (!generationParameters.IsSeeded || seed == 0)
            {
                seed = GetRandomSeed();
            }

            Generation generationOutput = generationParameters.ToGeneration(TileLayerInfo.LayerCount);
            generationOutput.Seed = seed;
            generationOutput.Token = token;
            generationOutput.SetLookupBundle(LookupBundleBuilder.GetNative()); // TODO - Have a feeling this is not thread safe? Even though the struct should be?

            try
            {
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                Generation result = RunGraphTraversalLoop(
                    Graph,
                    generationOutput,
                    (node, input) => Task.FromResult(node.Operate(input)) // force sync
                );

                watch.Stop();

                if (result == null)
                {
                    generationOutput.Dispose();
                    return null;
                }

                generationOutput.OverallOperationMilliseconds = watch.ElapsedMilliseconds;
                return generationOutput;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ThreadSafeGeneration] Error: {ex}");
                generationOutput.Dispose();
                return null;
            }
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

        public void SetGraph(GeneratorGraph inputGraph)
        {
            lastGeneratedGraph = Graph;
            generationParameters.Graph = inputGraph.Clone();
            CheckUngeneratedChanges();
        }

        public void RevertToLastGraph()
        {
            if (!GetUngeneratedChanges()) return;

            generationParameters.Graph = lastGeneratedGraph;
            CheckUngeneratedChanges();
        }

        public void CheckUngeneratedChanges()
        {
            events.RaiseUngeneratedChangesCheck(GetUngeneratedChanges());
        }

        public bool GetUngeneratedChanges()
        {
            return Graph != lastGeneratedGraph;
        }

        public TilemapInteractor GetTilemapInteractor()
        {
            return tilemapInteractor;
        }
    }
}