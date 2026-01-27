using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Utils
{
    public class FloodFillUtil : AbstractUtil
    {
        public bool DoInitialization { get; set; }

        private HashSet<int2> visited = new();

        private Predicate<int2> fillPredicate;
        private Action<int2> onFill;

        private static readonly int2[] nearby =
        {
            new (-1, 0),
            new (0, -1),
            new (1, 0),
            new (0, 1),
        };

        public FloodFillUtil(AbstractConfig config) : base(config)
        {
            this.config = config;
        }

        public void SetFillPredicate(Predicate<int2> fillPredicate)
        {
            this.fillPredicate = fillPredicate;
        }

        public void SetOnFillAction(Action<int2> onFill)
        {
            this.onFill = onFill;
        }

        public void FloodFill(int2 start)
        {
            if (fillPredicate == null || onFill == null) return;

            var stack = new Stack<int2>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var position = stack.Pop();

                if (tileGrid.IsRestricted(position) || visited.Contains(position))
                    continue;

                visited.Add(position);

                if (!fillPredicate(position))
                    continue;

                onFill(position);

                foreach (var movement in nearby)
                    stack.Push(position + movement);
            }
        }

        public bool IsVisited(int2 position)
        {
            return visited.Contains(position);
        }

        public void ResetVisited()
        {
            visited.Clear();
        }
    }
}