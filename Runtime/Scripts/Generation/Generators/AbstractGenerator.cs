using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractGenerator<C> : AbstractGridOperation<C, Generation, Generation>, IGenerator where C : AbstractGeneratorConfig
    {
        protected AbstractGenerator(C config) : base(config) {}

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

        //TODO: Definitely a trickle down issue of after masking is that this logic doesn't consider disposing yet - with the deep clone
        private void InitializingTileGrid(Generation generation)
        {
            if (!config.Masked) TileGrid = generation.Grid;
            else if (config.MaskTime == MaskTimeType.After) TileGrid = TileGrid.DeepClone(generation.Grid);

            if (config.Masked)
            {
                generation.Grid.CreateMask(config.IncludeList, config.ExcludeList);
                generation.Grid.ToggleMasked(config.Masked);
                if (config.MaskTime == MaskTimeType.During) TileGrid = generation.Grid;
            }
        }

        private void FinalizeOutputGrid(Generation generation)
        {
            //Setting Generation Info's final tile readGrid
            if (config.Masked && config.MaskTime == MaskTimeType.After) ApplyAfterMask(generation.Grid);
            else generation.Grid = TileGrid;

            //Removing Mask Variables from TileGrid
            generation.Grid.RemoveMask();
        }

        protected override bool RunCondition(Generation input)
        {
            return input.Grid != null && input.Valid;
        }

        protected override Generation FailConditionDefault(Generation input)
        {
            return input;
        }

        protected override void Initialize(Generation generation)
        {
            InitializingTileGrid(generation);
            SetUtilsTileGrid();
        }

        protected override void PostEnact(Generation generation)
        {
            FinalizeOutputGrid(generation);
        }
    }
}
