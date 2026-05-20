// Backing store for the public IconPack API. Holds icon packs registered at
// runtime in the iconify JSON format, keyed by their "prefix"; "prefix:name"
// icon references resolve against them.
static class IconPackRegistry
{
    public readonly record struct PackIcon(string Body, double Width, double Height);

    static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, PackIcon>> packs = new();

    public static string Register(Stream stream)
    {
        using var json = JsonDocument.Parse(stream);
        var root = json.RootElement;

        if (!root.TryGetProperty("prefix", out var prefixElement) ||
            prefixElement.GetString() is not { Length: > 0 } prefix)
        {
            throw new MermaidException("Icon pack JSON is missing a \"prefix\".");
        }

        var defaultWidth = root.TryGetProperty("width", out var w) ? w.GetDouble() : 16;
        var defaultHeight = root.TryGetProperty("height", out var h) ? h.GetDouble() : 16;

        var icons = new Dictionary<string, PackIcon>(StringComparer.Ordinal);

        if (root.TryGetProperty("icons", out var iconsElement))
        {
            foreach (var entry in iconsElement.EnumerateObject())
            {
                icons[entry.Name] = ReadIcon(entry.Value, defaultWidth, defaultHeight);
            }
        }

        // Aliases point at a parent icon. Transforms (rotate/flip) are not applied.
        if (root.TryGetProperty("aliases", out var aliasesElement))
        {
            foreach (var entry in aliasesElement.EnumerateObject())
            {
                if (entry.Value.TryGetProperty("parent", out var parent) &&
                    parent.GetString() is { } parentName &&
                    icons.TryGetValue(parentName, out var parentIcon))
                {
                    icons[entry.Name] = parentIcon;
                }
            }
        }

        packs[prefix] = icons;
        return prefix;
    }

    public static PackIcon? Resolve(string reference)
    {
        var colon = reference.IndexOf(':');
        if (colon <= 0 ||
            colon == reference.Length - 1)
        {
            return null;
        }

        var prefix = reference[..colon];
        var name = reference[(colon + 1)..];
        if (packs.TryGetValue(prefix, out var pack) && pack.TryGetValue(name, out var icon))
        {
            return icon;
        }

        return null;
    }

    static PackIcon ReadIcon(JsonElement element, double defaultWidth, double defaultHeight)
    {
        var body = element.GetProperty("body").GetString() ?? "";
        var width = element.TryGetProperty("width", out var w) ? w.GetDouble() : defaultWidth;
        var height = element.TryGetProperty("height", out var h) ? h.GetDouble() : defaultHeight;
        return new(body, width, height);
    }
}
