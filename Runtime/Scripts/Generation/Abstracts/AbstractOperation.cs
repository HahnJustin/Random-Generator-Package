using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Dalichrome.RandomGenerator.Utils;
using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using Dalichrome.RandomGenerator.Random;

namespace Dalichrome.RandomGenerator.Generators
{
    public abstract class AbstractOperation<C, T> : IDisposable 
        where C : AbstractConfig 
        where T : AbstractOperationData
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

        protected abstract void Initialize(T input);
        protected abstract void Enact();
        protected abstract void PostEnact(T input);

        public void Do(T input)
        {
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            random = input.Random;
            token = input.Token;

            Initialize(input);
            InitializeUtils();

            Enact();

            PostEnact(input);

            watch.Stop();
            input.AddOperationTime(config, watch.ElapsedMilliseconds);

            CancelCheck();
        }
    }
}