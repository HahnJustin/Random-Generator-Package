using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class StructureGenerator : AbstractGenerator<StructureConfig>
    {
        private OccupanceUtil util;

        public StructureGenerator(StructureConfig config) : base(config)
        {
            this.config = config;
            util = new(config);
            AddUtil(util);
        }

        protected override Generation Enact(Generation input)
        {
            StructureTable table = config.StructureTable;
            if (table == null || table.Rows.Length == 0) return input;

            // Pre-compute anchor list (so we can randomize if desired)
            List<int2> anchors = new List<int2>(TileGrid.GetPositions());
            if (config.Anchor == AnchorType.Random) anchors.Shuffle(random);
            else anchors.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            // Decide structure iteration order
            var rows = table.Rows;
            int[] rowOrder= null; 

            bool placed = false;

            if (config.TryAmount == TryAmountType.Once)
            {
                // Pick one structure by weight, then scan anchors until it fits (or give up).
                var chosen = PickWeighted(table, random);
                if (chosen != null)
                    placed = TryPlace(chosen, anchors);
            }
            else // TryAll
            {
                int attempts = 0;
                int maxAttempts = (config.TryAmount == TryAmountType.ToConstant)
                    ? Math.Max(1, config.TryAttempts)
                    : table.Rows.Length; // fix: was table.Length

                // Working set (no replacement)
                var candidates = new List<StructureTable.Row>(table.Rows.Length);
                int total = 0;
                foreach (var row in table.Rows)
                {
                    if (row.Weight <= 0) continue; // skip zero/neg weights
                    candidates.Add(row);
                    total += row.Weight;
                }

                while (attempts++ < maxAttempts && candidates.Count > 0 && total > 0)
                {
                    int idx = PickWeightedIndex(candidates, total, random);
                    var picked = candidates[idx];

                    // Remove the picked one so it won't be reconsidered
                    total -= picked.Weight;
                    candidates.RemoveAt(idx);

                    if (TryPlace(picked.Structure, anchors))
                    {
                        // placed = true; // optional if you track it
                        break;
                    }
                    // else: continue; (we've already removed it, so it won't re-pick)
                }
            }


            // You could expose success/failure into Generation metadata if useful
            return input;
        }

        /* ---------------- helpers ---------------- */

        private bool TryPlace(Structure structure, List<int2> anchors)
        {
            // Scan all anchor candidates (already ordered)
            foreach (var a in anchors)
            {
                if (FitsAt(structure, a))
                {
                    StampAt(structure, a);
                    return true;
                }
            }
            return false;
        }

        // Placement rule: every non-empty structure cell must
        // (a) map inside region, and (b) land on an empty cell in the TileGrid.
        // Adjust emptiness rule to your needs (layer-aware, masks, etc.).
        private bool FitsAt(Structure structure, int2 anchor)
        {
            int W = structure.Width, H = structure.Height;
            int depth = TileGrid.depth;
            // Quick reject: if any corner is out of region bounds, we still need to check per-cell,
            // because region can be concave/holey. So skip bbox-only tests; use per-cell mask.

            for (int sy = 0; sy < H; sy++)
            {
                for (int sx = 0; sx < W; sx++)
                {
                    int2 point = new int2(anchor.x + sx, anchor.y + sy);
                    if (TileGrid.IsRestricted(point)) return false;
                    if (util.IsOccupied(point) == 1) return false;

                    // For flat array: [ (y*W + x) * depth + z ] — but we only need to know if ANY z is non-empty.
                    // Optimization: precompute a 2D "hasAnyTile" bitset in Structure if you want.
                    bool anyFilled = false;
                    int baseIdx = (sy * W + sx) * depth;
                    for (int z = 0; z < depth; z++)
                    {
                        if (structure[baseIdx + z] >= 0) { anyFilled = true; break; }
                    }
                    if (!anyFilled) continue;
                }
            }
            return true;
        }

        private void StampAt(Structure structure, int2 anchor)
        {
            int W = structure.Width, H = structure.Height;
            int depth = TileGrid.depth;

            for (int sy = 0; sy < H; sy++)
            {
                for (int sx = 0; sx < W; sx++)
                {
                    int baseIdx = ((H - sy - 1) * W + sx) * depth;
                    int2 point = new int2(anchor.x + sx, anchor.y + sy);

                    for (int z = 0; z < depth; z++)
                    {
                        int tileId = structure[baseIdx + z];
                        if (tileId < 0) continue; // structure mask in this layer
                        TileGrid.SetTileIdBypassLayer(point, z, tileId);
                    }
                }
            }
        }

        private static int PickWeightedIndex(List<StructureTable.Row> rows, int totalWeight, AbstractRandom rng)
        {
            // assumes totalWeight > 0 and all weights >= 0
            int r = rng.NextInt(totalWeight);
            int acc = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                acc += rows[i].Weight;
                if (r < acc) return i;
            }
            return rows.Count - 1; // fallback
        }

        private static Structure PickWeighted(StructureTable table, AbstractRandom rng) 
        { 
            if (table.TotalWeight <= 0 || table.Rows.Length == 0) return null; 
            int randomVal = rng.NextInt(table.TotalWeight);
            int accumulated = 0; 
            foreach (var row in table.Rows) 
            { 
                accumulated += row.Weight; 
                if (randomVal < accumulated) return row.Structure;
            } 
            return table.Rows[^1].Structure; 
        }
    }
}
