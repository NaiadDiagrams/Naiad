namespace Naiad;

/// <summary>
/// Loads icon packs (in the <see href="https://iconify.design">iconify</see> JSON
/// format) so their icons can be referenced as <c>prefix:name</c> wherever a diagram
/// supports icons, e.g. an architecture service: <c>service x(logos:aws-lambda)[Lambda]</c>.
/// </summary>
public static class IconPack
{
    /// <summary>
    /// Loads an icon pack from a file and registers it. Returns the pack prefix
    /// that its icons are referenced by (the <c>prefix</c> field in the JSON).
    /// </summary>
    public static string Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    /// <summary>
    /// Loads an icon pack from a stream and registers it. Returns the pack prefix
    /// that its icons are referenced by (the <c>prefix</c> field in the JSON).
    /// </summary>
    public static string Load(Stream stream) => IconPackRegistry.Register(stream);
}
