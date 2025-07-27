using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Random;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator.Generators
{
    public class UpscaleNoiseSplitter : AbstractSplitter<UpscaleNoiseSplitterConfig>
    {
        private const int SCALE_AMOUNT = 2; // Constant upscale factor

        public UpscaleNoiseSplitter(UpscaleNoiseSplitterConfig config, int outputs)
            : base(config, outputs) { }

        // Helpers --------------------------------------------------------------

        private static readonly (int dx, int dy)[] Neighbor9 = {
            ( 0, 0), (-1, 0), (-1,-1), ( 0,-1), ( 1,-1),
            ( 1, 0), ( 1, 1), ( 0, 1), (-1, 1)
        };

        private int Mod(int x, int m) => (x % m + m) % m; // Positive modulo

        private int IsOccupied(int[,] g, int x, int y)
        {
            // Check bounds and optionally wrap coordinates
            if (x < 0 || y < 0 || x >= g.GetLength(0) || y >= g.GetLength(1))
                return config.WrapBounds
                    ? IsOccupied(g, Mod(x, g.GetLength(0)), Mod(y, g.GetLength(1)))
                    : (config.OutOfBoundsOccupied ? 1 : 0);
            return g[x, y];
        }

        private void BuildHistogram(int[,] g, int cx, int cy, Span<int> hist)
        {
            // Zero‑fill span then count 3×3 neighbourhood
            hist.Clear();
            foreach (var (dx, dy) in Neighbor9)
                hist[IsOccupied(g, cx + dx, cy + dy)]++;
        }

        private int WeightedPick(Span<int> w)
        {
            // Roulette‑wheel selection over integer weights
            float total = 0;
            for (int i = 0; i < w.Length; i++) total += w[i];
            if (total == 0) return 0;

            float r = random.NextFloat(total), acc = 0;
            for (int i = 0; i < w.Length; i++)
            {
                acc += w[i];
                if (r < acc) return i;
            }
            return 0;
        }

        private void MajoritySmooth(int[,] g)
        {
            // Replace pixels whose 5+ neighbours share another id
            int w = g.GetLength(0), h = g.GetLength(1);
            var copy = (int[,])g.Clone();
            Span<int> hist = stackalloc int[config.RegionCount + 1];

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    BuildHistogram(copy, x, y, hist);
                    hist[copy[x, y]]--; // Ignore self

                    int bestId = 0, best = 0;
                    for (int id = 0; id < hist.Length; id++)
                        if (hist[id] > best) { best = hist[id]; bestId = id; }

                    if (best >= 5) g[x, y] = bestId;
                }
        }

        // Main splitter --------------------------------------------------------

        protected override RegionSplits Split(Generation gen)
        {
            RegionSplits splits = new RegionSplits(gen); // Create result container

            // Calculate initial seed grid size
            int curW = math.clamp((int)math.floor(width * config.BaseNoiseRatio), 1, width);
            int curH = math.clamp((int)math.floor(height * config.BaseNoiseRatio), 1, height);

            // Seed first grid with random regions
            int[,] grid = new int[curW, curH];
            for (int x = 0, count = 0, occ = 0; x < curW; x++)
                for (int y = 0; y < curH; y++, count++)
                    if (random.NextFloat() < config.Density *
                            (config.ForceDensity && occ > 0 ? count / (float)occ : 1f))
                    {
                        occ++;
                        grid[x, y] = random.NextInt(1, config.RegionCount + 1);
                    }

            // Upscale until final resolution reached
            Span<int> hist = stackalloc int[config.RegionCount + 1];
            int nextW, nextH;

            while (curW < width || curH < height)
            {
                nextW = math.min(curW * SCALE_AMOUNT, width);
                nextH = math.min(curH * SCALE_AMOUNT, height);
                var next = new int[nextW, nextH];

                for (int x = 0; x < nextW; x++)
                    for (int y = 0; y < nextH; y++)
                    {
                        int parentX = (int)math.floor(x * (float)curW / nextW);
                        int parentY = (int)math.floor(y * (float)curH / nextH);
                        int parentId = grid[parentX, parentY];

                        BuildHistogram(grid, parentX, parentY, hist);

                        // Apply mild parent bias using LastGridImpact
                        hist[parentId] = (int)math.round(hist[parentId] * (1f + config.LastGridImpact));

                        next[x, y] = WeightedPick(hist);
                    }

                if (config.MajoritySmooth) MajoritySmooth(next);

                grid = next;
                curW = nextW;
                curH = nextH;

                CancelCheck(); // Check for cancellation request
            }

            // Convert final grid into RegionSplits
            var regions = new List<List<int2>>();
            for (int i = 0; i <= config.RegionCount; i++) regions.Add(new());

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (x >= grid.GetLength(0) || y >= grid.GetLength(1)) continue;
                    int id = grid[x, y];
                    if (id != 0) regions[id - 1].Add(new int2(x, y));
                    TileGrid.SetTileValue(x, y, id); // Write to output tile‑grid
                }

            foreach (var r in regions) splits.AddRegion(new RegionBounds(r));

            return splits;
        }
    }
}
