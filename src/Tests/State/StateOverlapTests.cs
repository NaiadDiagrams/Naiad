using Naiad.Diagrams.State;

// Drives the State renderer with ValidateLayout enabled, which makes Render throw when a label, line,
// or node overlaps another (or falls outside the SVG bounds). This is the Release-config guard for the
// self-checks that previously only ran under #if DEBUG (and so never ran in the test suite).
public class StateOverlapTests
{
    [Test]
    [Arguments(StateSamples.Simple)]
    [Arguments(StateSamples.MultipleStates)]
    [Arguments(StateSamples.TransitionLabels)]
    [Arguments(StateSamples.Description)]
    [Arguments(StateSamples.ForkJoinState)]
    [Arguments(StateSamples.ChoiceState)]
    [Arguments(StateSamples.StateWithNote)]
    [Arguments(StateSamples.StateDiagramV1)]
    [Arguments(StateSamples.Complex)]
    public async Task NoLayoutOverlaps(string input)
    {
        var result = new StateParser().Parse(input);
        await Assert.That(result.Success).IsTrue();

        var renderer = new StateRenderer { ValidateLayout = true };
        await Assert.That(() => renderer.Render(result.Value, RenderOptions.Default)).ThrowsNothing();
    }
}
