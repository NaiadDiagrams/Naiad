public class DocGeneratorTests
{
    [Test]
    [Explicit]
    public async Task Generate()
    {
        var testsDir = ProjectFiles.ProjectDirectory;
        var outputDir = Path.Combine(ProjectFiles.SolutionDirectory, "test-renders");
        Directory.Delete(outputDir, true);
        Directory.CreateDirectory(outputDir);

        var testFiles = Directory.GetFiles(testsDir, "*Tests.cs", SearchOption.AllDirectories)
            .Where(_ => !_.Contains("DocumentationGenerator"))
            .OrderBy(_ => _);

        // Group tests by category
        var testsByCategory = new Dictionary<string, List<TestInfo>>();

        foreach (var testFile in testFiles)
        {
            var tests = await ExtractTestsFromFile(testFile);
            if (tests.Count == 0)
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(testsDir, testFile);
            var category = Path.GetDirectoryName(relativePath)?.Replace("\\", "/");

            // Diagram-type tests live in a per-type subfolder; the folder name is the category.
            // Root-level test files (e.g. AllowHtmlElementsTests) cover cross-cutting features
            // rather than a diagram type, so they don't belong in the renders index.
            if (string.IsNullOrEmpty(category))
            {
                continue;
            }

            if (!testsByCategory.TryGetValue(category, out var value))
            {
                value = [];
                testsByCategory[category] = value;
            }

            value.AddRange(tests);
        }

        // Determine which categories are beta (first test's first line ends with -beta)
        var betaCategories = new HashSet<string>();
        foreach (var (category, tests) in testsByCategory)
        {
            if (tests.Count > 0 && IsBeta(tests[0].Input))
            {
                betaCategories.Add(category);
            }
        }

        // Write one file per category
        foreach (var (category, tests) in testsByCategory)
        {
            var isBeta = betaCategories.Contains(category);
            var markdown = new StringBuilder();
            markdown.AppendLine($"# {category}");
            markdown.AppendLine();

            foreach (var test in tests)
            {
                markdown.AppendLine($"## {test.Name}");
                markdown.AppendLine();

                markdown.AppendLine("**Input:**");
                markdown.AppendLine("```");
                markdown.AppendLine(test.Input);
                markdown.AppendLine("```");

                var relativePngPath = Path
                    .GetRelativePath(outputDir, test.VerifiedPngPath)
                    .Replace("\\", "/");
                markdown.AppendLine("**Rendered by Naiad:**");
                markdown.AppendLine(
                    $"""

                     <p align="center">
                       <img src="{relativePngPath}" />
                     </p>

                     """);

                // Only render mermaid block for non-beta (GitHub doesn't support beta syntax)
                if (!isBeta)
                {
                    markdown.AppendLine("**Rendered by Mermaid:**");
                    markdown.AppendLine("```mermaid");
                    markdown.AppendLine(test.Input);
                    markdown.AppendLine("```");
                }

                markdown.AppendLine();
                markdown.AppendLine($"[Open in Mermaid Live]({GetMermaidLiveUrl(test.Input)})");
                markdown.AppendLine();
            }

            var outputPath = Path.Combine(outputDir, $"{category}.md");
            await WriteWithLfAsync(outputPath, markdown.ToString());
        }

        // Generate index page
        var index = new StringBuilder(
            """
            ## Test Renders

            Auto-generated documentation from the test suite.

            """);

        var stableCategories = testsByCategory.Keys.Where(_ => !betaCategories.Contains(_)).OrderBy(_ => _);
        var betaCategoriesSorted = betaCategories.OrderBy(_ => _);

        foreach (var category in stableCategories)
        {
            index.AppendLine($"- [{category}](/src/test-renders/{category}.md)");
        }

        if (betaCategories.Count > 0)
        {
            index.AppendLine(
                """

                ### Beta diagram types

                """);
            foreach (var category in betaCategoriesSorted)
            {
                index.AppendLine($"- [{category}]({category}.md)");
            }
        }

        var indexPath = Path.Combine(outputDir, "renders.include.md");
        await WriteWithLfAsync(indexPath, index.ToString());
    }

    static Task WriteWithLfAsync(string path, string content)
    {
        var lfContent = content.ReplaceLineEndings("\n");
        return File.WriteAllTextAsync(path, lfContent);
    }

    static bool IsBeta(string input)
    {
        var firstLine = input.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        return firstLine.TrimEnd().EndsWith("-beta");
    }

    static async Task<List<TestInfo>> ExtractTestsFromFile(string filePath)
    {
        var results = new List<TestInfo>();
        var code = await File.ReadAllTextAsync(filePath);

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(_ => _.AttributeLists
                .SelectMany(_ => _.Attributes)
                .Any(_ => _.Name.ToString() == "Test"));

        foreach (var method in methods)
        {
            var body = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? "";

            if (!body.Contains("VerifySvg"))
            {
                continue;
            }

            var input = ExtractInputString(method) ?? await ResolveSharedSample(method, filePath);
            if (string.IsNullOrEmpty(input))
            {
                continue;
            }

            var testName = method.Identifier.Text;
            var className = method.Ancestors()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault()?.Identifier.Text ?? "";

            var verifiedPngPath = FindVerifiedFile(filePath, className, testName, ".verified.png");

            results.Add(new()
            {
                Name = testName,
                ClassName = className,
                Input = input,
                VerifiedPngPath = verifiedPngPath
            });
        }

        return results;
    }

    /// <summary>
    /// Resolves an input passed as a shared constant, e.g. <c>VerifySvg(StateSamples.Simple)</c>. Without
    /// this the test looks input-less, its whole category comes out empty, and the category's page is
    /// deleted by the rebuild.
    /// </summary>
    static async Task<string?> ResolveSharedSample(MethodDeclarationSyntax method, string testFilePath)
    {
        var access = method.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .FirstOrDefault(_ => _.Expression is IdentifierNameSyntax);

        if (access?.Expression is not IdentifierNameSyntax owner)
        {
            return null;
        }

        var directory = Path.GetDirectoryName(testFilePath);
        if (directory is null)
        {
            return null;
        }

        var samplesPath = Path.Combine(directory, $"{owner.Identifier.Text}.cs");
        if (!File.Exists(samplesPath))
        {
            return null;
        }

        var tree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(samplesPath));
        var root = await tree.GetRootAsync();
        var wanted = access.Name.Identifier.Text;

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            foreach (var declarator in field.Declaration.Variables)
            {
                if (declarator.Identifier.Text != wanted)
                {
                    continue;
                }

                if (declarator.Initializer?.Value is LiteralExpressionSyntax literal)
                {
                    return literal.Token.ValueText;
                }
            }
        }

        return null;
    }

    private static string? ExtractInputString(MethodDeclarationSyntax method)
    {
        // Find raw string literals (""" ... """)
        var rawStrings = method.DescendantNodes()
            .OfType<LiteralExpressionSyntax>()
            .Where(_ => _.IsKind(SyntaxKind.Utf8StringLiteralExpression) ||
                        _.Token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) ||
                        _.Token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
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
            .Where(_ => _.IsKind(SyntaxKind.StringLiteralExpression))
            .ToList();

        if (stringLiterals.Count > 0)
        {
            return stringLiterals[0].Token.ValueText;
        }

        // Try interpolated strings
        var interpolated = method.DescendantNodes()
            .OfType<InterpolatedStringExpressionSyntax>()
            .FirstOrDefault();

        return interpolated?
            .Contents
            .OfType<InterpolatedStringTextSyntax>()
            .Select(_ => _.TextToken.ValueText)
            .FirstOrDefault();
    }

    static string FindVerifiedFile(string testFile, string className, string testName, string extension)
    {
        var dir = Path.GetDirectoryName(testFile)!;

        var file = Path.Combine(dir, $"{className}.{testName}{extension}");
        if (File.Exists(file))
        {
            return file;
        }

        throw new($"Could not find: {file}");
    }

    static string GetMermaidLiveUrl(string code)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { code, mermaid = new { theme = "default" } });
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return $"https://mermaid.live/edit#base64:{base64}";
    }

    private class TestInfo
    {
        public string Name { get; init; } = "";
        public string ClassName { get; set; } = "";
        public string Input { get; init; } = "";
        public required string VerifiedPngPath { get; init; }
    }
}
