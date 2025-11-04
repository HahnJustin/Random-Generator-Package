// File: StructureTableRuntimeRegistry.cs
using System;
using System.Collections.Generic;
using Dalichrome.RandomGenerator.UserData;
using Dalichrome.RandomGenerator.Core;

public static class StructureTableRegistry
{
    private static readonly object _lock = new();
    private static Dictionary<string, StructureTable> _cache = new();

    public static void ClearCache()
    {
        lock (_lock) _cache = new Dictionary<string, StructureTable>();
    }

    /// <summary>
    /// Get a runtime StructureTable for a StructureObjectTable UUID.
    /// Converts once, then serves from cache.
    /// Returns null if UUID not found.
    /// </summary>
    public static StructureTable GetRuntime(string tableUuid)
    {
        if (string.IsNullOrEmpty(tableUuid)) return null;

        // Fast path
        if (_cache.TryGetValue(tableUuid, out var rt)) return rt;

        lock (_lock)
        {
            if (_cache.TryGetValue(tableUuid, out rt)) return rt;

            var so = UUIDRegistry<StructureObjectTable>.GetByUuid(tableUuid);
            if (so == null) return null;

            rt = so.ToRuntime();
            _cache[tableUuid] = rt;
            return rt;
        }
    }
}
