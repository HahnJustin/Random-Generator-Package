using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractGridOperation
    {
        protected CancellationToken token;
        protected List<AbstractUtil> utils = new();

        private TileGrid tileGrid;

        protected int width;
        protected int height;
        protected AbstractConfig config;

        protected AbstractGridOperation(AbstractConfig config)
        {
            this.config = config;
        }

        protected void SetTileGrid(TileGrid tileGrid)
        {
            this.tileGrid = tileGrid;
            width = tileGrid.width;
            height = tileGrid.height;
        }

        protected TileGrid GetTileGrid()
        {
            return tileGrid;
        }

        protected void SetUtilsTileGrid()
        {
            foreach (AbstractUtil util in utils)
            {
                util.SetTileGrid(tileGrid);
            }
        }

        protected void InitializeUtils()
        {
            foreach (AbstractUtil util in utils)
            {
                if (util is IInitializableUtil initializable) initializable.Initialize();
            }
        }

        protected void CancelCheck()
        {
            token.ThrowIfCancellationRequested();
        }

        protected void AddUtil(AbstractUtil util)
        {
            utils.Add(util);
        }
    }
}