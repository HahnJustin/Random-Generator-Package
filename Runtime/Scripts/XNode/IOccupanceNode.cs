// IOccupanceNode.cs
using System;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Nodes
{
    /// <summary>
    /// Interface for nodes that work with configs implementing IOccupanceConfig
    /// Provides consistent occupancy UI patterns across generator types
    /// </summary>
    public interface IOccupanceNode
    {
        // No properties needed - this is purely for editor detection
        // The actual occupancy properties come from the config
    }
}