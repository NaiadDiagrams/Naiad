using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Uses Roslyn to find all tests with const string input and regenerates
/// their verified files from mermaid.ink.
/// </summary>
public class VerifiedFileRegenerator
{
    static readonly HttpClient HttpClient = new();

    [Test]
    [Explicit("Run manually to regenerate all verified files from mermaid.ink")]
    public async Task RegenerateAllVerifiedFilesFromMermaidInk()
    {
        var testFiles = Directory.GetFiles(ProjectFiles.ProjectDirectory, "*Tests.cs", SearchOption.AllDirectories)
            .ToList();

        var allTestInputs = new List<TestInput>();

        foreach (var testFile in testFiles)
        {
            var inputs = await ExtractTestInputsFromFile(testFile);
            allTestInputs.AddRange(inputs);
        }

        foreach (var testInput in allTestInputs)
        {
            var svg = await FetchFromMermaidInk(testInput.Input);

            var verifiedPath = GetVerifiedFilePath(testInput);
            await File.WriteAllTextAsync(verifiedPath, svg, new UTF8Encoding(false));
        }
    }

    static async Task<List<TestInput>> ExtractTestInputsFromFile(string filePath)
    {
        var result = new List<TestInput>();
        var code = await File.ReadAllTextAsync(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var classDecl in classDeclarations)
        {
            var className = classDecl.Identifier.Text;
            var methods = classDecl.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                // Check if method has [Test] attribute
                var hasTestAttribute = method.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(a => a.Name.ToString() == "Test");

                if (!hasTestAttribute) continue;

                // Only include tests that return Task (which use Verify)
                var returnType = method.ReturnType.ToString();
                if (returnType != "Task") continue;

                // Look for const string input declaration
                var localDeclarations = method.Body?.DescendantNodes()
                    .OfType<LocalDeclarationStatementSyntax>()
                    .Where(ld => ld.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
                    .ToList() ?? [];

                foreach (var localDecl in localDeclarations)
                {
                    var variable = localDecl.Declaration.Variables.FirstOrDefault();
                    if (variable?.Identifier.Text != "input") continue;

                    var initializer = variable.Initializer?.Value;
                    if (initializer == null) continue;

                    string? inputValue = null;

                    // Handle raw string literal (""" ... """)
                    if (initializer is InterpolatedStringExpressionSyntax interpolated)
                    {
                        inputValue = ExtractInterpolatedString(interpolated);
                    }
                    else if (initializer.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        var literal = (LiteralExpressionSyntax)initializer;
                        inputValue = literal.Token.ValueText;
                    }
                    else if (initializer.IsKind(SyntaxKind.Utf8StringLiteralExpression))
                    {
                        var literal = (LiteralExpressionSyntax)initializer;
                        inputValue = literal.Token.ValueText;
                    }
                    else
                    {
                        // Try to get the text directly for raw string literals
                        var text = initializer.ToString();
                        if (text.StartsWith("\"\"\""))
                        {
                            inputValue = ExtractRawStringLiteral(text);
                        }
                    }

                    if (inputValue != null)
                    {
                        result.Add(new TestInput
                        {
                            ClassName = className,
                            MethodName = method.Identifier.Text,
                            Input = inputValue.Trim(),
                            SourceFile = filePath
                        });
                    }
                }
            }
        }

        return result;
    }

    static string? ExtractRawStringLiteral(string text)
    {
        // Handle """ ... """ raw string literals
        var match = Regex.Match(text, @"^""""""\s*\n?(.*?)\s*""""""$", RegexOptions.Singleline);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return text.Trim('"');
    }

    static string? ExtractInterpolatedString(InterpolatedStringExpressionSyntax interpolated)
    {
        // For simple interpolated strings without expressions
        return string.Concat(interpolated.Contents
            .OfType<InterpolatedStringTextSyntax>()
            .Select(t => t.TextToken.ValueText));
    }

    static async Task<string> FetchFromMermaidInk(string mermaidCode)
    {
        // Encode the mermaid code as base64 for the mermaid.ink API
        var bytes = Encoding.UTF8.GetBytes(mermaidCode);
        var base64 = Convert.ToBase64String(bytes);

        // Make it URL-safe
        var urlSafeBase64 = base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var url = $"https://mermaid.ink/svg/{urlSafeBase64}";

        var response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    static string GetVerifiedFilePath(TestInput testInput)
    {
        var directory = Path.GetDirectoryName(testInput.SourceFile)!;
        var fileName = $"{testInput.ClassName}.{testInput.MethodName}.verified.svg";
        return Path.Combine(directory, fileName);
    }

    record TestInput
    {
        public required string ClassName { get; init; }
        public required string MethodName { get; init; }
        public required string Input { get; init; }
        public required string SourceFile { get; init; }
    }
}
