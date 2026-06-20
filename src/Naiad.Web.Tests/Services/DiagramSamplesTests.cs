public class DiagramSamplesTests
{
    [Test]
    public async Task All_OffersSamples()
    {
        await Assert.That(DiagramSamples.All).IsNotEmpty();
        await Assert.That(DiagramSamples.Default).IsEqualTo(DiagramSamples.All[0]);
    }

    [Test]
    public async Task Find_ByName_ReturnsSample()
    {
        var sample = DiagramSamples.Find("Pie");

        await Assert.That(sample).IsNotNull();
        await Assert.That(sample!.Source).Contains("pie");
    }

    [Test]
    public async Task Find_UnknownName_ReturnsNull()
    {
        await Assert.That(DiagramSamples.Find("Nope")).IsNull();
        await Assert.That(DiagramSamples.Find(null)).IsNull();
    }

    // Every advertised sample must render cleanly through the real Naiad pipeline — this is what keeps the
    // picker honest as the renderer evolves. A failure names the offending sample and its error.
    [Test]
    public async Task EverySample_RendersWithoutError()
    {
        var failures = new List<string>();

        foreach (var sample in DiagramSamples.All)
        {
            var result = DiagramRenderer.Render(sample.Source);
            if (!result.HasSvg || result.HasError)
            {
                failures.Add($"{sample.Name}: {result.Error ?? "no SVG produced"}");
            }
        }

        await Assert.That(string.Join("; ", failures)).IsEqualTo("");
    }

    [Test]
    public async Task SampleNames_AreUnique()
    {
        var names = DiagramSamples.All.Select(_ => _.Name).ToList();

        await Assert.That(names.Distinct().Count()).IsEqualTo(names.Count);
    }
}
