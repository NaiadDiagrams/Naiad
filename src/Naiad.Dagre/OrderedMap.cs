namespace Naiad.Dagre;

/// <summary>
/// A string-keyed map that preserves insertion order across additions and removals — the C# stand-in for a
/// JavaScript object used as a hash, whose <c>Object.keys</c> order dagre relies on for deterministic output.
/// </summary>
sealed class OrderedMap<TValue> : IEnumerable<KeyValuePair<string, TValue>>
{
    readonly Dictionary<string, LinkedListNode<KeyValuePair<string, TValue>>> map = new(StringComparer.Ordinal);
    readonly LinkedList<KeyValuePair<string, TValue>> order = new();

    public int Count => map.Count;

    public bool ContainsKey(string key) => map.ContainsKey(key);

    public TValue this[string key]
    {
        get => map[key].Value.Value;
        set
        {
            if (map.TryGetValue(key, out var node))
            {
                node.Value = new(key, value);
            }
            else
            {
                map[key] = order.AddLast(new KeyValuePair<string, TValue>(key, value));
            }
        }
    }

    public bool TryGetValue(string key, out TValue value)
    {
        if (map.TryGetValue(key, out var node))
        {
            value = node.Value.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public TValue? GetValueOrDefault(string key) =>
        map.TryGetValue(key, out var node) ? node.Value.Value : default;

    public void Remove(string key)
    {
        if (map.TryGetValue(key, out var node))
        {
            order.Remove(node);
            map.Remove(key);
        }
    }

    /// <summary>Keys in insertion order (a snapshot, safe to mutate the map while iterating the result).</summary>
    public List<string> Keys()
    {
        var keys = new List<string>(order.Count);
        foreach (var entry in order)
        {
            keys.Add(entry.Key);
        }

        return keys;
    }

    /// <summary>Values in insertion order (a snapshot).</summary>
    public List<TValue> Values()
    {
        var values = new List<TValue>(order.Count);
        foreach (var entry in order)
        {
            values.Add(entry.Value);
        }

        return values;
    }

    public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() => order.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
