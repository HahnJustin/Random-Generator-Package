using Dalichrome.RandomGenerator.Configs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{

    public static class GeneratorTypeConversions
    {
        public static AbstractGeneratorConfig GetConfig(GeneratorType type)
        {
            switch (type)
            {
                case GeneratorType.Initial:
                    return new InitialConfig();
                case GeneratorType.Noise:
                    return new NoiseConfig();
                case GeneratorType.Oval:
                    return new OvalConfig();
                case GeneratorType.Perlin:
                    return new PerlinConfig();
                case GeneratorType.Random_Walk:
                    return new RandomWalkConfig();
                case GeneratorType.Cellular_Automata:
                    return new CellularAutomataConfig();
                case GeneratorType.Copy:
                    return new CopyConfig();
                case GeneratorType.Room_Connection:
                    return new RoomConnectionConfig();
                case GeneratorType.Entrance_Exit:
                    return new EntranceExitConfig();
                case GeneratorType.DLA:
                    return new DLAConfig();
                case GeneratorType.Labyrinth:
                    return new LabyrinthConfig();
                case GeneratorType.Room_Fill:
                    return new RoomFillConfig();
                case GeneratorType.Universal_Mask:
                    return new UniversalMaskConfig();
                case GeneratorType.In_Development:
                    return new DevelopmentConfig();
                case GeneratorType.Upscaled_Noise:
                    return new UpscaledNoiseConfig();
                case GeneratorType.Distance_Fill:
                    return new DistanceFillConfig();
                case GeneratorType.Nystrom_Dungeon:
                    return new NystromDungeonConfig();
                case GeneratorType.Guarantee_Spawn:
                    return new GuaranteeSpawnConfig();
                case GeneratorType.Border:
                    return new BorderConfig();
                default:
                    Debug.LogWarning("[GeneratorDataManager] A Config without a related strategy was found");
                    return null;
            }
        }

        public static AbstractGenerator GetGeneratorFromConfig(AbstractGeneratorConfig config)
        {
            AbstractGenerator generator = null;
            switch (config)
            {
                case InitialConfig iConfig:
                    generator = new InitializeGenerator(iConfig);
                    break;
                case NoiseConfig nConfig:
                    generator = new NoiseGenerator(nConfig);
                    break;
                case OvalConfig oConfig:
                    generator = new OvalGenerator(oConfig);
                    break;
                case PerlinConfig pConfig:
                    generator = new PerlinGenerator(pConfig);
                    break;
                case RandomWalkConfig rConfig:
                    generator = new RandomWalkGenerator(rConfig);
                    break;
                case CellularAutomataConfig cConfig:
                    generator = new CellularAutomataGenerator(cConfig);
                    break;
                case CopyConfig coConfig:
                    generator = new CopyGenerator(coConfig);
                    break;
                case RoomConnectionConfig rcConfig:
                    generator = new RoomConnectionGenerator(rcConfig);
                    break;
                case EntranceExitConfig eConfig:
                    generator = new EntranceExitGenerator(eConfig);
                    break;
                case DLAConfig dConfig:
                    generator = new DLAGenerator(dConfig);
                    break;
                case LabyrinthConfig lConfig:
                    generator = new LabyrinthGenerator(lConfig);
                    break;
                case RoomFillConfig rfConfig:
                    generator = new RoomFillGenerator(rfConfig);
                    break;
                case UniversalMaskConfig umConfig:
                    generator = new UniversalMaskGenerator(umConfig);
                    break;
                case DevelopmentConfig devConfig:
                    generator = new DevelopmentGenerator(devConfig);
                    break;
                case UpscaledNoiseConfig upnConfig:
                    generator = new UpscaledNoiseGenerator(upnConfig);
                    break;
                case DistanceFillConfig distConfig:
                    generator = new DistanceFillGenerator(distConfig);
                    break;
                case NystromDungeonConfig nyDunConfig:
                    generator = new NystromDungeonGenerator(nyDunConfig);
                    break;
                case GuaranteeSpawnConfig guSpConfig:
                    generator = new GuaranteeSpawnGenerator(guSpConfig);
                    break;
                case BorderConfig boConfig:
                    generator = new BorderGenerator(boConfig);
                    break;
                default:
                    Debug.LogError("[GeneratorDataManager] A Config without a related generator was found");
                    break;
            }
            return generator;
        }
    }
}