using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        [LimitTileLayer(LayerType.Ground)] public TileType Ground { get { return _ground; } set { _ground = value; } }
        [SerializeField] private TileType _ground = TileType.Ground_Light;

        [LimitTileLayer(LayerType.Wall)] public TileType Wall { get { return _wall; } set { _wall = value; } }
        [SerializeField] private TileType _wall = TileType.Wall_Cave;

        [LimitTileLayer(LayerType.Object)] public TileType ContainedObject { get { return _containedObject; } set { _containedObject = value; } }
        [SerializeField] private TileType _containedObject = TileType.Object_NA;

        [LimitTileLayer(LayerType.Debug)] public TileType Debug { get { return _debug; } set { _debug = value; } }
        [SerializeField] private TileType _debug = TileType.Debug_NA;
    }
}