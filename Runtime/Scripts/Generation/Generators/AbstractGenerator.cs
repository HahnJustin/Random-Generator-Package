using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System.Threading.Tasks;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractGenerator : AbstractGridOperation
    {
        protected TileGrid TileGrid { get { return GetTileGrid(); } set { SetTileGrid(value); } }
        protected AbstractRandom random;

        protected new AbstractGeneratorConfig config;

        protected AbstractGenerator(AbstractGeneratorConfig config) : base(config)
        {
            this.config = config;
        }

        private void ApplyAfterMask(TileGrid finalGrid)
        {
            for(int x = 0; x < TileGrid.width; x++)
            {
                for (int y = 0; y < TileGrid.height; y++)
                {
                    Tile tile = TileGrid.GetTile(x, y);
                    finalGrid.SetTile(x, y, tile);
                }
            }
            TileGrid.Dispose();
        }

        private void InitializingTileGrid(GenerationInfo generationInfo)
        {
            if (!config.Masked) TileGrid = generationInfo.Grid;
            else if (config.MaskTime == MaskTimeType.After) TileGrid = TileGrid.DeepClone(generationInfo.Grid);

            if (config.Masked)
            {
                generationInfo.Grid.CreateMask(config.IncludeList, config.ExcludeList);
                generationInfo.Grid.ToggleMasked(config.Masked);
                if (config.MaskTime == MaskTimeType.During) TileGrid = generationInfo.Grid;
            }

            SetUtilsTileGrid();
        }

        private void SetingGenerationInfoGrid(GenerationInfo generationInfo)
        {
            //Setting Generation Info's final tile readGrid
            if (config.Masked && config.MaskTime == MaskTimeType.After) ApplyAfterMask(generationInfo.Grid);
            else generationInfo.Grid = TileGrid;

            //Removing Mask Variables from TileGrid
            generationInfo.Grid.RemoveMask();
        }

        protected abstract void Enact();

        public async Task<GenerationInfo> Do(GenerationInfo generationInfo)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            random = generationInfo.Random;
            token = generationInfo.Token;

            InitializingTileGrid(generationInfo);
            InitializeUtils();

            Enact();

            SetingGenerationInfoGrid(generationInfo);

            watch.Stop();
            generationInfo.AddOperationTime(watch.ElapsedMilliseconds);

            CancelCheck();

            return generationInfo;
        }
    }
}
