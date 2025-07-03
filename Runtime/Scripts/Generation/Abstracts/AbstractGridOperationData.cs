using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Threading;
using UnityEngine;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator
{
    public class AbstractGridOperationData : AbstractOperationData, IDisposable
    {
        public int Width { get { return Grid.width; } }

        public int Height { get { return Grid.height; } }

        private TileGrid grid;
        public TileGrid Grid
        {
            get
            {
                return grid;
            }

            set
            {
                if (grid != null && grid != value && grid.IsValid)
                {
                    grid.Dispose();
                }
                grid = value;
            }
        }

        public AbstractGridOperationData()
        {
        }

        public AbstractGridOperationData(AbstractGridOperationData data)
        {
            Grid = data.Grid;
            Seed = data.Seed;
        }

        public void Dispose()
        {
            if (grid != null && grid.IsValid)
            {
                grid.Dispose();
            }
        }

        public void AddLayersLookups(Dictionary<int, LayerType> layerLookup)
        {
            Grid.AddLayersLookups(layerLookup);
        }
    }
}