namespace Naiad.Web.Pages;

public partial class Index : IDisposable
{
    // Re-render this long after the last keystroke. Keeps a fast typist from triggering a parse on every
    // character while still feeling live.
    const int debounceMilliseconds = 250;

    // Device-pixel multiplier for the PNG export, so downloaded rasters are crisp on high-DPI displays.
    const double pngScale = 2;

    string source = DiagramSamples.Default.Source;
    DiagramRenderResult result = DiagramRenderResult.Empty;
    string? issueUrl;
    string? userAgent;
    bool isExporting;
    CancellationTokenSource? debounce;

    // Render once synchronously so the first paint already shows the default sample's diagram.
    protected override void OnInitialized() =>
        Render();

    protected override async Task OnInitializedAsync() =>
        userAgent = await JsRuntime.InvokeAsync<string?>("appInfo.userAgent");

    async Task OnInput(ChangeEventArgs args)
    {
        source = args.Value?.ToString() ?? "";

        // Coalesce bursts of keystrokes: cancel the pending render and start a fresh timer. Only the
        // last keystroke in a burst survives to call Render.
        debounce?.Cancel();
        debounce?.Dispose();
        var cancellation = new CancellationTokenSource();
        debounce = cancellation;

        try
        {
            await Task.Delay(debounceMilliseconds, cancellation.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        Render();
    }

    void LoadSample(DiagramSample sample)
    {
        // A picked sample replaces the editor content and renders immediately — no debounce wait.
        debounce?.Cancel();
        source = sample.Source;
        Render();
    }

    void Render()
    {
        result = DiagramRenderer.Render(source);
        issueUrl = result.Unexpected is { } exception
            ? IssueLauncher.ForException("Render diagram", exception, AppInfo.Environment(userAgent), source)
            : null;
    }

    async Task DownloadSvg()
    {
        if (result.Svg is { } svg)
        {
            await FileDownloadService.DownloadTextAsync("diagram.svg", "image/svg+xml", svg);
        }
    }

    async Task DownloadPng()
    {
        if (!result.HasSvg)
        {
            return;
        }

        isExporting = true;
        try
        {
            // Rasterize a self-contained SVG (native text, no foreignObject) in the browser's canvas, then
            // hand the resulting PNG bytes to the download. Best-effort: an export failure leaves the
            // preview untouched rather than surfacing an error.
            var exportable = DiagramRenderer.RenderForPng(source);
            var base64 = await JsRuntime.InvokeAsync<string>("diagramExport.svgToPng", exportable, pngScale);
            await FileDownloadService.DownloadAsync("diagram.png", "image/png", Convert.FromBase64String(base64));
        }
        catch
        {
            // Swallow: PNG export is a convenience over the always-available SVG download.
        }
        finally
        {
            isExporting = false;
        }
    }

    public void Dispose()
    {
        debounce?.Cancel();
        debounce?.Dispose();
    }
}
