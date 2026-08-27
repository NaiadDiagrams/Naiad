// The suite shares process-global state (IconPackRegistry, reset per test) and was written for
// sequential execution, so run the whole assembly serially rather than TUnit's default parallelism.
[assembly: NotInParallel]

public static partial class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // Normalize floating point values to 4 decimal places for visual equivalence
        VerifierSettings.AddScrubber(NormalizeFloatingPoint);
        VerifierSettings.UseSsimForPng();
        VerifierSettings.InitializePlugins();
    }

    static void NormalizeFloatingPoint(StringBuilder builder)
    {
        var content = builder.ToString();
        var normalized = FloatRegex().Replace(content, match =>
        {
            var value = double.Parse(match.Value, CultureInfo.InvariantCulture);
            var rounded = Math.Round(value, 4);
            return rounded.ToString("0.####", CultureInfo.InvariantCulture);
        });
        builder.Clear();
        builder.Append(normalized);
    }

    [GeneratedRegex(@"-?\d+\.\d{5,}")]
    private static partial Regex FloatRegex();
}
