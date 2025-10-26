using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using Unity.Mathematics;
using System.Linq;

namespace Dalichrome.RandomGenerator.Utils
{
    public class Room : IComparable, IEnumerable<int2>
    {
        private readonly OrderedSet<int2> tiles = new();
        private readonly OrderedSet<int2> edges = new();

        private int2 top = new (-1,-1);
        private int2 bottom = new(-1, -1);
        private int2 right = new(-1, -1);
        private int2 left = new(-1, -1);

        private int2 initial = new(-1, -1);

        public int Width
        {
            get
            {
                return Bounds.xMax - Bounds.x;
            }
        }
        public int Height
        {
            get
            {
                return Bounds.yMax - Bounds.y;
            }
        }

        public IEnumerable<int2> Tiles => tiles;

        public IEnumerable<int2> Edges => edges;

        public int Value { get; private set; }

        public int Count { get { return tiles.Count; } }

        public int2 Minimum { get { return new int2(left.x, bottom.y); } }

        public int2 Maximum { get { return new int2(right.x, top.y); } }

        public BoundsInt Bounds { get { return new(new Vector3Int(left.x, bottom.y, 0), new Vector3Int(right.x - left.x, top.y - bottom.y, 1)); } } 

        public Room(int roomNumber)
        {
            Value = roomNumber;
        }

        private void FarEdgeHelper(int2 pos)
        {
            if (math.all(top == initial) || top.y < pos.y) top = pos;
            if (math.all(top == initial) || bottom.y > pos.y) bottom = pos;
            if (math.all(top == initial) || right.x < pos.x) right = pos;
            if (math.all(top == initial) || left.x > pos.x) left = pos;
        }

        public void AddPosition(int x, int y)
        {
            AddPosition(new int2(x, y));
        }

        public void AddPosition(int2 pos) 
        { 
            if (tiles.Add(pos)) FarEdgeHelper(pos); 
        }

        public void RemovePosition(int2 pos)
        {
            tiles.Remove(pos);

            //TODO: Recompute bounds
            FarEdgeHelper(pos);
        }

        public int2 GetFirstPosition()
        {
            return tiles.First();
        }

        public int2 GetRandomPosition(AbstractRandom random)
        {
            return tiles.ElementAt(random.NextInt(tiles.Count));
        }

        public bool ContainsPosition(int2 position) => tiles.Contains(position);

        public bool ContainsPosition(int x, int y)
        {
            return tiles.Contains(new int2(x,y));
        }

        public void AddEdge(int x, int y)
        {
            edges.Add(new int2(x,y));
        }

        public void AddEdge(int2 position)
        {
            edges.Add(position);
        }

        public void AddEdgeRange(IEnumerable<int2> otherEdges)
        {
            foreach (int2 edge in otherEdges)
                edges.Add(edge);
        }

        public void AddTileRange(List<int2> otherTiles)
        {
            foreach (int2 pos in otherTiles)
                edges.Add(pos);
        }

        //Biggest First
        public int CompareTo(object obj)
        {
            Room other = (Room)obj;
            if (tiles.Count < other.tiles.Count) return 1;
            else if (tiles.Count > other.tiles.Count) return -1;
            return 0;
        }

        public RegionBounds ToRegionBounds(TileGrid grid)
        {
            return new(Minimum, Maximum, tiles.ToList(), grid);
        }

        public IEnumerator<int2> GetEnumerator()
        {
            return tiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}