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
                _ => throw new ArgumentException($"No generator found for config type {config.GetType().Name}")
            };
        }

        public static IFilter CreateFilter(AbstractRegionFilterConfig config)
        {
            return config switch
            {
                RandomFilterConfig c => new RandomFilter(c),
                RegionSizeFilterConfig c => new RegionSizeFilter(c),
                _ => throw new ArgumentException($"Unknown filter config: {config.GetType().Name}")
            };
        }

        public static ILogicFilter CreateLogicFilter(AbstractLogicFilterConfig config, List<IFilter> inputs)
        {
            return config switch
            {
                AndFilterConfig c => new AndFilter(c, inputs),
                _ => throw new ArgumentException($"Unknown logic filter config: {config.GetType().Name}")
            };
        }

        public static ISplitter CreateSplitter(AbstractRegionSplitterConfig config, int inputs)
        {
            return config switch
            {
                RoomSplitterConfig c => new RoomSplitter(c, inputs),
                VoronoiSplitterConfig c => new VoronoiSplitter(c, inputs),
                _ => throw new ArgumentException($"Unknown splitter: {config.GetType().Name}")
            };
        }

        public static IJoiner CreateJoiner(AbstractRegionJoinerConfig config, int outputs)
        {
            return config switch
            {
                MapJoinerConfig c => new MapJoiner(c, outputs),
                _ => throw new ArgumentException($"Unknown joiner: {config.GetType().Name}")
            };
        }
    }
}