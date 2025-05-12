using Dalichrome.RandomGenerator.Random;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator
{
    public class GenerationInfo : AbstractOperationInfo
    {
        public new TileGrid Grid { get; set; }

        public int Width { get { return Grid.width; } }

        public int Height { get { return Grid.height; } }

        public AbstractRandom Random { get { return _random; } }
        private AbstractRandom _random;
        public uint Seed
        {
            get
            {
                return _seed;
            }
            set
            {
                _seed = value;
                _random = new CSharpNativeRandom(value);
            }
        }
        private uint _seed;

        public GenerationInfo()
        {

        }

        public GenerationInfo(GenerationParams genParams)
        {
            Grid = new(genParams.Width, genParams.Height);
            Seed = genParams.Seed;
        }
    }
}