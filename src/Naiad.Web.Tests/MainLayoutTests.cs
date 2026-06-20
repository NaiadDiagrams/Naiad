public class MainLayoutTests : BunitTestContext
{
    public MainLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<ThemePreferenceService>();
    }

    [Test]
    public async Task UnhandledError_InChild_ShowsIssuePrompt()
    {
        RenderFragment body = builder =>
        {
            builder.OpenComponent<ThrowingComponent>(0);
            builder.CloseComponent();
        };

        var cut = Render<MainLayout>(_ => _
            .Add(_ => _.Body, body));

        // The ErrorBoundary catches the child's exception and surfaces the pre-filled GitHub issue link.
        var link = cut.Find(".error-report a");
        await Assert.That(link.GetAttribute("href")).StartsWith("https://github.com/Papyrine/Naiad/issues/new?");
    }

    [Test]
    public async Task NoError_RendersBodyWithoutPrompt()
    {
        RenderFragment body = builder => builder.AddContent(0, "all good");

        var cut = Render<MainLayout>(_ => _
            .Add(_ => _.Body, body));

        await Assert.That(cut.Markup).Contains("all good");
        await Assert.That(cut.FindAll(".error-report")).IsEmpty();
    }

    [Test]
    public async Task Header_LinksToRepository()
    {
        RenderFragment body = builder => builder.AddContent(0, "");

        var cut = Render<MainLayout>(_ => _
            .Add(_ => _.Body, body));

        var link = cut.Find(".header-link");
        await Assert.That(link.GetAttribute("href")).IsEqualTo("https://github.com/Papyrine/Naiad");
    }

    class ThrowingComponent : ComponentBase
    {
        protected override void OnInitialized() =>
            throw new InvalidOperationException("kaboom");
    }
}
