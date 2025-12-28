// Global using directives

global using MermaidSharp;
global using System.Runtime.CompilerServices;
global using System.Text;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // Configure Verify to use UTF-8 without BOM to match mermaid.ink output
        VerifierSettings.UseUtf8NoBom();
    }
}