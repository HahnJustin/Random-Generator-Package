using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    //TODO: Remove this wrapper class of OccupanceUtil, its lazy and leads to confusion
    //      In addition the fact that the implemented interface funcs need to be public is gross
    public abstract class OccupanceGenerator : AbstractGenerator, IOccupanceUtil
    {
        protected new IOccupanceConfig config;
        
        private OccupanceUtil util;

        protected int OutOfBoundsOccupancy { get { return util.OutOfBoundsOccupancy; } set { util.OutOfBoundsOccupancy = value; } }

        protected OccupanceGenerator(IOccupanceConfig config) : base((AbstractGeneratorConfig)config)
        {
            this.config = config;
            util = new OccupanceUtil(config);
            AddUtil(util);
        }

        protected OccupanceUtil GetUtil()
        {
            return util;
        }
        public int IsOccupied(Vector2Int position)
        {
            return util.IsOccupied(position);
        }

        public int IsOccupied(int x, int y)
        {
            return util.IsOccupied(x, y);
        }

        public int IsOccupied(Tile tile)
        {
            return util.IsOccupied(tile);
        }

        public void Fill(Tile tile)
        {
            util.Fill(tile);
        }

        public bool GetIfOccupiedTileNextToPosition(int x, int y, int movement=1)
        {
            return util.GetIfOccupiedTileNextToPosition(x, y, movement);
        }

        public Vector2Int GetPointInDirection(Vector2Int point, Direction direction, int movement=1)
        {
            return util.GetPointInDirection(point, direction, movement);
        }
    }
}
