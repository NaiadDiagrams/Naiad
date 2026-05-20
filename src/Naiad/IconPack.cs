namespace Naiad;

/// <summary>
/// Loads icon packs (in the <see href="https://iconify.design">iconify</see> JSON
/// format) so their icons can be referenced as <c>prefix:name</c> wherever a diagram
/// supports icons, e.g. an architecture service: <c>service x(logos:aws-lambda)[Lambda]</c>.
/// Packs must be loaded at startup; loading after the first render throws.
/// </summary>
public static class IconPack
{
    /// <summary>
    /// Loads an icon pack from a file and registers it. Returns the pack prefix
    /// that its icons are referenced by (the <c>prefix</c> field in the JSON).
    /// </summary>
    /// <exception cref="MermaidException">A render has already occurred, or the JSON has no prefix.</exception>
    public static string Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    /// <summary>
    /// Loads an icon pack from a stream and registers it. Returns the pack prefix
    /// that its icons are referenced by (the <c>prefix</c> field in the JSON).
    /// </summary>
    /// <exception cref="MermaidException">A render has already occurred, or the JSON has no prefix.</exception>
    public static string Load(Stream stream) => IconPackRegistry.Register(stream);
}
