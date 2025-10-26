using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class InitialConfig : AbstractGeneratorConfig
    {
        public InitialConfig()
        {
            _description = StringType.Description_Generator_Initial;
        }

        public override GeneratorType Type { get { return GeneratorType.Initial; } }

        [TileDisplay, LimitTileLayer(LayerType.Ground)] public int Ground { get { return _ground; } set { _ground = value; } }
        [TileDisplay, LimitTileLayer(LayerType.Ground), SerializeField] private int _ground = (int)TileDefaults.Ground_Light;

        [TileDisplay, LimitTileLayer(LayerType.Wall)] public int Wall { get { return _wall; } set { _wall = value; } }
        [TileDisplay, LimitTileLayer(LayerType.Wall), SerializeField] private int _wall = (int)TileDefaults.Wall_Cave;

        [TileDisplay, LimitTileLayer(LayerType.Object)] public int ContainedObject { get { return _containedObject; } set { _containedObject = value; } }
        [TileDisplay, LimitTileLayer(LayerType.Object), SerializeField] private int _containedObject = (int)TileDefaults.Object_NA;

        [TileDisplay, LimitTileLayer(LayerType.Debug)] public int Debug { get { return _debug; } set { _debug = value; } }
        [TileDisplay, LimitTileLayer(LayerType.Debug), SerializeField] private int _debug = (int)TileDefaults.Debug_NA;
    }
}