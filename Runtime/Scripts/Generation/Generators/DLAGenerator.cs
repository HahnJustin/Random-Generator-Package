using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class DLAGenerator : OccupanceGenerator
    {
        protected new DLAConfig config;

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
                position.x = Mathf.Clamp(position.x, 0, grid.width - 1);
                position.y = Mathf.Clamp(position.y, 0, grid.height - 1);
            }
        }

        public DLAGenerator(DLAConfig config) : base(config)
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
            int raidusRoundedUp = Mathf.CeilToInt(node.radius);
            Vector2 position = node.position;
            for (int x = -raidusRoundedUp; x <= raidusRoundedUp; x++)
            {
                for (int y = -raidusRoundedUp; y <= raidusRoundedUp; y++)
                {
                    Vector2Int finalPositon = new(Mathf.FloorToInt(x + position.x), Mathf.FloorToInt(y + position.y));

                    if (Mathf.Pow(x, 2) + Mathf.Pow(y, 2) <= Mathf.Pow(node.radius, 2) &&
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
            int iterations = Mathf.CeilToInt(Mathf.Pow(node.radius,2));
            float increment = 360f / iterations;

            for (int i = 0; i <= iterations; i++)
            {
                int x = Mathf.RoundToInt(node.position.x + node.radius * (float) Mathf.Cos(increment * i));
                int y = Mathf.RoundToInt(node.position.y + node.radius * (float) Mathf.Sin(increment * i));

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
                walkers.Add(new(TileGrid.GetRandomEdgePoint(random), radius));
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
                walkers.Add(new(TileGrid.GetRandomEdgePoint(random), radius));
                radius *= config.Shrink;
            }

            ApplyGraphNodeTreeToGrid(tree);
        }
    }
}