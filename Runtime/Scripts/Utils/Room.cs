using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Utils
{
    public class Room : IComparable, IEnumerable<Tile>
    {
        private List<Tile> tiles = new();
        private List<Tile> edges = new();

        private Dictionary<Vector2Int, Tile> tileDictionary = new();

        private Tile top = null;
        private Tile bottom = null;
        private Tile right = null;
        private Tile left = null;

        public int Width
        {
            get
            {
                BoundsInt bounds = GetBounds();
                return bounds.xMax - bounds.x;
            }
        }
        public int Height
        {
            get
            {
                BoundsInt bounds = GetBounds();
                return bounds.yMax - bounds.y;
            }
        }

        public int Value { get; private set; }

        public int Count { get { return tiles.Count; } }

        public Room(int roomNumber)
        {
            Value = roomNumber;
        }

        public void AddTile(Tile tile)
        {
            Vector2Int position = new Vector2Int(tile.x, tile.y);
            if (tileDictionary.ContainsKey(position))
            {
                return;
            }
            tiles.Add(tile);
            tileDictionary.Add(position, tile);

            if (top == null || top.y < tile.y) top = tile;
            if (bottom == null || bottom.y > tile.y) bottom = tile;
            if (right == null || right.x < tile.x) right = tile;
            if (left == null || left.x > tile.x) left = tile;
        }

        //Could definitely have issue with using bounds related to rooms wrapping around a grid
        public BoundsInt GetBounds()
        {
            return new(new Vector3Int(left.x, bottom.y, 0), new Vector3Int(right.x-left.x, top.y-bottom.y, 1));
        }

        public Tile GetFirstTile()
        {
            return tiles[0];
        }

        public Tile GetRandomTile(System.Random random)
        {
            return tiles[random.Next(0, tiles.Count)];
        }

        public bool ContainsTile(Tile tile)
        {
            return tileDictionary.ContainsKey(tile.Vector);
        }

        public void AddEdge(Tile tile)
        {
            edges.Add(tile);
        }

        public void AddEdgeRange(List<Tile> otherEdges)
        {
            edges.AddRange(otherEdges);
        }

        public List<Tile> GetEdges()
        {
            return edges;
        }

        public void AddTileRange(List<Tile> otherTiles)
        {
            tiles.AddRange(otherTiles);
        }

        //Biggest First
        public int CompareTo(object obj)
        {
            Room other = (Room)obj;
            if (tiles.Count < other.tiles.Count) return 1;
            else if (tiles.Count > other.tiles.Count) return -1;
            return 0;
        }

        public IEnumerator<Tile> GetEnumerator()
        {
            return tiles.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}