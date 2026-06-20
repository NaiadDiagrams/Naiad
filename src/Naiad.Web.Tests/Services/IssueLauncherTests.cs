public class IssueLauncherTests
{
    [Test]
    public async Task ForException_BuildsPrefilledIssueUrl()
    {
        // A constructed (never thrown) exception has a null stack trace, so ToString() is deterministic.
        var exception = new InvalidOperationException("boom");

        var url = IssueLauncher.ForException("Render diagram", exception, "* Naiad version: 1.2.3", "flowchart LR\n A --> B");

        await Assert.That(url).StartsWith("https://github.com/Papyrine/Naiad/issues/new?title=");

        var decoded = WebUtility.UrlDecode(url);
        await Assert.That(decoded).Contains("Render diagram: InvalidOperationException");
        await Assert.That(decoded).Contains("* Action: Render diagram");
        await Assert.That(decoded).Contains("* Naiad version: 1.2.3");
        await Assert.That(decoded).Contains("* Diagram source:");
        await Assert.That(decoded).Contains("flowchart LR");
        await Assert.That(decoded).Contains("InvalidOperationException: boom");
    }

    [Test]
    public async Task ForException_WithoutEnvironmentOrSource_OmitsThoseBlocks()
    {
        var url = IssueLauncher.ForException("Boom", new("oops"));

        var decoded = WebUtility.UrlDecode(url);
        await Assert.That(decoded).Contains("* Action: Boom");
        await Assert.That(decoded).DoesNotContain("* Naiad version:");
        await Assert.That(decoded).DoesNotContain("* Diagram source:");
    }
}
