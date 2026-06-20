using System.Text.RegularExpressions;

static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyPlaywright.Initialize(installPlaywright: true);
        VerifierSettings.UseSsimForPng(.7);
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
