using EmptyFiles;

public static partial class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // Normalize floating point values to 4 decimal places for visual equivalence
        VerifierSettings.AddScrubber(NormalizeFloatingPoint);

        VerifyImageSharpCompare.RegisterComparers(threshold: 1000);

        VerifierSettings.InitializePlugins();
    }

    static void NormalizeFloatingPoint(StringBuilder builder)
    {
        var content = builder.ToString();
        var normalized = FloatRegex().Replace(content, match =>
        {
            var value = double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture);
            var rounded = Math.Round(value, 4);
            return rounded.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
        });
        builder.Clear();
        builder.Append(normalized);
    }

    [GeneratedRegex(@"-?\d+\.\d{5,}")]
    private static partial Regex FloatRegex();
}
