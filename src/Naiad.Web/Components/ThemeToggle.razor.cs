namespace Naiad.Web.Components;

public partial class ThemeToggle
{
    [Parameter]
    public ThemeType CurrentTheme { get; set; } = ThemeType.Light;

    [Parameter]
    public EventCallback<ThemeType> OnThemeChanged { get; set; }

    Task ToggleTheme()
    {
        var newTheme = CurrentTheme == ThemeType.Light ? ThemeType.Dark : ThemeType.Light;
        return OnThemeChanged.InvokeAsync(newTheme);
    }
}
