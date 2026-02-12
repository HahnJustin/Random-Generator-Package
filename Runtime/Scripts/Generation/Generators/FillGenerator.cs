using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Utils;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Diagnostics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class FillGenerator : AbstractGenerator<FillConfig>
    {
        public FillGenerator(FillConfig config) : base(config) { }

        protected override Generation Enact(Generation input)
        {
            NativeTileGrid readGrid = TileGrid.CloneNativeGrid();
            AddDisposable(readGrid);

            NativeTileGrid nativeGrid = TileGrid.GetNative();

            NativeArray<int> tileIds = new (config.Tiles.Count, Allocator.Persistent);
            for (int i = 0; i < config.Tiles.Count; i++) tileIds[i] = config.Tiles[i];

            AddDisposable(tileIds);

            var job = new FillJob
            {
                readGrid = readGrid,
                hotMetaIndex = TileGrid.GetMetaIndex(config.MetaKey),
                useMetaKey = config.UseMetaProbability,
                writeGrid = nativeGrid,
                fillIds = tileIds
            };

            JobHandle handle = job.Schedule(nativeGrid.width * nativeGrid.height, 64);
            handle.Complete();

            Dispose();
            return input;
        }
    }
}
