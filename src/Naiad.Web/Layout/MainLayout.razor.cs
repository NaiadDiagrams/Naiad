namespace Naiad.Web.Layout;

public partial class MainLayout : IDisposable
{
    ThemeType currentTheme = ThemeType.Light;
    string? userAgent;
    DownloadSize? downloadSize;
    long? ramBytes;
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

        // The WebAssembly heap grows as diagrams are parsed and rendered — and WASM linear memory never
        // shrinks back — so the boot-time figure understates the real footprint once any work is done. Poll
        // it so the footer tracks that high-water mark, repainting only when the number actually moves (so
        // the poll costs nothing once the heap plateaus).
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
                    var previous = ramBytes;
                    await SampleRamAsync();
                    if (ramBytes != previous)
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
        var ram = await JSRuntime.InvokeAsync<long>("appInfo.ramBytes");
        if (ram > 0)
        {
            ramBytes = ram;
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
