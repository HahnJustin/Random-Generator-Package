using System.Collections;
using System.Collections.Generic;

namespace Dalichrome.RandomGenerator.Utils
{
    public class OrderedSet<T> : IEnumerable<T>
    {
        private readonly Dictionary<T, LinkedListNode<T>> _map;
        private readonly LinkedList<T> _list;

        public OrderedSet(int capacity = 0)
        {
            _map = new Dictionary<T, LinkedListNode<T>>(capacity);
            _list = new LinkedList<T>();
        }

        public int Count => _map.Count;

        public bool Add(T item)
        {
            if (_map.ContainsKey(item)) return false;
            var node = _list.AddLast(item);
            _map[item] = node;
            return true;
        }

        public bool Remove(T item)
        {
            if (!_map.TryGetValue(item, out var node)) return false;
            _map.Remove(item);
            _list.Remove(node);
            return true;
        }

        public bool Contains(T item) => _map.ContainsKey(item);

        public void Clear()
        {
            _map.Clear();
            _list.Clear();
        }

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Optional: expose a stable indexer if you really need it (O(n))
        public T this[int index]
        {
            get
            {
                var i = 0;
                for (var node = _list.First; node != null; node = node.Next)
                {
                    if (i++ == index) return node.Value;
                }
                throw new System.IndexOutOfRangeException();
            }
        }
    }
}