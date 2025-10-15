using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Utils
{
    public class AStar
    {
        private int2 start;
        private int2 destination;
        private TileGrid tileGrid;
        private OccupanceUtil occupanceUtil;

        private Dictionary<int2, int2> cameFrom;
        private Dictionary<int2, float> gScore;
        private Dictionary<int2, float> fScore;

        private List<int2> openSet;

        private class FComparer : IComparer<int2>
        {
            private readonly AStar astarInstance;
            public FComparer(AStar astarInstance)
            {
                this.astarInstance = astarInstance;
            }

            public int Compare(int2 location1, int2 location2)
            {
                if (astarInstance.GetFScore(location1) == astarInstance.GetFScore(location2))
                    return 0;
                if (astarInstance.GetFScore(location1) < astarInstance.GetFScore(location2))
                    return -1;
                return 1;
            }
        }

        private bool Search(int2 position)
        {

            while (openSet.Count > 0)
            {
                openSet.Sort(new FComparer(this));
                position = openSet[0];
                if (math.all(position == destination))
                {
                    return true;
                }

                openSet.Remove(position);
                List<int2> nextLocations = GetAdjacentLocations(position);
                foreach (int2 adjLocation in nextLocations)
                {
                    float traversalCost = GetTraversalCost(position, adjLocation);
                    float gTemp = GetGScore(position) + traversalCost;
                    if (gTemp < GetGScore(adjLocation))
                    {
                        SetGScore(adjLocation, gTemp);
                        SetFScore(adjLocation, gTemp + GetHScore(adjLocation));
                        cameFrom[adjLocation] = position;
                        if (!openSet.Contains(adjLocation))
                        {
                            openSet.Add(adjLocation);
                        }
                    }
                }
            }
            return false;
        }

        private List<int2> GetAdjacentLocations(int2 position)
        {
            List<int2> neighbors = tileGrid.GetFourNeighborPositions(position);

            List<int2> validNeighbors = new();
            foreach (int2 pos in neighbors)
            {
                if (IsWalkable(pos.x, pos.y))
                {
                    validNeighbors.Add(pos);
                }
            }
            return validNeighbors;
        }

        private bool IsWalkable(int x, int y)
        {
            return occupanceUtil.IsOccupied(x, y) == 0;
        }

        //Add custom configurable traversability types here
        private float GetTraversalCost(int2 from, int2 to)
        {
            return 1f;
        }

        private float GetHScore(int2 position)
        {
            return math.distance((float2)position, (float2)start);
        }

        private float GetGScore(int2 position)
        {
            if (gScore.ContainsKey(position))
                return gScore[position];
            return float.MaxValue;
        }

        private float GetFScore(int2 position)
        {
            if (fScore.ContainsKey(position))
                return fScore[position];
            return float.MaxValue;
        }

        private void SetGScore(int2 position, float score)
        {
            gScore[position] = score;
        }

        private void SetFScore(int2 position, float score)
        {
            fScore[position] = score;
        }

        public List<int2> FindPath(TileGrid tileGrid, OccupanceUtil occupanceUtil, int2 start, int2 end)
        {
            this.tileGrid = tileGrid;
            this.occupanceUtil = occupanceUtil;
            this.start = start;
            destination = end;

            gScore = new();
            gScore[start] = 0;

            fScore = new();
            fScore[start] = GetHScore(destination);

            openSet = new();
            openSet.Add(start);

            cameFrom = new();

            List<int2> path = new List<int2>();
            bool success = Search(start);

            //Reverses Path
            if (success)
            {
                int2 position = destination;
                while (cameFrom.ContainsKey(position))
                {
                    path.Add(position);
                    position = cameFrom[position];
                }
                path.Reverse();
            }
            return path;
        }
    }
}
