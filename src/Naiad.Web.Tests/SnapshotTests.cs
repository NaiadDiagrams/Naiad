// Each test drives the real published WASM app in a browser page; run them one at a time so several
// runtime boots don't contend on a loaded CI runner.
[NotInParallel]
public class SnapshotTests
{
    static WebApplication? app;
    static int port;
    static IPlaywright? playwright;
    static IBrowser? browser;

    [Before(Class)]
    public static async Task OneTimeSetUp()
    {
        port = GetAvailablePort();

        // Serve the pre-published output produced by the PublishBlazorForTests build target.
        var testAssemblyDir = Path.GetDirectoryName(typeof(SnapshotTests).Assembly.Location)!;
        var wwwrootPath = Path.Combine(testAssemblyDir, "..", "blazor-publish", "wwwroot");

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://localhost:{port}");
        builder.Logging.ClearProviders();

        app = builder.Build();

        var contentTypeProvider = new FileExtensionContentTypeProvider
        {
            Mappings =
            {
                [".wasm"] = "application/wasm"
            }
        };

        var fileProvider = new PhysicalFileProvider(wwwrootPath);

        app.UseDefaultFiles(
            new DefaultFilesOptions
            {
                FileProvider = fileProvider
            });
        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = fileProvider,
                ContentTypeProvider = contentTypeProvider,
                ServeUnknownFileTypes = true
            });

        app.MapFallbackToFile(
            "index.html",
            new StaticFileOptions
            {
                FileProvider = fileProvider
            });

        await app.StartAsync();

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync();
    }

    [After(Class)]
    public static async Task OneTimeTearDown()
    {
        if (browser != null)
        {
            await browser.CloseAsync();
        }

        playwright?.Dispose();

        if (app != null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    [Test]
    public async Task HomePage()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync($"http://localhost:{port}/");

        await SettleAsync(page);

        await Verify(page);
    }

    [Test]
    public async Task HomePageMobile()
    {
        var page = await browser!.NewPageAsync();
        await page.SetViewportSizeAsync(375, 667); // iPhone SE size

        await page.GotoAsync($"http://localhost:{port}/");

        await SettleAsync(page);

        await VerifyViewportAsync(page);
    }

    [Test]
    public async Task HomePageDarkMode()
    {
        var page = await browser!.NewPageAsync();

        await page.GotoAsync($"http://localhost:{port}/");

        // Set dark theme in localStorage, then reload so Blazor boots into it.
        await page.EvaluateAsync("() => localStorage.setItem('selectedTheme', 'Dark')");
        await page.ReloadAsync();

        await SettleAsync(page);

        await Verify(page);
    }

    [Test]
    public async Task HomePageDarkModeMobile()
    {
        var page = await browser!.NewPageAsync();
        await page.SetViewportSizeAsync(375, 667); // iPhone SE size

        await page.GotoAsync($"http://localhost:{port}/");

        await page.EvaluateAsync("() => localStorage.setItem('selectedTheme', 'Dark')");
        await page.ReloadAsync();

        await SettleAsync(page);

        await VerifyViewportAsync(page);
    }

    // End-to-end on the real runtime: editing the source re-renders the preview. Typing a pie chart swaps
    // the default flowchart for an SVG the renderer tags as a pie, proving the live edit path works.
    [Test]
    public async Task TypingRendersPreview()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync($"http://localhost:{port}/");
        await SettleAsync(page);

        await page.FillAsync(".code-editor", "pie\n    \"A\" : 10\n    \"B\" : 20");

        var svg = await page.WaitForSelectorAsync(
            ".preview-content svg[aria-roledescription='pie']",
            new()
            {
                Timeout = 30000
            });

        await Assert.That(svg).IsNotNull();
    }

    // Picking a sample from the dropdown loads its source and renders it.
    [Test]
    public async Task SampleRendersPreview()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync($"http://localhost:{port}/");
        await SettleAsync(page);

        await page.SelectOptionAsync(".sample-select", new SelectOptionValue { Value = "Pie" });

        var svg = await page.WaitForSelectorAsync(
            ".preview-content svg[aria-roledescription='pie']",
            new()
            {
                Timeout = 30000
            });

        await Assert.That(svg).IsNotNull();
    }

    // The SVG download button hands the rendered markup to the browser as a file.
    [Test]
    public async Task DownloadingSvgSavesFile()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync($"http://localhost:{port}/");
        await SettleAsync(page);

        var download = await page.RunAndWaitForDownloadAsync(
            () => page.ClickAsync(".toolbar-btn[title='Download SVG']"),
            new()
            {
                Timeout = 30000
            });

        await Assert.That(download.SuggestedFilename).EndsWith(".svg");
    }

    // The PNG button rasterizes the rendered SVG in a canvas and downloads the result.
    [Test]
    public async Task DownloadingPngSavesFile()
    {
        var page = await browser!.NewPageAsync();
        await page.GotoAsync($"http://localhost:{port}/");
        await SettleAsync(page);

        var download = await page.RunAndWaitForDownloadAsync(
            () => page.ClickAsync(".toolbar-btn[title='Download PNG']"),
            new()
            {
                Timeout = 30000
            });

        await Assert.That(download.SuggestedFilename).EndsWith(".png");
    }

    // Verifies a fixed-size viewport screenshot rather than Verify(page)'s full-page capture. Mobile pages
    // scroll, so a full-page screenshot's height is content- (hence font-) driven and differs across OSes
    // (local Windows vs the Linux CI runner) — which fails Verify on a dimension mismatch before the SSIM
    // tolerance can absorb font-rendering differences. The viewport is a stable size on every platform.
    // (Desktop pages are viewport-locked, so they keep the richer Verify(page) png + html capture.)
    static async Task VerifyViewportAsync(IPage page)
    {
        var screenshot = await page.ScreenshotAsync(new() { FullPage = false });
        await Verify(screenshot, "png");
    }

    // Waits for the app to settle before a snapshot: the editor present, the default diagram rendered,
    // every asset loaded, the theme label agreeing with data-theme, and web fonts ready — so the capture
    // is the deterministic settled page rather than a mid-boot frame.
    static async Task SettleAsync(IPage page)
    {
        await page.WaitForSelectorAsync(".code-editor");
        await page.WaitForSelectorAsync(
            ".preview-content svg",
            new()
            {
                Timeout = 60000
            });
        await page.WaitForLoadStateAsync(
            LoadState.NetworkIdle,
            new()
            {
                Timeout = 120000
            });
        // The theme toggle's label is driven by an async preference load, so wait for it to agree with the
        // active data-theme — otherwise a dark-theme screenshot can catch the pre-flip label.
        await page.WaitForFunctionAsync(
            """
            () => {
                const dark = document.documentElement.getAttribute('data-theme') === 'dark';
                const button = document.querySelector('.theme-toggle-btn');
                return button && (dark ? button.textContent.includes('Light') : button.textContent.includes('Dark'));
            }
            """);
        await page.EvaluateAsync("() => document.fonts.ready");
    }

    static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint) listener.LocalEndpoint).Port;
    }
}
