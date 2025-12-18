using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Utils;
using System;
using System.Collections.Generic;
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

            if (config.TryAmount == TryAmountType.Once)
            {
                var chosen = PickWeighted(table, random);
                if (chosen != null)
                    TryPlace(chosen, anchors);
            }
            else // TryAll
            {
                int attempts = 0;
                int maxAttempts = (config.TryAmount == TryAmountType.ToConstant)
                    ? Math.Max(1, config.TryAttempts)
                    : table.Rows.Length;

                var candidates = new List<StructureTable.Row>(table.Rows.Length);
                int total = 0;
                foreach (var row in table.Rows)
                {
                    if (row.Weight <= 0) continue;
                    candidates.Add(row);
                    total += row.Weight;
                }

                while (attempts++ < maxAttempts && candidates.Count > 0 && total > 0)
                {
                    int idx = PickWeightedIndex(candidates, total, random);
                    var picked = candidates[idx];

                    total -= picked.Weight;
                    candidates.RemoveAt(idx);

                    if (TryPlace(picked.Structure, anchors))
                        break;
                }
            }

            return input;
        }

        /* ---------------- helpers ---------------- */

        private struct Transform2D
        {
            public int rot;     // 0, 90, 180, 270 (CCW)
            public bool flipX;  // mirror horizontally AFTER rotation

            public Transform2D(int rot, bool flipX)
            {
                this.rot = rot;
                this.flipX = flipX;
            }

            public int2 Size(int srcW, int srcH)
            {
                // 90/270 swaps W/H
                bool swap = (rot == 90 || rot == 270);
                return swap ? new int2(srcH, srcW) : new int2(srcW, srcH);
            }

            // dst (transformed space) -> src (original structure space)
            public int2 SrcFromDst(int2 dst, int srcW, int srcH)
            {
                // work in rotated space first (unflip -> unrotate)
                int2 rotSize = (rot == 90 || rot == 270) ? new int2(srcH, srcW) : new int2(srcW, srcH);
                int dx = dst.x;
                int dy = dst.y;

                // unflip in rotated space
                if (flipX)
                    dx = (rotSize.x - 1) - dx;

                // inverse rotate (rot is CCW; inverse is CW)
                switch (rot)
                {
                    case 0:
                        return new int2(dx, dy);

                    case 90: // CCW 90; inverse is CW 90
                        // CCW mapping: src->dst : (sx,sy) -> (sy, W-1-sx)
                        // inverse: (dx,dy) -> (sx,sy) = (W-1-dy, dx)
                        return new int2(srcW - 1 - dy, dx);

                    case 180:
                        return new int2(srcW - 1 - dx, srcH - 1 - dy);

                    case 270: // CCW 270; inverse is CW 270 (CCW 90)
                        // CCW 270 mapping: src->dst : (sx,sy) -> (H-1-sy, sx)
                        // inverse: (dx,dy) -> (sx,sy) = (dy, H-1-dx)
                        return new int2(dy, srcH - 1 - dx);

                    default:
                        return new int2(dx, dy);
                }
            }

            // src -> dst (used for metadata/chance positions)
            public int2 DstFromSrc(int2 src, int srcW, int srcH)
            {
                int sx = src.x;
                int sy = src.y;

                int2 rotSize = (rot == 90 || rot == 270) ? new int2(srcH, srcW) : new int2(srcW, srcH);

                int dx, dy;

                // rotate CCW
                switch (rot)
                {
                    case 0:
                        dx = sx; dy = sy;
                        break;

                    case 90:
                        dx = sy;
                        dy = (srcW - 1) - sx;
                        break;

                    case 180:
                        dx = (srcW - 1) - sx;
                        dy = (srcH - 1) - sy;
                        break;

                    case 270:
                        dx = (srcH - 1) - sy;
                        dy = sx;
                        break;

                    default:
                        dx = sx; dy = sy;
                        break;
                }

                // flipX in rotated space
                if (flipX)
                    dx = (rotSize.x - 1) - dx;

                return new int2(dx, dy);
            }
        }

        private bool TryPlace(Structure structure, List<int2> anchors)
        {
            var transforms = BuildTransforms(structure);
            transforms.Shuffle(random); // optional: randomize orientation preference per placement attempt

            foreach (var a in anchors)
            {
                for (int i = 0; i < transforms.Count; i++)
                {
                    var t = transforms[i];
                    if (FitsAt(structure, a, t))
                    {
                        StampAt(structure, a, t);
                        return true;
                    }
                }
            }
            return false;
        }

        private List<Transform2D> BuildTransforms(Structure structure)
        {
            var list = new List<Transform2D>(8)
            {
                new Transform2D(0, false) // identity always
            };

            if (structure.Rotatable)
            {
                list.Add(new Transform2D(90, false));
                list.Add(new Transform2D(180, false));
                list.Add(new Transform2D(270, false));
            }

            if (structure.Flippable)
            {
                list.Add(new Transform2D(0, true));

                if (structure.Rotatable)
                {
                    list.Add(new Transform2D(90, true));
                    list.Add(new Transform2D(180, true));
                    list.Add(new Transform2D(270, true));
                }
            }

            return list;
        }

        private bool FitsAt(Structure structure, int2 anchor, Transform2D t)
        {
            int srcW = structure.Width, srcH = structure.Height;
            int depth = TileGrid.depth;

            int2 size = t.Size(srcW, srcH);
            int W = size.x;
            int H = size.y;

            for (int ty = 0; ty < H; ty++)
            {
                for (int tx = 0; tx < W; tx++)
                {
                    int2 dst = new int2(tx, ty);
                    int2 src = t.SrcFromDst(dst, srcW, srcH);

                    int2 point = new int2(anchor.x + tx, anchor.y + ty);

                    if (TileGrid.IsRestricted(point)) return false;
                    if (util.IsOccupied(point) == 1) return false;

                    bool anyFilled = false;
                    int baseIdx = (src.y * srcW + src.x) * depth;
                    for (int z = 0; z < depth; z++)
                    {
                        if (structure[baseIdx + z] >= 0) { anyFilled = true; break; }
                    }

                    if (!anyFilled) continue;
                }
            }

            return true;
        }

        private void StampAt(Structure structure, int2 anchor, Transform2D t)
        {
            int srcW = structure.Width, srcH = structure.Height;
            int depth = TileGrid.depth;

            int2 size = t.Size(srcW, srcH);
            int W = size.x;
            int H = size.y;

            HashSet<int2> skippedCells = new();

            // CHANCE: decide skips in transformed coordinates
            foreach (MetadataEntry entry in structure.GetMetaEnumerable())
            {
                if (entry.field != MetaKeyType.CHANCE)
                    continue;

                int2 dst = t.DstFromSrc(new int2(entry.x, entry.y), srcW, srcH);

                int chance = entry.value; // 0..100
                int roll = random.NextInt(0, 100);

                if (roll >= chance)
                    skippedCells.Add(dst);
            }

            // Stamp metadata (except CHANCE), applying transform + direction adjustment
            foreach (MetadataEntry entry in structure.GetMetaEnumerable())
            {
                if (entry.field == MetaKeyType.CHANCE)
                    continue;

                int2 dst = t.DstFromSrc(new int2(entry.x, entry.y), srcW, srcH);
                if (skippedCells.Contains(dst))
                    continue;

                var e = entry;
                e.x = dst.x;
                e.y = dst.y;

                if (e.field == MetaKeyType.DIRECTION)
                {
                    e.value = TransformDirection(e.value, t.rot, t.flipX);
                }

                e.Shift(anchor, true);
                TileGrid.AddData(e);
            }

            // Stamp tiles, using dst->src mapping
            for (int ty = 0; ty < H; ty++)
            {
                for (int tx = 0; tx < W; tx++)
                {
                    int2 dst = new int2(tx, ty);
                    if (skippedCells.Contains(dst))
                        continue;

                    int2 src = t.SrcFromDst(dst, srcW, srcH);
                    int baseIdx = (src.y * srcW + src.x) * depth;

                    int2 point = new int2(anchor.x + tx, anchor.y + ty);

                    for (int z = 0; z < depth; z++)
                    {
                        int tileId = structure[baseIdx + z];
                        if (tileId < 0) continue;
                        TileGrid.SetTileIdBypassLayer(point, z, tileId);
                    }
                }
            }
        }

        private static int TransformDirection(int dirDegrees, int rotDegreesCW, bool flipX)
        {
            // Normalize to [0,360)
            int d = dirDegrees % 360;
            if (d < 0) d += 360;

            // Quantize to 0/90/180/270 (round to nearest)
            int q = (int)math.round(d / 90f) & 3; // 0..3
                                                  // q mapping (clockwise): 0=Up, 1=Right, 2=Down, 3=Left

            // Apply CLOCKWISE rotation steps (CW means add in clockwise indexing)
            int steps = ((rotDegreesCW / 90) % 4 + 4) % 4;
            q = (q + steps) & 3;

            // FlipX mirrors Left<->Right; Up/Down unchanged.
            if (flipX)
            {
                // Up(0)->Up, Right(1)->Left(3), Down(2)->Down, Left(3)->Right(1)
                q = q switch
                {
                    1 => 3,
                    3 => 1,
                    _ => q
                };
            }

            return q * 90;
        }

        private static int PickWeightedIndex(List<StructureTable.Row> rows, int totalWeight, AbstractRandom rng)
        {
            int r = rng.NextInt(totalWeight);
            int acc = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                acc += rows[i].Weight;
                if (r < acc) return i;
            }
            return rows.Count - 1;
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
