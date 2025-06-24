using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractGridOperation<C, D, R> : AbstractOperation<C, D, R> 
        where C : AbstractConfig
        where D : AbstractOperationData
        where R : AbstractOperationData
    {
        private TileGrid tileGrid;
        protected TileGrid TileGrid { get { return GetTileGrid(); } set { SetTileGrid(value); } }

        protected int width;
        protected int height;

        protected AbstractGridOperation(C config) : base(config) { }

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
    }
}