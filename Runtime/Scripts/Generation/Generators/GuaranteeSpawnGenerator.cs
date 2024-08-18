using System.Collections.Generic;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;
using System.Numerics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class GuaranteeSpawnGenerator : RoomGenerator
    {
        protected new GuaranteeSpawnConfig config;

        public GuaranteeSpawnGenerator(GuaranteeSpawnConfig config) : base(config)
        {
            this.config = config;
        }

        protected override void Enact()
        {
            Vector2Int position = TileGrid.GetNearestPosition(TileGrid.Center, TileType.Object_Entrance);
            Vector2Int entranceAir = TileGrid.GetNearestPosition(position, TileType.Wall_Object_NA);

            if (position == Constants.OutsideGridVectorInt || entranceAir == Constants.OutsideGridVectorInt) return;

            Room room = new(1);

            RoomCreate(TileGrid, entranceAir.x, entranceAir.y, room, true, -1);

            int value = random.NextInt(config.MinimumAmount, config.MaximumAmount);

            while(value > 0 && room.Count > 0) 
            {
                Tile tile = room.GetRandomTile(random);
                if (tile.Value > -config.MinimumSpawnDistance)
                {
                    room.RemoveTile(tile);
                    continue;
                }

                TileType type = config.TileTypes[random.NextInt(0, config.TileTypes.Count)];
                TileGrid.SetTileType(tile.Vector, type);
                room.RemoveTile(tile);

                value -= 1;

                CancelCheck();
            }
        }
    }
}
