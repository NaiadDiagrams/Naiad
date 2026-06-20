using System.Text.RegularExpressions;

static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyPlaywright.Initialize();
        // These tests only ever launch Chromium, so install just that. Initialize(installPlaywright: true)
        // would fetch Chromium, Firefox and WebKit (~300 MB of unused browsers) and trip the Firefox/WebKit
        // host-dependency validation warning on CI runners that lack their native libs.
        var playwrightExit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (playwrightExit != 0)
        {
            throw new InvalidOperationException($"Playwright Chromium install failed (exit code {playwrightExit}).");
        }

        VerifierSettings.UseSsimForPng(.6);
        VerifierSettings.InitializePlugins();

        // bUnit stamps a fresh element-reference GUID on some elements each render; pin it so component
        // snapshots stay stable. Only matches the bUnit attribute, so Playwright/text snapshots are untouched.
        VerifierSettings.ScrubLinesWithReplace(_ =>
            Regex.Replace(
                _,
                "blazor:elementreference=\"[^\"]*\"",
                "blazor:elementreference=\"scrubbed\"",
                RegexOptions.IgnoreCase));
    }
}
