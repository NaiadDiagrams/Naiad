namespace Naiad.Dagre;

static class ResolveConflicts
{
    sealed class MappedEntry
    {
        public int Indegree;
        public List<MappedEntry> In = [];
        public List<MappedEntry> Out = [];
        public List<string> Vs = [];
        public int I;
        public double? Barycenter;
        public double? Weight;
        public bool Merged;
    }

    public static List<ResolvedEntry> Run(List<BarycenterEntry> entries, Graph constraintGraph)
    {
        var mappedEntries = new Dictionary<string, MappedEntry>(StringComparer.Ordinal);
        var insertionOrder = new List<string>();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var tmp = new MappedEntry
            {
                Indegree = 0,
                In = [],
                Out = [],
                Vs = [entry.V],
                I = i
            };
            if (entry.Barycenter != null)
            {
                tmp.Barycenter = entry.Barycenter;
                tmp.Weight = entry.Weight;
            }

            if (!mappedEntries.ContainsKey(entry.V))
            {
                insertionOrder.Add(entry.V);
            }

            mappedEntries[entry.V] = tmp;
        }

        foreach (var e in constraintGraph.Edges())
        {
            var hasV = mappedEntries.TryGetValue(e.V, out var entryV);
            var hasW = mappedEntries.TryGetValue(e.W, out var entryW);
            if (hasV && hasW)
            {
                entryW!.Indegree++;
                entryV!.Out.Add(entryW);
            }
        }

        var sourceSet = insertionOrder
            .Select(v => mappedEntries[v])
            .Where(entry => entry.Indegree == 0)
            .ToList();

        return DoResolveConflicts(sourceSet);
    }

    static List<ResolvedEntry> DoResolveConflicts(List<MappedEntry> sourceSet)
    {
        var entries = new List<MappedEntry>();

        static Action<MappedEntry> HandleIn(MappedEntry vEntry) =>
            uEntry =>
            {
                if (uEntry.Merged)
                {
                    return;
                }

                if (uEntry.Barycenter == null ||
                    vEntry.Barycenter == null ||
                    uEntry.Barycenter >= vEntry.Barycenter)
                {
                    MergeEntries(vEntry, uEntry);
                }
            };

        Action<MappedEntry> HandleOut(MappedEntry vEntry) =>
            wEntry =>
            {
                wEntry.In.Add(vEntry);
                if (--wEntry.Indegree == 0)
                {
                    sourceSet.Add(wEntry);
                }
            };

        while (sourceSet.Count > 0)
        {
            var entry = sourceSet[^1];
            sourceSet.RemoveAt(sourceSet.Count - 1);
            entries.Add(entry);
            entry.In.Reverse();
            var handleIn = HandleIn(entry);
            foreach (var uEntry in entry.In)
            {
                handleIn(uEntry);
            }

            var handleOut = HandleOut(entry);
            foreach (var wEntry in entry.Out)
            {
                handleOut(wEntry);
            }
        }

        return entries
            .Where(_ => !_.Merged)
            .Select(entry => new ResolvedEntry
            {
                Vs = entry.Vs,
                I = entry.I,
                Barycenter = entry.Barycenter,
                Weight = entry.Weight
            })
            .ToList();
    }

    static void MergeEntries(MappedEntry target, MappedEntry source)
    {
        var sum = 0d;
        var weight = 0d;

        if (target.Weight is { } tw && tw != 0)
        {
            sum += target.Barycenter!.Value * tw;
            weight += tw;
        }

        if (source.Weight is { } sw && sw != 0)
        {
            sum += source.Barycenter!.Value * sw;
            weight += sw;
        }

        target.Vs = [.. source.Vs, .. target.Vs];
        target.Barycenter = sum / weight;
        target.Weight = weight;
        target.I = Math.Min(source.I, target.I);
        source.Merged = true;
    }
}
