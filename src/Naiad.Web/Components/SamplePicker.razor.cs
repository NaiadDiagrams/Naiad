namespace Naiad.Web.Components;

public partial class SamplePicker
{
    // Always reset to the placeholder after a pick, so the control reads "Load a sample…" again and
    // re-selecting the same sample still raises the event (a value change is what fires onchange).
    string selected = "";

    [Parameter]
    public IReadOnlyList<DiagramSample> Samples { get; set; } = [];

    [Parameter]
    public EventCallback<DiagramSample> OnSampleSelected { get; set; }

    Task OnChanged(ChangeEventArgs args)
    {
        var sample = DiagramSamples.Find(args.Value?.ToString());
        selected = "";
        if (sample is not null)
        {
            return OnSampleSelected.InvokeAsync(sample);
        }

        return Task.CompletedTask;
    }
}
