using System.Collections.Generic;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using System.Linq;

namespace Dalichrome.RandomGenerator.Generators
{
    public class GuaranteeSpawnGenerator : AbstractGenerator<GuaranteeSpawnConfig>
    {
        private RoomUtil util;
        public GuaranteeSpawnGenerator(GuaranteeSpawnConfig config) : base(config)
        {
            this.config = config;
            util = new(config);
            AddUtil(util);
            util.DoInitialization = false;
        }

        protected override Generation Enact(Generation input)
        {
            // Return if there are no tiles configured
            if (config.TileWeights.Count <= 0) return input;

            NativeList<int2> candidates;

            // Using Entrance Distance
            if (config.UseEntranceDistance)
            {
                int2 entrance = TileGrid.GetNearestPosition(TileGrid.Center, (int)TileDefaults.Object_Entrance);

                if (math.all(entrance == Constants.OutsideGridInt2))
                    return input;

                Room room = new(1);
                util.RoomCreate(TileGrid, entrance.x, entrance.y, room, true, -1);
                List<int2> positions = room.ToList();
                positions.Shuffle(random);

                // Build NativeList of candidates
                candidates = new(positions.Count, Allocator.Persistent);
                foreach (int2 pos in positions)
                    candidates.Add(pos);
            }
            // Guaranteed spawn for unoccupied every tile
            else
            {
                List<int2> roomTilePositions = new ();
                util.RoomList.ForEach(room => roomTilePositions.AddRange(room.Tiles));
                roomTilePositions.Shuffle(random);

                candidates = new(roomTilePositions.Count, Allocator.Persistent);
                foreach (int2 pos in roomTilePositions)
                    candidates.Add(pos);
            }
            AddDisposable(candidates);

            // Create counter
            NativeReference<int> spawnCounter = new (0, Allocator.Persistent);
            AddDisposable(spawnCounter);

            // Max Spawns and Universal Mask Exclusion List
            int maxSpawns = random.NextInt(config.MinimumAmount, config.MaximumAmount);
            var tempExcludes = new NativeList<int2>(maxSpawns, Allocator.Persistent);
            AddDisposable(tempExcludes);

            // Tile Id List
            var pairsNative = new NativeArray<int2>(
            config.TileWeights.Count, Allocator.Persistent);
            for (int i = 0; i < pairsNative.Length; ++i)
            {
                var p = config.TileWeights[i];
                pairsNative[i] = new int2(p.Key, p.Value);   // x = id, y = weight
            }
            AddDisposable(pairsNative);

            // Create Grid to Read From
            NativeTileGrid inputGrid = NativeTileGrid.DeepClone(TileGrid.GetNative());
            AddDisposable(inputGrid);

            var job = new GuaranteeSpawnJob
            {
                candidateTiles = candidates.AsArray(),
                inputGrid = inputGrid,
                outputGrid = TileGrid.GetNative(),
                outputExcludes = tempExcludes,
                maxSpawns = maxSpawns,
                minDistance = config.MinimumDistanceFromEntrance,
                updateMask = config.AddSpawnsToMask,
                useEntranceDistance = config.UseEntranceDistance,
                tilePairs = pairsNative,
                seed = random.NextUInt()
            };

            JobHandle handle = job.Schedule();
            handle.Complete();
            
            // Update Excluded Positions / Universal Mask 
            foreach (var pos in tempExcludes)
                TileGrid.AddExcludedPosition(pos);

            Dispose();
            return input;
        }
    }
}