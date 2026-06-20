namespace Naiad.Web.Services;

public enum ThemeType
{
    Light,
    Dark
}

public class ThemePreferenceService(IJSRuntime jsRuntime)
{
    const string ThemeKey = "selectedTheme";

    public async Task<ThemeType> GetSavedThemeAsync()
    {
        var value = await jsRuntime.InvokeAsync<string?>("statePreference.get", ThemeKey);
        if (value is not null && Enum.TryParse<ThemeType>(value, out var theme))
        {
            return theme;
        }

        return ThemeType.Light;
    }

    public async Task SaveThemeAsync(ThemeType theme) =>
        await jsRuntime.InvokeVoidAsync("statePreference.set", ThemeKey, theme.ToString());
}
