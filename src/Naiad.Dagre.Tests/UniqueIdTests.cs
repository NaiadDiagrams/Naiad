using System.Text.RegularExpressions;

namespace Naiad.Dagre.Tests;

// Port of dagre's test/unique-id-test.ts.
public class UniqueIdTests
{
    [Test]
    public async Task UniqueIdNameGeneratesAValidIdentifier()
    {
        // This test guards against a bug #477, where the call to toString(prefix) inside
        // uniqueId() produced [object undefined].
        var id = Util.UniqueId("_root");
        await Assert.That(id).DoesNotContain("[object undefined]");
        await Assert.That(Regex.IsMatch(id, @"_root\d+")).IsTrue();
    }

    [Test]
    public async Task CallingUniqueIdNameMultipleTimesGenerateDistinctValues()
    {
        var first = Util.UniqueId("name");
        var second = Util.UniqueId("name");
        var third = Util.UniqueId("name");
        await Assert.That(first).IsNotEqualTo(second);
        await Assert.That(second).IsNotEqualTo(third);
    }

    [Test]
    public async Task CallingUniqueIdNumberWithANumberCreatesAValidIdentifierString()
    {
        var id = Util.UniqueId("99");
        await Assert.That(id).IsTypeOf<string>();

        await Assert.That(Regex.IsMatch(id, @"99\d+")).IsTrue();
    }
}
