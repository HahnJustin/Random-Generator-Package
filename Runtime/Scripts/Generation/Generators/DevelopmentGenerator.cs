using System;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class DevelopmentGenerator : OccupanceGenerator
    {
        protected new DevelopmentConfig config;

        private class DLANode
        {
            public Vector2 position;
            public bool stuck = false;
            public float radius;
            public List<DLANode> neighbors;

            public DLANode(Vector2 gridPos, float radius)
            {
                neighbors = new List<DLANode>();
                position = gridPos;
                stuck = false;
                this.radius = radius;
            }

            public void MakeStuck()
            {
                stuck = true;
            }

            public void Walk(TileGrid grid, Vector2 velocity)
            {
                position += velocity;
                position.x = (float)Math.Max(0, Math.Min(grid.width - 1, position.x));
                position.y = (float)Math.Max(0, Math.Min(grid.height - 1, position.y));
            }
        }

        public DevelopmentGenerator(DevelopmentConfig config) : base(config)
        {
            this.config = config;
            //outOfBoundsOccupancy = 0;
        }

        private void ApplyGraphNodeTreeToGrid(List<DLANode> tree)
        {
            if (tree.Count <= 0)
            {
                return;
            }

            foreach (DLANode node in tree)
            {
                FillCircle(node);
            }
        }

        private void FillCircle(DLANode node)
        {
            int raidusRoundedUp = (int)Math.Ceiling(node.radius);
            Vector2 position = node.position;
            for (int x = -raidusRoundedUp; x <= raidusRoundedUp; x++)
            {
                for (int y = -raidusRoundedUp; y <= raidusRoundedUp; y++)
                {
                    Vector2Int finalPositon = new Vector2Int((int)Math.Floor(x + position.x), (int)Math.Floor(y + position.y));

                    if (Math.Pow(x, 2) + Math.Pow(y, 2) <= Math.Pow(node.radius, 2) &&
                        finalPositon.x >= 0 && finalPositon.y >= 0 && finalPositon.x < width && finalPositon.y < height)
                    {
                        Tile tile = TileGrid.GetTile(finalPositon);
                        tile.SetType(config.StickTo);
                    }
                }
            }
        }

        private bool HasCircleNeighbors(DLANode node)
        {
            int iterations = (int)Math.Ceiling(Math.Pow(node.radius, 2));
            float increment = 360f / iterations;

            for (int i = 0; i <= iterations; i++)
            {
                int x = (int)Math.Round(node.position.x + node.radius * Math.Cos(increment * i * Math.PI / 180.0));
                int y = (int)Math.Round(node.position.y + node.radius * Math.Sin(increment * i * Math.PI / 180.0));

                if (GetIfOccupiedTileNextToPosition(x, y))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IfStuckAddToTree(DLANode node, List<DLANode> others)
        {
            for (int i = others.Count - 1; i >= 0; i--)
            {
                DLANode other = others[i];
                float squareDistance = node.position.DistanceSquared(other.position);
                if (squareDistance <= ((node.radius * node.radius) + (other.radius * other.radius) + (2 * other.radius * node.radius)))
                {
                    node.neighbors.Add(others[i]);
                    other.neighbors.Add(node);
                    node.stuck = true;
                    return true;
                }
            }

            if (HasCircleNeighbors(node)) return true;

            return false;
        }

        private Vector2Int GetRandomEdgePoint()
        {
            int value = random.NextInt(4);

            if (value == 0) return new Vector2Int(width, random.NextInt(0, height));
            if (value == 1) return new Vector2Int(0, random.NextInt(0, height));
            if (value == 2) return new Vector2Int(random.NextInt(0, width), height);
            else return new Vector2Int(random.NextInt(0, width), 0);
        }

        protected override void Enact()
        {
            // Setting all of the graphnode values
            float radius = config.Radius;

            List<DLANode> tree = new();
            List<DLANode> walkers = new();

            if (config.HasHeadAtCenter)
            {
                DLANode headWalker = new(new Vector2(width / 2f, height / 2f), radius);
                headWalker.MakeStuck();
                tree.Add(headWalker);
            }

            radius *= config.Shrink;

            for (int i = 0; i < config.MaxWalkers; i++)
            {
                walkers.Add(new(GetRandomEdgePoint(), radius));
                radius *= config.Shrink;
            }

            for (var n = 0; n < config.Iterations; n++)
            {
                for (var i = walkers.Count - 1; i >= 0; i--)
                {
                    Vector2 randomVelocity = random.InsideUnitCircle() * config.Speed;
                    walkers[i].Walk(TileGrid, randomVelocity);
                    if (IfStuckAddToTree(walkers[i], tree))
                    {
                        tree.Add(walkers[i]);
                        walkers.RemoveAt(i);
                    }
                    CancelCheck();
                }
            }

            while (walkers.Count < config.MaxWalkers && radius > 1)
            {
                walkers.Add(new(GetRandomEdgePoint(), radius));
                radius *= config.Shrink;
            }

            ApplyGraphNodeTreeToGrid(tree);
        }
    }
}
