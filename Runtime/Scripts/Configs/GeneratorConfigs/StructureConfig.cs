using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Configs
{
    [Serializable]
    public class StructureConfig : AbstractGeneratorConfig, IStructureConfig, IOccupanceConfig
    {
        public StructureConfig()
        {
            _description = StringType.Description_Generator_Structure;
        }

        public override GeneratorType Type { get { return GeneratorType.Structure; } }

        public string StructureTableId { get { return _structureTableId; } set { _structureTableId = value; } }
        [SerializeField, StructureDisplay] private string _structureTableId = "";

        public StructureTable StructureTable { get { return _structureTable; } set { _structureTable = value; } }
        [SerializeField] private StructureTable _structureTable;

        public TryAmountType TryAmount { get { return _tryAmount; } set { _tryAmount = value; } }
        [SerializeField] protected TryAmountType _tryAmount = TryAmountType.Once;

        [Condition("TryAmount", TryAmountType.ToConstant)] public int TryAttempts { get { return _tryAttempts; } set { _tryAttempts = value; } }
        [Condition("TryAmount", TryAmountType.ToConstant), SerializeField] protected int _tryAttempts = 5;

        public AnchorType Anchor { get { return _anchorType; } set { _anchorType = value; } }
        [SerializeField] protected AnchorType _anchorType = AnchorType.Random;

        public OccupanceType Occupance { get { return _occupance; } set { _occupance = value; } }
        [SerializeField] protected OccupanceType _occupance = OccupanceType.Default;

        [LayerDisplay, Condition("Occupance", OccupanceType.Layer_Not_NA)] public int OccupyLayer { get { return _occupyLayer; } set { _occupyLayer = value; } }
        [LayerDisplay, Condition("Occupance", OccupanceType.Layer_Not_NA), SerializeField] protected int _occupyLayer = (int)LayerType.Wall;

        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A)] public int TileA { get { return _tileA; } set { _tileA = value; } }
        [TileDisplay, Condition("Occupance", OccupanceType.Contains_A), SerializeField] protected int _tileA = (int)TileDefaults.Wall_NA;

        public bool InvertOccupance { get { return _invertOccupance; } set { _invertOccupance = value; } }
        [SerializeField] protected bool _invertOccupance = false;
    }
}
