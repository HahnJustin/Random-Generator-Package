using UnityEngine;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RandomWalkGenerator : AbstractGenerator
    {
        protected new RandomWalkConfig config;

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

        public RandomWalkGenerator(RandomWalkConfig config) : base(config)
        {
            this.config = config;
        }

        protected override void Enact()
        {
            Walker.TileGrid = TileGrid;

            Vector2 origin = new(width / 2, height / 2);
            if (config.OriginRange > 0) 
            {
                origin += random.InsideUnitCircle()* config.OriginRange;
            }
            Vector2Int intOrigin = Vector2Int.RoundToInt(origin);

            Walker drunkGuy = new(intOrigin.x, intOrigin.y);
            Tile tile = TileGrid.GetTile(intOrigin);
            tile.SetType(config.Path);

            if (config.DebugEnds) TileGrid.GetTile(drunkGuy.x, drunkGuy.y).SetType(TileType.Debug_Star_Green);

            for (int step = 0; step < config.Steps; step++)
            {
                int value = random.NextInt(0, 4);
                drunkGuy.Step(value);
                TileGrid.GetTile(drunkGuy.x, drunkGuy.y).SetType(config.Path);
                CancelCheck();
            }

            if (config.DebugEnds) TileGrid.GetTile(drunkGuy.x, drunkGuy.y).SetType(TileType.Debug_Star_Red);
        }
    }
}
