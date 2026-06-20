public class ErrorPanelTests : BunitTestContext
{
    [Test]
    public async Task Render_ShowsMessage()
    {
        var cut = Render<ErrorPanel>(_ => _
            .Add(_ => _.Message, "Failed to parse pie chart"));

        await Assert.That(cut.Find(".error-message").TextContent).IsEqualTo("Failed to parse pie chart");
    }

    [Test]
    public async Task Render_WithoutIssueUrl_OmitsReportPrompt()
    {
        var cut = Render<ErrorPanel>(_ => _
            .Add(_ => _.Message, "Failed to parse pie chart"));

        await Assert.That(cut.FindAll(".error-report")).IsEmpty();
    }

    [Test]
    public async Task Render_WithIssueUrl_ShowsReportLink()
    {
        var cut = Render<ErrorPanel>(_ => _
            .Add(_ => _.Message, "Something went wrong")
            .Add(_ => _.IssueUrl, "https://github.com/Papyrine/Naiad/issues/new?title=x"));

        var link = cut.Find(".error-report a");
        await Assert.That(link.GetAttribute("href")).StartsWith("https://github.com/Papyrine/Naiad/issues/new");
        await Assert.That(link.GetAttribute("target")).IsEqualTo("_blank");
    }
}
