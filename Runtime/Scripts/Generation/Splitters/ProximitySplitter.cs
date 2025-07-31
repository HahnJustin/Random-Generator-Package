using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Data;
using System.Collections.Generic;
using Dalichrome.RandomGenerator.Core;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class ProxiomitySplitter : AbstractSplitter<ProximitySplitterConfig>
    {
        private readonly static int MAX_RADIUS_CAP = 100;

        private OccupanceUtil util;
        public ProxiomitySplitter(ProximitySplitterConfig config, int outputs) : base(config, outputs)
        {
            this.config = config;
            util = new(config);
            AddUtil(util);
        }

        protected override RegionSplits Split(Generation generation)
        {
            RegionSplits regionSplits = new RegionSplits(generation);
            List<int2> tilePositions = new();

            // Find all tiles matching configured ids
            foreach (Tile tile in generation.Grid)
            {
                foreach (int id in config.Tiles) {
                    if (tile.ContainsId(id))
                    {
                        tilePositions.Add(tile.Int2);
                        break;
                    }
                }
            }

            foreach (int2 pos in tilePositions)
            {
                int radius = random.NextInt(
                    Mathf.Max(1,config.MinimumRadiusSize),
                    Mathf.Min(config.MaximumRadiusSize, MAX_RADIUS_CAP));
                int radiusSquared = radius * radius;
                List<int2> positions = new();

                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        int2 offset = new int2(x, y);
                        if (math.lengthsq(offset) <= radiusSquared &&
                            !generation.Grid.IsRestricted(pos + offset) &&
                            (!config.UseOccupance || util.IsOccupied(pos + offset) == 0))
                        {
                            positions.Add(pos + offset);
                        }
                    }
                }
                RegionBounds bounds = new(positions, TileGrid);
                regionSplits.AddRegion(bounds);
            }

            return regionSplits;
        }
    }
}
