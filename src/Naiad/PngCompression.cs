namespace Naiad;

/// <summary>
/// How hard a PNG backend compresses its output. This only trades encode speed against file size — the
/// rendered pixels are identical at every level. Both the Naiad.Skia and Naiad.ImageSharp backends map
/// these onto the matching zlib deflate level.
/// </summary>
public enum PngCompression
{
    /// <summary>Fastest to encode, largest files (zlib level 1).</summary>
    Fast,

    /// <summary>Balanced encode speed and file size — the default (zlib level 6).</summary>
    Balanced,

    /// <summary>Smallest files, slowest to encode (zlib level 9).</summary>
    Small,
}
