using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Utils
{
    public abstract class AbstractUtil
    {
        protected AbstractConfig config;
        protected TileGrid tileGrid;

        protected int width;
        protected int height;

        protected AbstractUtil(AbstractConfig config)
        {
            this.config = config;
        }

        public void SetTileGrid(TileGrid tileGrid)
        {
            this.tileGrid = tileGrid;
            width = tileGrid.width;
            height = tileGrid.height;
        }
    }
}