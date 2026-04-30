namespace Naiad;

public interface IDiagramParser<TModel> where TModel : DiagramBase
{
    Result<char, TModel> Parse(string input);
}
