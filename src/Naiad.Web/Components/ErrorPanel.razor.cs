namespace Naiad.Web.Components;

public partial class ErrorPanel
{
    [Parameter]
    public string Message { get; set; } = "";

    // When set, the error came from an unexpected exception rather than user input, so we invite a bug
    // report pre-filled at this GitHub "new issue" URL.
    [Parameter]
    public string? IssueUrl { get; set; }
}
