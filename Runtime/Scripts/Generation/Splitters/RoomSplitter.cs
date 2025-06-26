using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Dalichrome.RandomGenerator.Generators
{
    public class RoomSplitter : AbstractSplitter<RoomSplitterConfig>
    {
        private RoomUtil util;
        public RoomSplitter(RoomSplitterConfig config) : base(config)
        {
            this.config = config;
            util = new(config);
            AddUtil(util);
        }

        protected override void Initialize(Generation generation) { }

        protected override RegionSplits Enact(Generation generation) { return default; }

        protected override void PostEnact(RegionSplits regionSplits) { }
    }
}
