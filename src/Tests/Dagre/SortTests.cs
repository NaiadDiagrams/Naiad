namespace Naiad.Dagre.Tests;

public class SortTests
{
    static string Join(List<string> vs) => string.Join(",", vs);

    [Test]
    public async Task SortsNodesByBarycenter()
    {
        var input = new List<ResolvedEntry>
        {
            new() { Vs = ["a"], I = 0, Barycenter = 2, Weight = 3 },
            new() { Vs = ["b"], I = 1, Barycenter = 1, Weight = 2 }
        };

        var result = Sort.Run(input);
        await Assert.That(Join(result.Vs)).IsEqualTo("b,a");
        await Assert.That(result.Barycenter!.Value).IsEqualTo((2 * 3 + 1 * 2) / (3.0 + 2)).Within(0.001);
        await Assert.That(result.Weight!.Value).IsEqualTo(3 + 2).Within(0.001);
    }

    [Test]
    public async Task CanSortSuperNodes()
    {
        var input = new List<ResolvedEntry>
        {
            new() { Vs = ["a", "c", "d"], I = 0, Barycenter = 2, Weight = 3 },
            new() { Vs = ["b"], I = 1, Barycenter = 1, Weight = 2 }
        };

        var result = Sort.Run(input);
        await Assert.That(Join(result.Vs)).IsEqualTo("b,a,c,d");
        await Assert.That(result.Barycenter!.Value).IsEqualTo((2 * 3 + 1 * 2) / (3.0 + 2)).Within(0.001);
        await Assert.That(result.Weight!.Value).IsEqualTo(3 + 2).Within(0.001);
    }

    [Test]
    public async Task BiasesToTheLeftByDefault()
    {
        var input = new List<ResolvedEntry>
        {
            new() { Vs = ["a"], I = 0, Barycenter = 1, Weight = 1 },
            new() { Vs = ["b"], I = 1, Barycenter = 1, Weight = 1 }
        };

        var result = Sort.Run(input);
        await Assert.That(Join(result.Vs)).IsEqualTo("a,b");
        await Assert.That(result.Barycenter!.Value).IsEqualTo(1).Within(0.001);
        await Assert.That(result.Weight!.Value).IsEqualTo(2).Within(0.001);
    }

    [Test]
    public async Task BiasesToTheRightIfBiasRightIsTrue()
    {
        var input = new List<ResolvedEntry>
        {
            new() { Vs = ["a"], I = 0, Barycenter = 1, Weight = 1 },
            new() { Vs = ["b"], I = 1, Barycenter = 1, Weight = 1 }
        };

        var result = Sort.Run(input, true);
        await Assert.That(Join(result.Vs)).IsEqualTo("b,a");
        await Assert.That(result.Barycenter!.Value).IsEqualTo(1).Within(0.001);
        await Assert.That(result.Weight!.Value).IsEqualTo(2).Within(0.001);
    }

    [Test]
    public async Task CanSortNodesWithoutABarycenter()
    {
        var input = new List<ResolvedEntry>
        {
            new() { Vs = ["a"], I = 0, Barycenter = 2, Weight = 1 },
            new() { Vs = ["b"], I = 1, Barycenter = 6, Weight = 1 },
            new() { Vs = ["c"], I = 2 },
            new() { Vs = ["d"], I = 3, Barycenter = 3, Weight = 1 }
        };

        var result = Sort.Run(input);
        await Assert.That(Join(result.Vs)).IsEqualTo("a,d,c,b");
        await Assert.That(result.Barycenter!.Value).IsEqualTo((2 + 6 + 3) / 3.0).Within(0.001);
        await Assert.That(result.Weight!.Value).IsEqualTo(3).Within(0.001);
    }

    [Test]
    public async Task CanHandleNoBarycentersForAnyNodes()
    {
        var input = new List<ResolvedEntry>
        {
            new() { Vs = ["a"], I = 0 },
            new() { Vs = ["b"], I = 3 },
            new() { Vs = ["c"], I = 2 },
            new() { Vs = ["d"], I = 1 }
        };

        var result = Sort.Run(input);
        await Assert.That(Join(result.Vs)).IsEqualTo("a,d,c,b");
        await Assert.That(result.Barycenter).IsNull();
        await Assert.That(result.Weight).IsNull();
    }

    [Test]
    public async Task CanHandleABarycenterOf0()
    {
        var input = new List<ResolvedEntry>
        {
            new() { Vs = ["a"], I = 0, Barycenter = 0, Weight = 1 },
            new() { Vs = ["b"], I = 3 },
            new() { Vs = ["c"], I = 2 },
            new() { Vs = ["d"], I = 1 }
        };

        var result = Sort.Run(input);
        await Assert.That(Join(result.Vs)).IsEqualTo("a,d,c,b");
        await Assert.That(result.Barycenter!.Value).IsEqualTo(0).Within(0.001);
        await Assert.That(result.Weight!.Value).IsEqualTo(1).Within(0.001);
    }
}
