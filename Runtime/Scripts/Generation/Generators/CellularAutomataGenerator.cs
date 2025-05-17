using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Unity.Jobs;
using Unity.Collections;
using System.Threading.Tasks;

namespace Dalichrome.RandomGenerator.Generators
{
    public class CellularAutomataGenerator : OccupanceGenerator
    {
        protected new CellularAutomataConfig config;

        public CellularAutomataGenerator(CellularAutomataConfig config) : base(config)
        {
            this.config = config;
            OutOfBoundsOccupancy = this.config.BorderOccupied ? 1 : 0;
        }

        protected override void Enact()
        {
            // DeepClone readGrid twice: one as input, one as writeGrid
            TileGridData inputGrid = TileGridData.DeepClone(TileGrid.GetGridData());
            TileGridData outputGrid = TileGridData.DeepClone(TileGrid.GetGridData());

            uint baseSeed = random.NextUInt();
            OccupanceData occupance = GetOccupanceData();

            for (int rep = 0; rep < config.Repetitions; rep++)
            {
                var job = new CellularAutomataJob
                {
                    readGrid = inputGrid,
                    writeGrid = outputGrid,
                    occupance = occupance,
                    liveNeighborsRequired = config.LiveNeighboursRequired,
                    placeProbability = config.PlaceProbability,
                    fillType = config.Fill,
                    emptyType = config.Empty,
                    baseSeed = baseSeed,
                    repetition = (uint)rep
                };

                JobHandle handle = job.Schedule(inputGrid.width * inputGrid.height, 64);
                handle.Complete();

                // Swap input/writeGrid for next round
                (inputGrid, outputGrid) = (outputGrid, inputGrid);
            }

            // Copy the final state back to the TileGrid's buffer
            TileGrid.OverrideGridData(inputGrid);
            outputGrid.Dispose();
        }
    }
}
