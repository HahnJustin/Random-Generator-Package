using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Utils
{
    public class DistanceUtil : OccupanceUtil, IInitializableUtil
    {
        protected new IDistanceConfig config;

        private readonly int depthProddableValue;

        public bool DoInitialization { get; set; }

        public DistanceUtil(IDistanceConfig config) : base(config)
        {
            this.config = config;
            depthProddableValue = config.FillOccupied ? 1 : 0;
            DoInitialization = true;
        }

        private void AddRingHelper(List<int2> newRing, int2 pos, int value)
        {
            if (IsOccupied(pos) == depthProddableValue)
            {
                int val = tileGrid.GetTileValue(pos);
                if (val == 0)
                {
                    tileGrid.SetTileValue(pos, value);
                   newRing.Add(pos);
                }
            }
        }

        private bool GetIfTileNextToPositionHelper(int x, int y, int occupiedVal, int movement = 1)
        {
            if (config.Distance == DistanceType.Cardinal)
            {
                return IsOccupied(x - movement, y) == occupiedVal || // Check left
                       IsOccupied(x, y - movement) == occupiedVal || // Check down
                       IsOccupied(x + movement, y) == occupiedVal || // Check right
                       IsOccupied(x, y + movement) == occupiedVal;   // Check up
            }
            return IsOccupied(x - movement, y) == occupiedVal || // Check left
                   IsOccupied(x - movement, y - movement) == occupiedVal || // Check down left
                   IsOccupied(x, y - movement) == occupiedVal || // Check down
                   IsOccupied(x + movement, y - movement) == occupiedVal || // Check down right
                   IsOccupied(x + movement, y) == occupiedVal || // Check right
                   IsOccupied(x + movement, y + movement) == occupiedVal || // Check Up Right
                   IsOccupied(x, y + movement) == occupiedVal || // Check up
                   IsOccupied(x - movement, y + movement) == occupiedVal; //Check up left
        }

        public new bool GetIfOccupiedTileNextToPosition(int2 pos, int movement = 1)
        {
            return GetIfOccupiedTileNextToPosition(pos.x, pos.y, movement);
        }

        public new bool GetIfOccupiedTileNextToPosition(int x, int y, int movement = 1)
        {
            return GetIfTileNextToPositionHelper(x,y, 1, movement);
        }

        public new bool GetIfUnoccupiedTileNextToPosition(int2 pos, int movement = 1)
        {
            return GetIfUnoccupiedTileNextToPosition(pos.x, pos.y, movement);
        }

        public new bool GetIfUnoccupiedTileNextToPosition(int x, int y, int movement = 1)
        {
            return GetIfTileNextToPositionHelper(x, y, 0, movement);
        }

        public void CreateDistanceMap()
        {
            tileGrid.ClearNumbers();

            int value = 1;
            List<int2> ring = new();
            foreach (ITileColumn tile in tileGrid)
            {
                //need to have get unoccupiedTileNext func
                if(IsOccupied(tile.Int2) == depthProddableValue && ((GetIfOccupiedTileNextToPosition(tile.Int2) && !config.FillOccupied) ||
                                                              GetIfUnoccupiedTileNextToPosition(tile.Int2) && config.FillOccupied))
                {
                    tileGrid.SetTileValue(tile.Int2, value);
                    ring.Add(tile.Int2);
                }
                else if (IsOccupied(tile.Int2) != depthProddableValue)
                {
                    tileGrid.SetTileValue(tile.Int2, -1);
                }
            }
            
            
            while (ring.Count > 0)
            {
                List<int2> newRing = new();
                value += 1;
                foreach (int2 pos in ring)
                {
                    if (config.Distance == DistanceType.Cardinal)
                    {
                        foreach (int2 pos2 in tileGrid.GetFourNeighborPositions(pos))
                        {
                            AddRingHelper(newRing, pos2, value);
                        }
                    }
                    else
                    {
                        foreach (int2 pos2 in tileGrid.GetEightNeighborPositions(pos))
                        {
                            AddRingHelper(newRing, pos2, value);
                        }
                    }
                }
                if (newRing.Count > 3000000)
                {
                    throw new System.Exception("DistanceUtil is going Infinite - more than 3 million tiles in a ring");
                }
                ring = newRing;
            }
            
        }

        public void Initialize()
        {
            CreateDistanceMap();
        }
    }
}