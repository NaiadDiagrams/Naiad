using Naiad;

namespace Benchmarks;

// Benchmarks the real PNG backends (Skia and ImageSharp) rasterizing + encoding a pre-built SvgDocument.
// The document is built once in setup so parse/layout is not measured — unlike RasterBenchmarks (which uses
// a no-op surface), this drives each backend's actual surface, so backend-specific costs such as the
// ImageSharp backend's image.Mutate batching are reflected in the numbers.
[MemoryDiagnoser]
public class BackendRenderBenchmarks
{
    SvgDocument flowchart = null!;
    SvgDocument @class = null!;
    SvgDocument state = null!;
    SvgDocument sequence = null!;
    SvgDocument er = null!;

    [GlobalSetup]
    public void Setup()
    {
        flowchart = Mermaid.RenderToSvgDocument(LargeFixtures.Flowchart);
        @class = Mermaid.RenderToSvgDocument(LargeFixtures.Class);
        state = Mermaid.RenderToSvgDocument(LargeFixtures.State);
        sequence = Mermaid.RenderToSvgDocument(LargeFixtures.Sequence);
        er = Mermaid.RenderToSvgDocument(LargeFixtures.ER);
    }

    [Benchmark] public byte[] Flowchart_ImageSharp() => ImageSharpRenderer.RenderPng(flowchart, RenderOptions.Default);
    [Benchmark] public byte[] Flowchart_Skia() => SkiaRenderer.RenderPng(flowchart, RenderOptions.Default);
    [Benchmark] public byte[] Sequence_ImageSharp() => ImageSharpRenderer.RenderPng(sequence, RenderOptions.Default);
    [Benchmark] public byte[] Sequence_Skia() => SkiaRenderer.RenderPng(sequence, RenderOptions.Default);
    [Benchmark] public byte[] State_ImageSharp() => ImageSharpRenderer.RenderPng(state, RenderOptions.Default);
    [Benchmark] public byte[] State_Skia() => SkiaRenderer.RenderPng(state, RenderOptions.Default);
    [Benchmark] public byte[] ER_ImageSharp() => ImageSharpRenderer.RenderPng(er, RenderOptions.Default);
    [Benchmark] public byte[] ER_Skia() => SkiaRenderer.RenderPng(er, RenderOptions.Default);
    [Benchmark] public byte[] Class_ImageSharp() => ImageSharpRenderer.RenderPng(@class, RenderOptions.Default);
    [Benchmark] public byte[] Class_Skia() => SkiaRenderer.RenderPng(@class, RenderOptions.Default);
}
