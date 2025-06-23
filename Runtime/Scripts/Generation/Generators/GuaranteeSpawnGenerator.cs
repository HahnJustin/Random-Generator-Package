using System.Collections.Generic;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Generators;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine;
using System.Threading.Tasks;
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
        }

        protected override void Enact()
        {
            // Return if there are no tiles configured
            if (config.TileWeights.Count <= 0) return;

            NativeList<int2> candidates;

            // Using Entrance Distance
            if (config.UseEntranceDistance)
            {

                Vector2Int position = TileGrid.GetNearestPosition(TileGrid.Center, (int)TileType.Object_Entrance);
                Vector2Int entranceAir = TileGrid.GetNearestPosition(position, (int)TileType.Wall_Object_NA);

                if (position == Constants.OutsideGridVectorInt || entranceAir == Constants.OutsideGridVectorInt) return;

                Room room = new(1);
                util.RoomCreate(TileGrid, entranceAir.x, entranceAir.y, room, true, -1);
                List<Tile> tileList = room.ToList();
                tileList.Shuffle(random);

                // Build NativeList of candidates
                candidates = new(tileList.Count, Allocator.Persistent);
                foreach (var tile in tileList)
                    candidates.Add(tile.Int2);
            }
            // Guaranteed spawn for unoccupied every tile
            else
            {
                List<int2> roomTilePositions = new ();
                util.RoomList.ForEach(room => roomTilePositions.AddRange(room.Int2TilesList));
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
            TileGridData inputGrid = TileGridData.DeepClone(TileGrid.GetGridData());
            AddDisposable(inputGrid);

            var job = new GuaranteeSpawnJob
            {
                candidateTiles = candidates.AsArray(),
                inputGrid = inputGrid,
                outputGrid = TileGrid.GetGridData(),
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
        }
    }
}