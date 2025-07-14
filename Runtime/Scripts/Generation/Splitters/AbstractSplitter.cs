using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Data;
using UnityEngine.UIElements;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractSplitter<C> : AbstractGridOperation<C, Generation, RegionSplits>, ISplitter
        where C : AbstractRegionSplitterConfig
    {
        private readonly int expectedOutputs;
        private int filledOutputs;
        private RegionSplits resultSplits = null;

        protected AbstractSplitter(C config, int outputs) : base(config)
        {
            expectedOutputs = outputs;
        }

        public void AddOutputComplete() => filledOutputs++;
        public bool Done => filledOutputs >= expectedOutputs;

        protected override void Initialize(Generation generation)
        {
            TileGrid = generation.Grid;
            SetUtilsTileGrid();
        }

        protected abstract RegionSplits Split(Generation input);

        protected override RegionSplits Enact(Generation input)
        {
            filledOutputs += 1;
            if (resultSplits != null) 
                return resultSplits;

            resultSplits = Split(input);
            return resultSplits;
        }

        public RegionSplits GetSplits() => resultSplits;

        public void ParallelDispose()
        {
            resultSplits.ParallelDispose();
        }
    }
}
