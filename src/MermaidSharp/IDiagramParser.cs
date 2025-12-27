namespace MermaidSharp;

public interface IDiagramParser<TModel> where TModel : DiagramBase
{
    DiagramType DiagramType { get; }
    Result<char, TModel> Parse(string input);
}