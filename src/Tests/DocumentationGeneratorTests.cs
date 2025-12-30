public class DocGeneratorTests
{
    [Test]
    [Explicit]
    public async Task Generate()
    {
        var testsDir = ProjectFiles.ProjectDirectory;
        var outputPath = Path.Combine(ProjectFiles.SolutionDirectory, "test-cases.include.md");

        var markdown = new StringBuilder();
        markdown.AppendLine("# MermaidSharp Test Examples");
        markdown.AppendLine();
        markdown.AppendLine("This document is auto-generated from the test suite.");
        markdown.AppendLine();

        var testFiles = Directory.GetFiles(testsDir, "*Tests.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("DocumentationGenerator"))
            .OrderBy(f => f);

        foreach (var testFile in testFiles)
        {
            var tests = await ExtractTestsFromFile(testFile);
            if (tests.Count == 0) continue;

            var relativePath = Path.GetRelativePath(testsDir, testFile);
            var category = Path.GetDirectoryName(relativePath)?.Replace("\\", "/") ?? "";

            markdown.AppendLine($"## {category}");
            markdown.AppendLine();

            foreach (var test in tests)
            {
                markdown.AppendLine($"### {test.Name}");
                markdown.AppendLine();
                markdown.AppendLine("**Input:**");
                markdown.AppendLine("```mermaid");
                markdown.AppendLine(test.Input.Trim());
                markdown.AppendLine("```");
                markdown.AppendLine();

                if (!string.IsNullOrEmpty(test.VerifiedSvgContent))
                {
                    markdown.AppendLine("**Output SVG:**");
                    markdown.AppendLine("```xml");
                    markdown.AppendLine(test.VerifiedSvgContent.Trim());
                    markdown.AppendLine("```");
                    markdown.AppendLine();
                }
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, markdown.ToString());

        Console.WriteLine($"Generated documentation at: {outputPath}");
    }

    static async Task<List<TestInfo>> ExtractTestsFromFile(string filePath)
    {
        var results = new List<TestInfo>();
        var code = await File.ReadAllTextAsync(filePath);

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.AttributeLists
                .SelectMany(a => a.Attributes)
                .Any(a => a.Name.ToString() == "Test"));

        foreach (var method in methods)
        {
            var body = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? "";

            if (!body.Contains("VerifySvg")) continue;

            var input = ExtractInputString(method);
            if (string.IsNullOrEmpty(input)) continue;

            var testName = method.Identifier.Text;
            var className = method.Ancestors()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault()?.Identifier.Text ?? "";

            var verifiedSvgPath = FindVerifiedFile(filePath, className, testName, ".verified.svg");
            var verifiedSvgContent = verifiedSvgPath != null ? await File.ReadAllTextAsync(verifiedSvgPath) : null;

            results.Add(new TestInfo
            {
                Name = testName,
                ClassName = className,
                Input = input,
                VerifiedSvgContent = verifiedSvgContent
            });
        }

        return results;
    }

    private static string? ExtractInputString(MethodDeclarationSyntax method)
    {
        // Find raw string literals (""" ... """)
        var rawStrings = method.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(l => l.IsKind(SyntaxKind.Utf8StringLiteralExpression) ||
                        l.Token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) ||
                        l.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
            .ToList();

        if (rawStrings.Count > 0)
        {
            var token = rawStrings[0].Token;
            var text = token.ValueText;
            return text;
        }

        // Fall back to regular string literals
        var stringLiterals = method.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(l => l.IsKind(SyntaxKind.StringLiteralExpression))
            .ToList();

        if (stringLiterals.Count > 0)
        {
            return stringLiterals[0].Token.ValueText;
        }

        // Try interpolated strings
        var interpolated = method.DescendantNodes()
            .OfType<InterpolatedStringExpressionSyntax>()
            .FirstOrDefault();

        if (interpolated != null)
        {
            return interpolated.Contents
                .OfType<InterpolatedStringTextSyntax>()
                .Select(c => c.TextToken.ValueText)
                .FirstOrDefault();
        }

        return null;
    }

    static string? FindVerifiedFile(string testFile, string className, string testName, string extension)
    {
        var dir = Path.GetDirectoryName(testFile)!;

        // Try pattern: ClassName.TestName.verified.svg
        var pattern1 = Path.Combine(dir, $"{className}.{testName}{extension}");
        if (File.Exists(pattern1)) return pattern1;

        // Try without class name
        var pattern2 = Path.Combine(dir, $"{testName}{extension}");
        if (File.Exists(pattern2)) return pattern2;

        // Search in subdirectories
        var searchPattern = $"*{testName}{extension}";
        var found = Directory.GetFiles(dir, searchPattern, SearchOption.AllDirectories).FirstOrDefault();

        return found;
    }

    private class TestInfo
    {
        public string Name { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string Input { get; set; } = "";
        public string? VerifiedSvgContent { get; set; }
    }
}
