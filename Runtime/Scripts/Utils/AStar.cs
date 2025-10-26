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
        private HashSet<int2> closedSet;
        private OpenHeap open;

        // Tiny PQ for int2 with external fScore
        private class OpenHeap
        {
            private readonly List<int2> data = new();
            private readonly AStar astar;

            public OpenHeap(AStar a) { astar = a; }
            public int Count => data.Count;

            private float F(int2 p) => astar.GetFScore(p);

            public void Push(int2 v)
            {
                data.Add(v);
                int i = data.Count - 1;
                while (i > 0)
                {
                    int parent = (i - 1) >> 1;
                    if (F(data[i]) >= F(data[parent])) break;
                    (data[i], data[parent]) = (data[parent], data[i]);
                    i = parent;
                }
            }

            public int2 Pop()
            {
                var root = data[0];
                var last = data[^1];
                data.RemoveAt(data.Count - 1);
                if (data.Count == 0) return root;
                data[0] = last;
                int i = 0;
                while (true)
                {
                    int left = (i << 1) + 1, right = left + 1, smallest = i;
                    if (left < data.Count && F(data[left]) < F(data[smallest])) smallest = left;
                    if (right < data.Count && F(data[right]) < F(data[smallest])) smallest = right;
                    if (smallest == i) break;
                    (data[i], data[smallest]) = (data[smallest], data[i]);
                    i = smallest;
                }
                return root;
            }
        }

        private bool Search(int2 position)
        {
            while (open.Count > 0)
            {
                position = open.Pop();
                if (closedSet.Contains(position)) continue; // skip stale duplicates
                if (math.all(position == destination)) return true;

                closedSet.Add(position);
                foreach (var adj in GetAdjacentLocations(position))
                {
                    if (closedSet.Contains(adj)) continue;

                    float gTemp = GetGScore(position) + 1f;
                    if (gTemp < GetGScore(adj))
                    {
                        SetGScore(adj, gTemp);
                        SetFScore(adj, gTemp + GetHScore(adj));
                        cameFrom[adj] = position;
                        open.Push(adj);
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
            return math.distance((float2)position, (float2)destination);
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
            gScore[start] = 0f;

            fScore = new();
            fScore[start] = GetHScore(start);

            closedSet = new HashSet<int2>();
            open = new OpenHeap(this);
            open.Push(start);
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
