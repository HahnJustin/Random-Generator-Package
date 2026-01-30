using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Utils
{
    public class MetaFunctionUtil : AbstractUtil, IInitializableUtil
    {
        protected new IMetaFunctionConfig config;

        private CompiledMetaFunction compiledFunc;

        public bool DoInitialization { get; set; }

        public MetaFunctionUtil(IMetaFunctionConfig config) : base((AbstractConfig)config)
        {
            this.config = config;
            DoInitialization = true;
        }

        private void Compile()
        {
            try
            {
                compiledFunc = MetaFunctionCompiler.Compile(
                    config.MetaFunction.expression,
                    config.MetaFunction.outputKey
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"MetaFunctionUtil compile failed. " +
                    $"outputKey='{config.MetaFunction.outputKey}'.\n" +
                    $"{ex}"
                );
                throw;
            }
        }

        public void Initialize()
        {
            Compile();
        }

        public void ApplyFunction(int x, int y)
        {
            compiledFunc.Run(ref tileGrid, x, y);
        }
    }
}