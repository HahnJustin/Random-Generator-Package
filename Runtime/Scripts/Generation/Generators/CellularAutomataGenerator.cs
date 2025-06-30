using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Unity.Jobs;
using Unity.Collections;
using System.Threading.Tasks;
using Dalichrome.RandomGenerator.Utils;
using UnityEngine.Diagnostics;

namespace Dalichrome.RandomGenerator.Generators
{
    public class CellularAutomataGenerator : AbstractGenerator<CellularAutomataConfig>
    {
        private OccupanceUtil util;

        public CellularAutomataGenerator(CellularAutomataConfig config) : base(config)
        {
            this.config = config;
            util = new(config) { OutOfBoundsOccupancy = config.BorderOccupied ? 1 : 0 };
            AddUtil(util);
        }

        protected override Generation Enact(Generation input)
        {
            // DeepClone readGrid twice: one as input, one as writeGrid
            NativeTileGrid inputGrid = TileGrid.CloneNativeGrid();
            NativeTileGrid outputGrid = TileGrid.CloneNativeGrid();

            AddDisposable(inputGrid);
            AddDisposable(outputGrid);

            uint baseSeed = random.NextUInt();
            OccupanceData occupance = util.GetOccupanceData();

            for (int rep = 0; rep < config.Repetitions; rep++)
            {
                var job = new CellularAutomataJob
                {
                    readGrid = inputGrid,
                    writeGrid = outputGrid,
                    occupance = occupance,
                    liveNeighborsRequired = config.LiveNeighboursRequired,
                    placeProbability = config.PlaceProbability,
                    fillId = config.Fill,
                    emptyId = config.Empty,
                    baseSeed = baseSeed,
                    repetition = (uint)rep
                };

                JobHandle handle = job.Schedule(inputGrid.width * inputGrid.height, 64);
                handle.Complete();

                // Swap input/writeGrid for next round
                (inputGrid, outputGrid) = (outputGrid, inputGrid);
            }

            // Copy the final state back to the TileGrid's data struct
            TileGrid.OverrideSubGrid(inputGrid);
            outputGrid.Dispose();

            ClearDisposables();
            return input;
        }
    }
}
