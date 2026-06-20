namespace Naiad.Web.Layout;

public partial class MainLayout
{
    ThemeType currentTheme = ThemeType.Light;
    string? userAgent;

    protected override async Task OnInitializedAsync()
    {
        currentTheme = await ThemePreferenceService.GetSavedThemeAsync();
        await JSRuntime.InvokeVoidAsync("themeManager.applyTheme", currentTheme.ToString());
        userAgent = await JSRuntime.InvokeAsync<string?>("appInfo.userAgent");
    }

    async Task HandleThemeChanged(ThemeType newTheme)
    {
        currentTheme = newTheme;
        await ThemePreferenceService.SaveThemeAsync(newTheme);
        await JSRuntime.InvokeVoidAsync("themeManager.applyTheme", newTheme.ToString());
    }
}
