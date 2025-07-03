using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using Dalichrome.RandomGenerator.Random;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractOperation<C, D, R> : IDisposable 
        where C : AbstractConfig 
        where D : AbstractOperationData
        where R : AbstractOperationData
    {
        protected CancellationToken token;
        protected AbstractRandom random;
        protected List<AbstractUtil> utils = new();
        protected List<IDisposable> disposables = new();

        protected C config;

        protected AbstractOperation()
        {
            config = default;
        }

        public AbstractOperation(C config)
        {
            this.config = config;
        }

        protected virtual void InitializeUtils()
        {
            foreach (AbstractUtil util in utils)
            {
                if (util is IInitializableUtil initializable) initializable.Initialize();
            }
        }

        protected void CancelCheck()
        {
            Dispose();
            token.ThrowIfCancellationRequested();
        }

        protected void AddUtil(AbstractUtil util)
        {
            utils.Add(util);
        }

        protected void AddDisposable(IDisposable disposable)
        {
            disposables.Add(disposable);
        }

        protected void ClearDisposables()
        {
            disposables.Clear();
        }

        public void Dispose()
        {
            disposables.ForEach(x => x.Dispose());
            ClearDisposables();
        }

        protected virtual bool RunCondition(D input) { return true; }

        protected virtual void Initialize(D input) { }
        protected abstract R Enact(D input);
        protected virtual void PostEnact(R output) { }

        public R Do(D input)
        {
            if (!RunCondition(input)) return null;

            var watch = new Stopwatch();
            watch.Start();

            random = input.Random;
            token = input.Token;

            Initialize(input);
            InitializeUtils();

            R output = Enact(input);

            PostEnact(output);

            watch.Stop();
            input.AddOperationTime(config, watch.ElapsedMilliseconds);

            CancelCheck();
            return output;
        }
    }
}