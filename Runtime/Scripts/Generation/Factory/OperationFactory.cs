using Dalichrome.RandomGenerator.Configs;
using System;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator.Generators
{
    public static class OperationFactory
    {
        public static IGenerator CreateGenerator(AbstractGeneratorConfig config)
        {
            return config switch
            {
                InitialConfig iConfig => new InitializeGenerator(iConfig),
                NoiseConfig nConfig => new NoiseGenerator(nConfig),
                OvalConfig oConfig => new OvalGenerator(oConfig),
                PerlinConfig pConfig => new PerlinGenerator(pConfig),
                RandomWalkConfig rConfig => new RandomWalkGenerator(rConfig),
                CellularAutomataConfig cConfig => new CellularAutomataGenerator(cConfig),
                CopyConfig coConfig => new CopyGenerator(coConfig),
                RoomConnectionConfig rcConfig => new RoomConnectionGenerator(rcConfig),
                EntranceExitConfig eConfig => new EntranceExitGenerator(eConfig),
                DLAConfig dConfig => new DLAGenerator(dConfig),
                LabyrinthConfig lConfig => new LabyrinthGenerator(lConfig),
                RoomFillConfig rfConfig => new RoomFillGenerator(rfConfig),
                UniversalMaskConfig umConfig => new UniversalMaskGenerator(umConfig),
                DevelopmentConfig devConfig => new DevelopmentGenerator(devConfig),
                UpscaledNoiseConfig upnConfig => new UpscaledNoiseGenerator(upnConfig),
                DistanceFillConfig distConfig => new DistanceFillGenerator(distConfig),
                NystromDungeonConfig nyDunConfig => new NystromDungeonGenerator(nyDunConfig),
                GuaranteeSpawnConfig guSpConfig => new GuaranteeSpawnGenerator(guSpConfig),
                BorderConfig boConfig => new BorderGenerator(boConfig),
                FillConfig fillConfig => new FillGenerator(fillConfig),
                StructureConfig structConfig => new StructureGenerator(structConfig),
                MetaPerlinConfig metaPerlinConfig => new MetaPerlinGenerator(metaPerlinConfig),
                MetaOperationConfig metaOperationConfig => new MetaOperationGenerator(metaOperationConfig),
                _ => throw new ArgumentException($"Unknown generator config: {config.GetType().Name}")
            };
        }

        public static IFilter CreateFilter(AbstractRegionFilterConfig config)
        {
            return config switch
            {
                ChanceFilterConfig c => new ChanceFilter(c),
                RegionSizeFilterConfig c => new RegionSizeFilter(c),
                GuaranteeFilterConfig c => new GuaranteeFilter(c),
                ContainsFilterConfig c => new ContainsFilter(c),
                DistanceFilterConfig c => new DistanceFilter(c),
                _ => throw new ArgumentException($"Unknown filter config: {config.GetType().Name}")
            };
        }

        public static ILogicFilter CreateLogicFilter(AbstractLogicFilterConfig config, List<IFilter> inputs)
        {
            return config switch
            {
                AndFilterConfig c => new AndFilter(c, inputs),
                OrFilterConfig c => new OrFilter(c, inputs),
                NotFilterConfig c => new NotFilter(c, inputs),
                _ => throw new ArgumentException($"Unknown logic filter config: {config.GetType().Name}")
            };
        }

        public static ISplitter CreateSplitter(AbstractRegionSplitterConfig config, int inputs)
        {
            return config switch
            {
                RoomSplitterConfig c => new RoomSplitter(c, inputs),
                VoronoiSplitterConfig c => new VoronoiSplitter(c, inputs),
                ProximitySplitterConfig c => new ProxiomitySplitter(c, inputs),
                PerlinSplitterConfig c => new PerlinSplitter(c, inputs),
                MultiPerlinSplitterConfig c => new MultiPerlinSplitter(c, inputs),
                UpscaleNoiseSplitterConfig c => new UpscaleNoiseSplitter(c, inputs),
                MetaSplitterConfig c => new MetaSplitter(c, inputs),
                _ => throw new ArgumentException($"Unknown splitter: {config.GetType().Name}")
            };
        }

        public static IJoiner CreateJoiner(AbstractRegionJoinerConfig config, int outputs)
        {
            return config switch
            {
                MapJoinerConfig c => new MapJoiner(c, outputs),
                RegionJoinerConfig c => new RegionJoiner(c, outputs),
                _ => throw new ArgumentException($"Unknown joiner: {config.GetType().Name}")
            };
        }
    }
}