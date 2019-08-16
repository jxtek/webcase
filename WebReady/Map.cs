using System;
using System.Collections;
using System.Collections.Generic;

namespace WebReady
{
    /// <summary>
    /// An add-only data collection that can act as both a list, a dictionary and/or a two-layered tree.
    /// </summary>
    public class Map<K, V> : IReadOnlyList<Map<K, V>.Entry>
    {
        int[] buckets;

        protected Entry[] entries;

        int count;

        // current group head
        int head = -1;

        public Map(int capacity = 16)
        {
            // find a least power of 2 that is greater than or equal to capacity
            int size = 8;
            while (size < capacity)
            {
                size <<= 1;
            }

            ReInit(size);
        }

        void ReInit(int size) // size must be power of 2
        {
            if (entries == null || size > entries.Length) // allocalte new arrays as needed
            {
                buckets = new int[size];
                entries = new Entry[size];
            }

            for (int i = 0; i < buckets.Length; i++) // initialize all buckets to -1
            {
                buckets[i] = -1;
            }

            count = 0;
        }

        public int Count => count;

        public Entry EntryAt(int idx) => entries[idx];

        public V[] GroupOf(K key)
        {
            int idx = IndexOf(key);
            if (idx > -1)
            {
                int tail = entries[idx].tail;
                int ret = tail - idx; // number of returned elements
                V[] arr = new V[ret];
                for (int i = 0; i < ret; i++)
                {
                    arr[i] = entries[idx + 1 + i].value;
                }

                return arr;
            }

            return null;
        }

        public int IndexOf(K key)
        {
            int code = key.GetHashCode() & 0x7fffffff;
            int buck = code % buckets.Length; // target bucket
            int idx = buckets[buck];
            while (idx != -1)
            {
                Entry e = entries[idx];
                if (e.Match(code, key))
                {
                    return idx;
                }

                idx = entries[idx].next; // adjust for next index
            }

            return -1;
        }

        public void Clear()
        {
            if (entries != null)
            {
                ReInit(entries.Length);
            }
        }

        public void Add(K key, V value)
        {
            Add(key, value, false);
        }

        public void Add<M>(M v) where M : V, IKeyable<K>
        {
            Add(v.Key, v, false);
        }

        void Add(K key, V value, bool rehash)
        {
            // ensure double-than-needed capacity
            if (!rehash && count >= entries.Length / 2)
            {
                Entry[] old = entries;
                int oldc = count;
                ReInit(entries.Length * 2);
                // re-add old elements
                for (int i = 0; i < oldc; i++)
                {
                    Add(old[i].key, old[i].value, true);
                }
            }

            int code = key.GetHashCode() & 0x7fffffff;
            int buck = code % buckets.Length; // target bucket
            int idx = buckets[buck];
            while (idx != -1)
            {
                Entry e = entries[idx];
                if (e.Match(code, key))
                {
                    e.value = value;
                    return; // replace the old value
                }

                idx = entries[idx].next; // adjust for next index
            }

            // add a new entry
            idx = count;
            entries[idx] = new Entry(code, buckets[buck], key, value);
            buckets[buck] = idx;
            count++;

            // decide group
            if (value is IGroupKeyable<K> gkeyable)
            {
                // compare to current head
                if (head == -1 || !gkeyable.GroupAs(entries[head].key))
                {
                    head = idx;
                }

                entries[head].tail = idx;
            }
        }

        public bool Contains(K key)
        {
            if (TryGetValue(key, out _))
            {
                return true;
            }

            return false;
        }

        public V GetValue(K key)
        {
            return TryGetValue(key, out V v) ? v : default;
        }

        public bool TryGetValue(K key, out V value)
        {
            int code = key.GetHashCode() & 0x7fffffff;
            int buck = code % buckets.Length; // target bucket
            int idx = buckets[buck];
            while (idx != -1)
            {
                var e = entries[idx];
                if (e.Match(code, key))
                {
                    value = e.value;
                    return true;
                }

                idx = entries[idx].next; // adjust for next index
            }

            value = default;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public IEnumerator<Entry> GetEnumerator()
        {
            return new Enumerator(this);
        }

        //
        // advanced search operations that can be overridden with concurrency constructs

        public Entry this[int index]
        {
            get
            {
                if (index >= count)
                {
                    throw new IndexOutOfRangeException();
                }

                return entries[index];
            }
        }

        public V[] All(Predicate<V> cond = null)
        {
            var list = new ValueList<V>(16);
            for (int i = 0; i < count; i++)
            {
                var v = entries[i].value;
                if (cond == null || cond(v))
                {
                    list.Add(v);
                }
            }

            return list.ToArray();
        }

        public V Find(Predicate<V> cond = null)
        {
            for (int i = 0; i < count; i++)
            {
                var v = entries[i].value;
                if (cond == null || cond(v))
                {
                    return v;
                }
            }

            return default;
        }

        public void ForEach(Func<K, V, bool> cond, Action<K, V> hand)
        {
            for (int i = 0; i < count; i++)
            {
                K key = entries[i].key;
                V value = entries[i].value;
                if (cond == null || cond(key, value))
                {
                    hand(entries[i].key, entries[i].value);
                }
            }
        }

        public struct Entry
        {
            readonly int code; // lower 31 bits of hash code

            internal readonly K key; // entry key

            internal V value; // entry value

            internal readonly int next; // index of next entry, -1 if last

            internal int tail; // the index of group tail, when this is the head entry

            internal Entry(int code, int next, K key, V value)
            {
                this.code = code;
                this.next = next;
                this.key = key;
                this.value = value;
                this.tail = -1;
            }

            internal bool Match(int code, K key)
            {
                return this.code == code && this.key.Equals(key);
            }

            public override string ToString()
            {
                return key.ToString();
            }

            public K Key => key;

            public V Value => value;

            public bool IsHead => tail > -1;
        }

        public struct Enumerator : IEnumerator<Entry>
        {
            readonly Map<K, V> map;

            int current;

            internal Enumerator(Map<K, V> map)
            {
                this.map = map;
                current = -1;
            }

            public bool MoveNext()
            {
                return ++current < map.Count;
            }

            public void Reset()
            {
                current = -1;
            }

            public Entry Current => map.entries[current];

            object IEnumerator.Current => map.entries[current];

            public void Dispose()
            {
            }
        }

        static void Test()
        {
            Map<string, string> m = new Map<string, string>
            {
                {"010101", "mike"},
                {"010102", "jobs"},
                {"010103", "tim"},
                {"010104", "john"},
                {"010301", "abigae"},
                {"010302", "stephen"},
                {"010303", "cox"},
            };

            var r = m.GroupOf("010101");
        }
    }
}