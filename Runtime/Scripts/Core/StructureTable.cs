// File: StructureTable.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Dalichrome.RandomGenerator.Random;

namespace Dalichrome.RandomGenerator.Core
{
    /// <summary>
    /// Immutable, runtime table of Structures with weights.
    /// Safe to share across threads once constructed.
    /// </summary>
    public sealed class StructureTable
    {
        public readonly string Name;
        public readonly Row[] Rows;
        public readonly int TotalWeight;

        public int Length {  get { return Rows.Length; } }

        public readonly struct Row
        {
            public readonly Structure Structure;
            public readonly int Weight;
            public Row(Structure structure, int weight)
            { Structure = structure; Weight = Math.Max(0, weight); }
        }

        public StructureTable(string name, IList<Row> rows)
        {
            Name = name ?? "";
            Rows = rows is Row[] arr ? arr : rows?.ToArray() ?? Array.Empty<Row>();
            int total = 0;
            for (int i = 0; i < Rows.Length; i++) total += Rows[i].Weight;
            TotalWeight = total;
        }

        /// <summary>Pick a structure by weight. Returns null if table is empty or all weights are 0.</summary>
        public Structure Pick(AbstractRandom rng)
        {
            if (Rows.Length == 0 || TotalWeight <= 0) return null;
            int n = rng.NextInt(TotalWeight);
            int acc = 0;
            for (int i = 0; i < Rows.Length; i++)
            {
                acc += Rows[i].Weight;
                if (n < acc) return Rows[i].Structure;
            }
            return Rows[^1].Structure; // fallback (shouldnft hit)
        }
    }
}
