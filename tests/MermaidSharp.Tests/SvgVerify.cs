public static class SvgVerify
{
    public static Task Verify(
        string input,
        [CallerFilePath] string sourceFile = "")
    {
        var svg = Mermaid.Render(input);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));
        return Verifier.Verify(stream, "svg", sourceFile: sourceFile);
    }
}