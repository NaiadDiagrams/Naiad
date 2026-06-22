namespace Naiad.Dagre;

/// <summary>
/// A string-keyed map that preserves insertion order across additions and removals. Backed by the runtime's
/// array-based <see cref="OrderedDictionary{TKey,TValue}"/> (one object holding two internal arrays) rather
/// than a <c>Dictionary</c> plus a <c>LinkedList</c> with a heap node per entry, because a layout builds many
/// thousands of these small per-node maps and their construction dominates its allocation. Insertion order is
/// the property the layout's deterministic output depends on.
/// </summary>
sealed class OrderedMap<TValue> : IEnumerable<KeyValuePair<string, TValue>>
{
    readonly OrderedDictionary<string, TValue> map = new(StringComparer.Ordinal);

    public int Count => map.Count;

    public bool ContainsKey(string key) => map.ContainsKey(key);

    public TValue this[string key]
    {
        get => map[key];
        // OrderedDictionary's indexer updates an existing key in place (keeping its position) and appends an
        // unseen key at the end — the same insertion-order semantics the LinkedList-backed map had.
        set => map[key] = value;
    }

    public bool TryGetValue(string key, out TValue value)
    {
        if (map.TryGetValue(key, out var found))
        {
            value = found;
            return true;
        }

        value = default!;
        return false;
    }

    public TValue? GetValueOrDefault(string key) =>
        map.GetValueOrDefault(key);

    public void Remove(string key) => map.Remove(key);

    /// <summary>Keys in insertion order (a snapshot, safe to mutate the map while iterating the result).</summary>
    public List<string> Keys() => new(map.Keys);

    /// <summary>Values in insertion order (a snapshot).</summary>
    public List<TValue> Values() => new(map.Values);

    /// <summary>Keys in insertion order, enumerated without allocating a snapshot list. Unlike
    /// <see cref="Keys"/> the map must not be mutated while the result is iterated.</summary>
    public KeyEnumerable EnumerateKeys() => new(map);

    /// <summary>Values in insertion order, enumerated without allocating a snapshot list. Unlike
    /// <see cref="Values"/> the map must not be mutated while the result is iterated.</summary>
    public ValueEnumerable EnumerateValues() => new(map);

    // A struct GetEnumerator so `foreach (var kv in map)` in the hot layout passes does not box. The explicit
    // interface implementations remain for IEnumerable/LINQ callers.
    public OrderedDictionary<string, TValue>.Enumerator GetEnumerator() => map.GetEnumerator();

    IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator() => map.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => map.GetEnumerator();

    public readonly struct KeyEnumerable(OrderedDictionary<string, TValue> map)
    {
        public Enumerator GetEnumerator() => new(map.GetEnumerator());

        public struct Enumerator(OrderedDictionary<string, TValue>.Enumerator inner)
        {
            OrderedDictionary<string, TValue>.Enumerator inner = inner;

            public readonly string Current => inner.Current.Key;

            public bool MoveNext() => inner.MoveNext();
        }
    }

    public readonly struct ValueEnumerable(OrderedDictionary<string, TValue> map)
    {
        public Enumerator GetEnumerator() => new(map.GetEnumerator());

        public struct Enumerator(OrderedDictionary<string, TValue>.Enumerator inner)
        {
            OrderedDictionary<string, TValue>.Enumerator inner = inner;

            public readonly TValue Current => inner.Current.Value;

            public bool MoveNext() => inner.MoveNext();
        }
    }
}
