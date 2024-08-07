using Dalichrome.RandomGenerator.Configs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Utils
{
    public class AStar
    {
        private Vector2Int start;
        private Vector2Int destination;
        private TileGrid tileGrid;
        private OccupanceUtil occupanceUtil;

        private Dictionary<Vector2Int, Vector2Int> cameFrom;
        private Dictionary<Vector2Int, float> gScore;
        private Dictionary<Vector2Int, float> fScore;

        private List<Vector2Int> openSet;

        private class FComparer : IComparer<Vector2Int>
        {
            private readonly AStar astarInstance;
            public FComparer(AStar astarInstance)
            {
                this.astarInstance = astarInstance;
            }

            public int Compare(Vector2Int location1, Vector2Int location2)
            {
                if (astarInstance.GetFScore(location1) == astarInstance.GetFScore(location2))
                    return 0;
                if (astarInstance.GetFScore(location1) < astarInstance.GetFScore(location2))
                    return -1;
                return 1;
            }
        }

        private bool Search(Vector2Int position)
        {

            while (openSet.Count > 0)
            {
                openSet.Sort(new FComparer(this));
                position = openSet[0];
                if (position == destination)
                {
                    return true;
                }

                openSet.Remove(position);
                List<Vector2Int> nextLocations = GetAdjacentLocations(position);
                foreach (Vector2Int adjLocation in nextLocations)
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

        private List<Vector2Int> GetAdjacentLocations(Vector2Int position)
        {
            List<Vector2Int> neighbors = new();

            neighbors.Add(position + Vector2Int.left);
            neighbors.Add(position + Vector2Int.right);
            neighbors.Add(position + Vector2Int.down);
            neighbors.Add(position + Vector2Int.up);

            List<Vector2Int> validNeighbors = new();
            foreach (Vector2Int pos in neighbors)
            {
                if (pos.x < 0 || pos.x >= tileGrid.width || pos.y < 0 || pos.y >= tileGrid.height)
                {
                    continue;
                }
                else if (IsWalkable(tileGrid.GetTile(pos)))
                {
                    validNeighbors.Add(pos);
                }
            }
            return validNeighbors;
        }

        private bool IsWalkable(Tile tile)
        {
            return occupanceUtil.IsOccupied(tile) == 0;
        }

        //Add custom configurable traversability types here
        private float GetTraversalCost(Vector2Int from, Vector2Int to)
        {
            return 1f;
        }

        private float GetHScore(Vector2Int position)
        {
            return Vector2Int.Distance(position, start);
        }

        private float GetGScore(Vector2Int position)
        {
            if (gScore.ContainsKey(position))
                return gScore[position];
            return float.MaxValue;
        }

        private float GetFScore(Vector2Int position)
        {
            if (fScore.ContainsKey(position))
                return fScore[position];
            return float.MaxValue;
        }

        private void SetGScore(Vector2Int position, float score)
        {
            gScore[position] = score;
        }

        private void SetFScore(Vector2Int position, float score)
        {
            fScore[position] = score;
        }

        public List<Vector2Int> FindPath(TileGrid tileGrid, OccupanceUtil occupanceUtil, Vector2Int start, Vector2Int end)
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

            List<Vector2Int> path = new List<Vector2Int>();
            bool success = Search(start);

            //Reverses Path
            if (success)
            {
                Vector2Int position = destination;
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
