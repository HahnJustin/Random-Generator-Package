using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;
using System.Threading.Tasks;

namespace Dalichrome.RandomGenerator.Generators
{
    public class LabyrinthGenerator : AbstractGenerator<LabyrinthConfig>
    {
        private readonly MazeUtil util;

        public LabyrinthGenerator(LabyrinthConfig config) : base(config)
        {
            this.config = config;

            util = new(config);
            util.OutOfBoundsOccupancy = 1;
            AddUtil(util);
        }

        protected override Generation Enact(Generation input)
        {
            foreach (Room room in util.RoomList)
            {
                util.CreateMazeInRoom(room, random);
            }
            return input;
        }
    }
}
