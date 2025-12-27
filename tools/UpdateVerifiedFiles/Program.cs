using System.Text;
using System.Text.RegularExpressions;

var testsPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "MermaidSharp.Tests"));
Console.WriteLine($"Tests path: {testsPath}");

// Find all test files
var testFiles = Directory.GetFiles(testsPath, "*Tests.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains("obj") && !f.Contains("bin"))
    .ToList();

Console.WriteLine($"Found {testFiles.Count} test files");

var httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromSeconds(30);

foreach (var testFile in testFiles)
{
    Console.WriteLine($"\nProcessing: {Path.GetFileName(testFile)}");
    var content = await File.ReadAllTextAsync(testFile);

    // Find all test methods with Mermaid.Render(input)
    var testPattern = new Regex(
        @"public\s+(?:async\s+)?Task\s+(\w+)\s*\(\s*\)\s*\{[^}]*?(?:const\s+)?(?:string|var)\s+input\s*=\s*""""""(.*?)""""""[^}]*?Mermaid\.Render\s*\(\s*input\s*\)",
        RegexOptions.Singleline);

    var matches = testPattern.Matches(content);
    Console.WriteLine($"  Found {matches.Count} tests with Mermaid.Render(input)");

    foreach (Match match in matches)
    {
        var testName = match.Groups[1].Value;
        var mermaidCode = match.Groups[2].Value;

        // Normalize the mermaid code - handle raw string literal indentation
        var lines = mermaidCode.Split('\n');
        if (lines.Length > 0)
        {
            // Find minimum indentation (excluding empty lines)
            var minIndent = lines
                .Where(l => l.Trim().Length > 0)
                .Select(l => l.TakeWhile(c => c == ' ' || c == '\t').Count())
                .DefaultIfEmpty(0)
                .Min();

            // Remove common indentation
            mermaidCode = string.Join("\n", lines.Select(l =>
                l.Length >= minIndent ? l.Substring(Math.Min(minIndent, l.Length)) : l));
        }

        mermaidCode = mermaidCode.Trim();

        // Determine verified file path
        var testDir = Path.GetDirectoryName(testFile)!;
        var testClassName = Path.GetFileNameWithoutExtension(testFile);
        var verifiedFileName = $"{testClassName}.{testName}.verified.svg";
        var verifiedPath = Path.Combine(testDir, verifiedFileName);

        Console.WriteLine($"    {testName} -> {verifiedFileName}");

        // Encode for mermaid.ink
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(mermaidCode))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var url = $"https://mermaid.ink/svg/{base64}";

        // Retry logic for rate limiting
        for (int retry = 0; retry < 3; retry++)
        {
            try
            {
                var svg = await httpClient.GetStringAsync(url);
                await File.WriteAllTextAsync(verifiedPath, svg);
                Console.WriteLine($"      ✓ Updated");
                break;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                Console.WriteLine($"      Retry {retry + 1}/3 (503 Service Unavailable)...");
                await Task.Delay(2000 * (retry + 1)); // Increasing backoff
                if (retry == 2)
                {
                    Console.WriteLine($"      ✗ Failed after retries");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ✗ Error: {ex.Message}");
                break;
            }
        }

        // Longer delay to avoid rate limiting
        await Task.Delay(500);
    }
}

Console.WriteLine("\nDone!");
