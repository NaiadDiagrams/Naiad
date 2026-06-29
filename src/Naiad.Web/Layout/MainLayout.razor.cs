namespace Naiad.Web.Layout;

public partial class MainLayout : IDisposable
{
    ThemeType currentTheme = ThemeType.Light;
    string? userAgent;
    DownloadSize? downloadSize;
    long? liveBytes;
    long? peakBytes;
    PeriodicTimer? ramPoll;

    protected override async Task OnInitializedAsync()
    {
        currentTheme = await ThemePreferenceService.GetSavedThemeAsync();
        await JSRuntime.InvokeVoidAsync("themeManager.applyTheme", currentTheme.ToString());
        userAgent = await JSRuntime.InvokeAsync<string?>("appInfo.userAgent");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // appInfo.downloadSize waits for the load event before totalling the boot download, so resolve it
        // off the first render rather than during init to avoid blocking the initial paint behind it.
        downloadSize = await JSRuntime.InvokeAsync<DownloadSize>("appInfo.downloadSize");

        await SampleRamAsync();
        StateHasChanged();

        // The managed heap rises and falls as diagrams are parsed and rendered, and the WebAssembly arena it
        // lives in grows to fit each peak and never shrinks back — so a single boot-time figure tells you
        // little. Poll both: the live managed heap "now" and that never-shrinking high-water mark, repainting
        // only when a figure actually moves (so the poll costs nothing while the app sits idle).
        var poll = new PeriodicTimer(TimeSpan.FromSeconds(2));
        ramPoll = poll;
        _ = PollRamAsync(poll);
    }

    async Task PollRamAsync(PeriodicTimer timer)
    {
        try
        {
            while (await timer.WaitForNextTickAsync())
            {
                // Hop back onto the renderer's dispatcher: the tick resumes on a pool thread, but JS interop
                // and StateHasChanged must run on the UI thread.
                await InvokeAsync(async () =>
                {
                    var previous = (liveBytes, peakBytes);
                    await SampleRamAsync();
                    if ((liveBytes, peakBytes) != previous)
                    {
                        StateHasChanged();
                    }
                });
            }
        }
        catch (ObjectDisposedException)
        {
            // Disposed mid-poll (page torn down); stop quietly.
        }
    }

    async Task SampleRamAsync()
    {
        // Live managed heap: the bytes .NET currently holds in objects. It falls after a collection, so it's
        // the "right now" usage rather than a peak.
        liveBytes = GC.GetTotalMemory(false);

        // Committed WebAssembly linear memory — the whole runtime's arena (managed heap + code + thread
        // stacks + GC headroom). WASM memory only ever grows, so this is already the high-water mark; take
        // the max anyway to stay honest on the JS-heap fallback path (interop.js), which can shrink.
        var sample = await JSRuntime.InvokeAsync<long>("appInfo.ramBytes");
        if (sample > 0)
        {
            peakBytes = peakBytes is { } previousPeak ? Math.Max(previousPeak, sample) : sample;
        }
    }

    static string FormatMb(long bytes) =>
        $"{bytes / (1024d * 1024d):0.0} MB";

    readonly record struct DownloadSize(long Zipped, long Unzipped);

    async Task HandleThemeChanged(ThemeType newTheme)
    {
        currentTheme = newTheme;
        await ThemePreferenceService.SaveThemeAsync(newTheme);
        await JSRuntime.InvokeVoidAsync("themeManager.applyTheme", newTheme.ToString());
    }

    public void Dispose() =>
        ramPoll?.Dispose();
}
