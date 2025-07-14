namespace Dalichrome.RandomGenerator.Generators
{
    public interface ISplitter : IAbstractOperation
    {
        public bool Done { get; }

        public void ParallelDispose();
    }
}
