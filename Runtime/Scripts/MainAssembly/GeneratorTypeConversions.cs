using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Generators;
using System;
using UnityEngine;

namespace Dalichrome.RandomGenerator
{
    public static class GeneratorTypeConversions
    {
        public static AbstractGeneratorConfig GetConfig(GeneratorType type) => type switch
        {
            GeneratorType.Initial => new InitialConfig(),
            GeneratorType.Noise => new NoiseConfig(),
            GeneratorType.Oval => new OvalConfig(),
            GeneratorType.Perlin => new PerlinConfig(),
            GeneratorType.Random_Walk => new RandomWalkConfig(),
            GeneratorType.Cellular_Automata => new CellularAutomataConfig(),
            GeneratorType.Copy => new CopyConfig(),
            GeneratorType.Room_Connection => new RoomConnectionConfig(),
            GeneratorType.Entrance_Exit => new EntranceExitConfig(),
            GeneratorType.DLA => new DLAConfig(),
            GeneratorType.Labyrinth => new LabyrinthConfig(),
            GeneratorType.Room_Fill => new RoomFillConfig(),
            GeneratorType.Universal_Mask => new UniversalMaskConfig(),
            GeneratorType.In_Development => new DevelopmentConfig(),
            GeneratorType.Upscaled_Noise => new UpscaledNoiseConfig(),
            GeneratorType.Distance_Fill => new DistanceFillConfig(),
            GeneratorType.Nystrom_Dungeon => new NystromDungeonConfig(),
            GeneratorType.Guarantee_Spawn => new GuaranteeSpawnConfig(),
            GeneratorType.Border => new BorderConfig(),
            _ => LogAndReturnNull(type)
        };

        private static AbstractGeneratorConfig LogAndReturnNull(GeneratorType type)
        {
            Debug.LogError($"[GeneratorDataManager] No config found for generator type: {type}");
            return null;
        }

        public static IGenerator GetGeneratorFromConfig(AbstractGeneratorConfig config)
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
    }
}
