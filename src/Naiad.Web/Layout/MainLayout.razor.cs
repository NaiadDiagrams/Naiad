namespace Naiad.Web.Layout;

public partial class MainLayout
{
    ThemeType currentTheme = ThemeType.Light;
    string? userAgent;
    DownloadSize? downloadSize;
    long? ramBytes;

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

        var ram = await JSRuntime.InvokeAsync<long>("appInfo.ramBytes");
        if (ram > 0)
        {
            ramBytes = ram;
        }

        StateHasChanged();
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
}
