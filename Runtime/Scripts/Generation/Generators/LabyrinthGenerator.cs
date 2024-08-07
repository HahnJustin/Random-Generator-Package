using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;

namespace Dalichrome.RandomGenerator.Generators
{
    public class LabyrinthGenerator : RoomGenerator
    {
        protected new LabyrinthConfig config;
        private readonly MazeUtil util;

        public LabyrinthGenerator(LabyrinthConfig config) : base(config)
        {
            this.config = config;
            OutOfBoundsOccupancy = 1;

            util = new(config);
            util.OutOfBoundsOccupancy = 1;
            AddUtil(util);
        }

        protected override void Enact()
        {
            foreach (Room room in RoomList)
            {
                util.CreateMazeInRoom(room, random);
            }
        }
    }
}
