using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator.Utils
{
    public class DistanceUtil : OccupanceUtil, IInitializableUtil
    {
        protected new IDistanceConfig config;

        private readonly int depthProddableValue;

        public DistanceUtil(IDistanceConfig config) : base(config)
        {
            this.config = config;
            depthProddableValue = config.FillOccupied ? 1 : 0;
        }

        private void AddRingHelper(List<Tile> newRing, Tile neighbor, int value)
        {
            if (IsOccupied(neighbor) == depthProddableValue && neighbor.Value == 0)
            {
                neighbor.Value = value;
                newRing.Add(neighbor);
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

        public new bool GetIfOccupiedTileNextToPosition(Tile tile, int movement = 1)
        {
            return GetIfOccupiedTileNextToPosition(tile.x, tile.y, movement);
        }

        public new bool GetIfOccupiedTileNextToPosition(int x, int y, int movement = 1)
        {
            return GetIfTileNextToPositionHelper(x,y, 1, movement);
        }

        public new bool GetIfUnoccupiedTileNextToPosition(Tile tile, int movement = 1)
        {
            return GetIfUnoccupiedTileNextToPosition(tile.x, tile.y, movement);
        }

        public new bool GetIfUnoccupiedTileNextToPosition(int x, int y, int movement = 1)
        {
            return GetIfTileNextToPositionHelper(x, y, 0, movement);
        }

        public void CreateDistanceMap()
        {
            tileGrid.ClearNumbers();

            int value = 1;
            List<Tile> ring = new();
            foreach (Tile tile in tileGrid)
            {
                //need to have get unoccupiedTileNext func
                if(IsOccupied(tile) == depthProddableValue && ((GetIfOccupiedTileNextToPosition(tile) && !config.FillOccupied) ||
                                                                GetIfUnoccupiedTileNextToPosition(tile) && config.FillOccupied))
                {
                    tile.Value = value;
                    ring.Add(tile);
                }
                else if (IsOccupied(tile) != depthProddableValue)
                {
                    tile.Value = -1;
                }
            }
            
            
            while (ring.Count > 0)
            {
                List<Tile> newRing = new();
                value += 1;
                foreach (Tile tile in ring)
                {
                    if (config.Distance == DistanceType.Cardinal)
                    {
                        foreach (Tile neighbor in tileGrid.GetFourNeighborTiles(tile))
                        {
                            AddRingHelper(newRing, neighbor, value);
                        }
                    }
                    else
                    {
                        foreach (Tile neighbor in tileGrid.GetEightNeighborTiles(tile))
                        {
                            AddRingHelper(newRing, neighbor, value);
                        }
                    }
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