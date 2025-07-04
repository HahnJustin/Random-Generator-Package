using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RandomWalkGenerator : AbstractGenerator<RandomWalkConfig>
    {
        public RandomWalkGenerator(RandomWalkConfig config) : base(config) { }

        public class Walker
        {
            public int x;
            public int y;

            public static TileGrid TileGrid { get; set; }

            public Walker(int startX, int startY)
            {
                x = startX;
                y = startY;
            }

            public Vector2Int GetPosition()
            {
                Vector2Int posPt = new Vector2Int(x, y);
                return posPt;
            }

            public int Step(int rnd)
            {
                int choice = rnd;
                if (choice == 0 && x < TileGrid.width - 1)
                {
                    x++;
                }
                else if (choice == 1 && x > 0)
                {
                    x--;
                }
                else if (choice == 2 && y < TileGrid.height - 1)
                {
                    y++;
                }
                else if (choice == 3 && y > 0) 
                {
                    y--;
                }

                return choice;
            }
        }

        protected override Generation Enact(Generation input)
        {
            Walker.TileGrid = TileGrid;

            Vector2 origin = new(width / 2, height / 2);
            if (config.OriginRange > 0) 
            {
                origin += random.InsideUnitCircle()* config.OriginRange;
            }
            Vector2Int intOrigin = Vector2Int.RoundToInt(origin);

            Walker drunkGuy = new(intOrigin.x, intOrigin.y);
            TileGrid.SetTileId(intOrigin, (int)config.Path);

            if (config.DebugEnds) TileGrid.SetTileId(drunkGuy.GetPosition(), (int)TileType.Debug_Star_Green);

            for (int step = 0; step < config.Steps; step++)
            {
                int value = random.NextInt(0, 4);
                drunkGuy.Step(value);
                TileGrid.SetTileId(drunkGuy.GetPosition(), (int)config.Path);
                CancelCheck();
            }

            if (config.DebugEnds) TileGrid.SetTileId(drunkGuy.GetPosition(), (int)TileType.Debug_Star_Red);

            return input;
        }
    }
}
