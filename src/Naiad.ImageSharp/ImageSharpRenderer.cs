namespace Naiad;

/// <summary>
/// Renders Mermaid diagram markup to a PNG raster through
/// <see href="https://github.com/SixLabors/ImageSharp">SixLabors.ImageSharp</see>. The parse, layout and
/// styling that produce the diagram are exactly the same code path as
/// <see cref="Mermaid.Render(string, RenderOptions)"/>; this only swaps the final stage from emitting SVG
/// to rasterizing it with ImageSharp. The companion Naiad.Skia package renders the same way through
/// Skia. Rasterization is controlled by <see cref="RenderOptions.Png"/> (scale and background).
/// </summary>
public static class ImageSharpRenderer
{
    /// <summary>Renders <paramref name="mermaid"/> to PNG bytes.</summary>
    public static byte[] RenderPng(string mermaid, RenderOptions? options = null)
    {
        using var stream = new MemoryStream();
        RenderPng(mermaid, stream, options);
        return stream.ToArray();
    }

    /// <summary>Renders <paramref name="mermaid"/> to a PNG file at <paramref name="path"/>.</summary>
    public static void RenderPng(string mermaid, string path, RenderOptions? options = null)
    {
        // Render fully into memory before touching the destination so a parse/validation failure leaves
        // the file untouched rather than stranding a partial PNG.
        var bytes = RenderPng(mermaid, options);
        File.WriteAllBytes(path, bytes);
    }

    /// <summary>Renders <paramref name="mermaid"/> to PNG and writes it to <paramref name="stream"/>.</summary>
    public static void RenderPng(string mermaid, Stream stream, RenderOptions? options = null)
    {
        options ??= RenderOptions.Default;
        RenderDocument(Mermaid.RenderToSvgDocument(mermaid, options), stream, options);
    }

    // Renders an already parsed/laid-out document. This is the seam BackendRenderBenchmarks uses to measure
    // the ImageSharp rasterize + encode path on its own, without the parse/layout cost dominating the number.
    internal static byte[] RenderPng(SvgDocument document, RenderOptions options)
    {
        using var stream = new MemoryStream();
        RenderDocument(document, stream, options);
        return stream.ToArray();
    }

    static void RenderDocument(SvgDocument document, Stream stream, RenderOptions options)
    {
        var background = CssColor.TryParse(options.Png.Background, out var color) ? color : Rgba.White;
        using var surface = SvgRasterizer.Paint(document, options.Png.Scale, (width, height) => new ImageSharpSurface(width, height, background));
        surface.Encode(stream);
    }
}
