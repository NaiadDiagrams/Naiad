using System.Numerics;
using Naiad;

namespace Benchmarks;

// Benchmarks the SVG -> raster walk (CSS cascade resolution, transforms, geometry flattening, marker and
// label layout) in isolation. The SvgDocument is built once in setup so parsing/layout is not measured,
// and the surface is a no-op that records nothing — so MemoryDiagnoser attributes allocations to the
// rasterizer's per-element work rather than to a backend's pixel buffer or PNG encode.
[MemoryDiagnoser]
public class RasterBenchmarks
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

    [Benchmark] public int Flowchart_Rasterize() => Rasterize(flowchart);
    [Benchmark] public int Class_Rasterize() => Rasterize(@class);
    [Benchmark] public int State_Rasterize() => Rasterize(state);
    [Benchmark] public int Sequence_Rasterize() => Rasterize(sequence);
    [Benchmark] public int ER_Rasterize() => Rasterize(er);

    static int Rasterize(SvgDocument document)
    {
        using var surface = SvgRasterizer.Paint(document, 1.0, (_, _) => new NoOpSurface());
        return surface.PrimitiveCount;
    }

    // Counts primitives so the result is consumed (no dead-code elimination) without doing any real
    // drawing, encoding or buffer allocation.
    sealed class NoOpSurface : IRenderSurface
    {
        public int PrimitiveCount { get; private set; }

        public void FillPath(IReadOnlyList<SubPath> subpaths, Matrix3x2 transform, Paint paint, FillRule rule, float opacity) =>
            PrimitiveCount++;

        public void StrokePath(IReadOnlyList<SubPath> subpaths, Matrix3x2 transform, Rgba color, float width, IReadOnlyList<float>? dash, float opacity) =>
            PrimitiveCount++;

        public void DrawText(string text, float x, float y, Matrix3x2 transform, TextStyle style) =>
            PrimitiveCount++;

        public void Encode(Stream stream)
        {
        }

        public void Dispose()
        {
        }
    }
}
