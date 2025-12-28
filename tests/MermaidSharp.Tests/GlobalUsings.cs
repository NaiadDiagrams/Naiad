// Global using directives

global using MermaidSharp;
global using System.Runtime.CompilerServices;
global using System.Text;
global using System.Text.RegularExpressions;

public static partial class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // Configure Verify to use UTF-8 without BOM
        VerifierSettings.UseUtf8NoBom();

        // Normalize floating point values to 4 decimal places for visual equivalence
        VerifierSettings.AddScrubber(NormalizeFloatingPoint);
    }

    static void NormalizeFloatingPoint(StringBuilder sb)
    {
        var content = sb.ToString();
        var normalized = FloatRegex().Replace(content, match =>
        {
            var value = double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture);
            var rounded = Math.Round(value, 4);
            return rounded.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        });
        sb.Clear();
        sb.Append(normalized);
    }

    [GeneratedRegex(@"-?\d+\.\d{5,}")]
    private static partial Regex FloatRegex();
}
