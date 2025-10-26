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
            Debug.LogError($"[GeneratorTypeConversions] No config found for generator type: {type}");
            return null;
        }
    }
}
