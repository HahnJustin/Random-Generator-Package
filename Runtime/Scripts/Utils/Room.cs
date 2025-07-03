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
    public class Room : IComparable, IEnumerable<Tile>
    {
        private List<Tile> tiles = new();
        private List<Tile> edges = new();

        private Dictionary<Vector2Int, Tile> tileDictionary = new();

        private Tile top;
        private Tile bottom;
        private Tile right;
        private Tile left;

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

        public List<int2> Int2TilesList
        {
            get { return tiles.Select(tile => tile.Int2).ToList(); }
        }


        public List<int2> Int2EdgesList
        {
            get { return edges.Select(tile => tile.Int2).ToList(); }
        }

        public int Value { get; private set; }

        public int Count { get { return tiles.Count; } }

        public int2 Minimum { get { return new int2(left.x, bottom.y); } }

        public int2 Maximum { get { return new int2(right.x - left.x, top.y - bottom.y); } }

        public BoundsInt Bounds { get { return new(new Vector3Int(left.x, bottom.y, 0), new Vector3Int(right.x - left.x, top.y - bottom.y, 1)); } } 

        public Room(int roomNumber)
        {
            Value = roomNumber;
        }

        public void AddTile(Tile tile)
        {
            Vector2Int position = new (tile.x, tile.y);
            if (tileDictionary.ContainsKey(position))
            {
                return;
            }
            tiles.Add(tile);
            tileDictionary.Add(position, tile);

            if (!top.IsValid || top.y < tile.y) top = tile;
            if (!bottom.IsValid || bottom.y > tile.y) bottom = tile;
            if (!right.IsValid || right.x < tile.x) right = tile;
            if (!left.IsValid || left.x > tile.x) left = tile;
        }

        public void RemoveTile(Tile tile)
        {
            Vector2Int position = new (tile.x, tile.y);
            if (!tileDictionary.ContainsKey(position))
            {
                return;
            }
            tiles.Remove(tile);
            tileDictionary.Remove(position);

            //TODO: Redo these here :o
            if (!top.IsValid || top.y < tile.y) top = tile;
            if (!bottom.IsValid || bottom.y > tile.y) bottom = tile;
            if (!right.IsValid || right.x < tile.x) right = tile;
            if (!left.IsValid || left.x > tile.x) left = tile;
        }

        public Tile GetFirstTile()
        {
            return tiles[0];
        }

        public Tile GetRandomTile(AbstractRandom random)
        {
            return tiles[random.NextInt(0, tiles.Count)];
        }

        public bool ContainsTile(Tile tile)
        {
            return tileDictionary.ContainsKey(tile.Position);
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

        public RegionBounds ToRegionBounds()
        {
            List<int2> regionPositions = tiles.Select(x => x.Int2).ToList();

            return new(Minimum, Maximum, regionPositions);
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