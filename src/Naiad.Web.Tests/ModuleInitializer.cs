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
        var playwrightExit = Program.Main(["install", "chromium"]);
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

        // The page-HTML snapshots are compared with Verify.Bunit's AngleSharp markup comparer, which throws
        // NullReferenceException whenever an element's `style` attribute can't be read back as
        // IHtmlElement.Style. A Playwright page capture contains two such cases, so normalise both before the
        // comparison runs:
        //
        //   * The rendered diagram is inline SVG. SVG nodes are ISvgElement (their IHtmlElement.Style is null),
        //     so a styled node like `<svg style="max-width: ...">` trips the NRE on every run once a baseline
        //     exists. The diagram's geometry is already snapshotted exhaustively by the core renderer tests and
        //     captured visually by the page PNG, so collapse it to a placeholder — that drops the styled SVG
        //     nodes and keeps this structure snapshot stable when the diagram's layout numbers change.
        //
        //   * Playwright hides the text caret while it screenshots the page by toggling
        //     `caret-color: transparent !important` on the editable <textarea>. The concurrent page.content()
        //     capture therefore races between `style="caret-color: ..."`, an emptied `style=""`, and no
        //     attribute — and the emptied form trips the same NRE. It's a screenshot artifact, not app state,
        //     so strip the textarea's style attribute outright.
        VerifierSettings.AddScrubber(
            "html",
            builder =>
            {
                var html = Regex.Replace(
                    builder.ToString(),
                    "<svg\\b[^>]*\\bid=\"naiad\"[^>]*>.*?</svg>",
                    "<svg id=\"naiad\"><!-- diagram scrubbed --></svg>",
                    RegexOptions.Singleline);
                html = Regex.Replace(
                    html,
                    "(<textarea\\b[^>]*?) style=\"[^\"]*\"",
                    "$1");
                // The status bar carries figures that vary run to run (render time) or with the diagram's
                // layout numbers — the same numbers the SVG above is already scrubbed for (dimensions, byte
                // size). Pin every value tagged status-volatile so the structure snapshot stays stable; the
                // diagram type is stable and left to assert.
                html = Regex.Replace(
                    html,
                    "(<span class=\"[^\"]*\\bstatus-volatile\\b[^\"]*\">).*?(</span>)",
                    "$1scrubbed$2");
                // The footer version comes from AssemblyInformationalVersion, which the SDK suffixes with the
                // build's git commit SHA — so it changes on every commit. Pin it to keep the snapshot stable.
                html = Regex.Replace(
                    html,
                    "(<span class=\"footer-version\">).*?(</span>)",
                    "$1scrubbed$2");
                // The footer download total is measured from the browser's Resource Timing data, so it shifts
                // with the published bundle's byte size. Pin it too.
                html = Regex.Replace(
                    html,
                    "(<span class=\"footer-size\"[^>]*>).*?(</span>)",
                    "$1scrubbed$2");
                // The footer RAM figure is the live WebAssembly heap size, which varies run to run. Pin it too.
                html = Regex.Replace(
                    html,
                    "(<span class=\"footer-ram\"[^>]*>).*?(</span>)",
                    "$1scrubbed$2");
                builder.Clear();
                builder.Append(html);
            });
    }
}
