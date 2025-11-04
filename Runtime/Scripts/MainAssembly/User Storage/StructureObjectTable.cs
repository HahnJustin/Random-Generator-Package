// File: StructureObjectTable.cs (append/replace class body as shown)
using Dalichrome.RandomGenerator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dalichrome.RandomGenerator.UserData
{
    [CreateAssetMenu(menuName = "RandomGenerator/UserData/StructureTable", order = 1001)]
    [UUIDFolder("StructureTables")]
    public class StructureObjectTable : UUIDScriptableObject
    {
        [SerializeField] private List<TableEntry> table;

        [Serializable]
        private class TableEntry
        {
            public StructureObject structure;
            public int weight = 100;
        }

        /// <summary>Public read-only projection (safe; doesnÅft expose the private class).</summary>
        public IReadOnlyList<(StructureObject structure, int weight)> Entries =>
            (table == null)
                ? Array.Empty<(StructureObject, int)>()
                : table.Select(e => (e.structure, e.weight)).ToList();

        /// <summary>
        /// Materialize a runtime StructureTable (pure C# object). This performs the heavy
        /// lifting (flattening tiles) once so generators can safely use it on worker threads.
        /// </summary>
        public StructureTable ToRuntime()
        {
            var rows = new List<StructureTable.Row>(Entries.Count);
            foreach (var (so, weight) in Entries)
            {
                if (so == null) continue;

                // Convert the layered StructureObject into a flat int[] grid.
                // The array length is width * height * TileLayerRegistry.LayerCount (z-stacked).
                var flat = so.ConvertToIntArray(); // already provided on StructureObject
                var runtimeStruct = new Structure(flat, so.width, so.height);
                rows.Add(new StructureTable.Row(runtimeStruct, Math.Max(0, weight)));
            }
            return new StructureTable(DisplayName, rows);
        }
    }
}
