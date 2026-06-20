public class ThemePreferenceServiceTests : BunitTestContext
{
    [Test]
    public async Task GetSavedThemeAsync_WithSavedTheme_ReturnsTheme()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string?>("statePreference.get", "selectedTheme").SetResult("Dark");

        var service = new ThemePreferenceService(JSInterop.JSRuntime);
        var theme = await service.GetSavedThemeAsync();

        await Assert.That(theme).IsEqualTo(ThemeType.Dark);
    }

    [Test]
    public async Task GetSavedThemeAsync_WithNoSavedTheme_ReturnsLight()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string?>("statePreference.get", "selectedTheme").SetResult(null);

        var service = new ThemePreferenceService(JSInterop.JSRuntime);
        var theme = await service.GetSavedThemeAsync();

        await Assert.That(theme).IsEqualTo(ThemeType.Light);
    }

    [Test]
    public async Task GetSavedThemeAsync_WithInvalidValue_ReturnsLight()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string?>("statePreference.get", "selectedTheme").SetResult("InvalidTheme");

        var service = new ThemePreferenceService(JSInterop.JSRuntime);
        var theme = await service.GetSavedThemeAsync();

        await Assert.That(theme).IsEqualTo(ThemeType.Light);
    }

    [Test]
    public async Task SaveThemeAsync_SavesThemeValue()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("statePreference.set", "selectedTheme", "Dark").SetVoidResult();

        var service = new ThemePreferenceService(JSInterop.JSRuntime);
        await service.SaveThemeAsync(ThemeType.Dark);

        var invocations = JSInterop.Invocations["statePreference.set"];
        await Assert.That(invocations.Count).IsEqualTo(1);
        await Assert.That(invocations[0].Arguments[0]).IsEqualTo("selectedTheme");
        await Assert.That(invocations[0].Arguments[1]).IsEqualTo("Dark");
    }
}
