using Naiad.Diagrams.State;

public class StateParserTests
{
    // A composite block names a state that a transition has usually already introduced. Creating a second
    // State for it left the id in model.States twice, and the renderer drew it twice - once as a plain
    // state and once as a container. Declaration order must not change the outcome.
    [Test]
    [Arguments("transition first", """
                                   stateDiagram-v2
                                       [*] --> Outer
                                       state Outer {
                                           [*] --> Inner
                                       }
                                       Outer --> Finished
                                   """)]
    [Arguments("block first", """
                              stateDiagram-v2
                                  state Outer {
                                      [*] --> Inner
                                  }
                                  Outer --> Finished
                              """)]
    [Arguments("described first", """
                                  stateDiagram-v2
                                      state "The outer one" as Outer
                                      [*] --> Outer
                                      state Outer {
                                          [*] --> Inner
                                      }
                                  """)]
    public async Task CompositeStateIsDeclaredOnce(string name, string input)
    {
        var result = new StateParser().Parse(input);
        await Assert.That(result.Success).IsTrue();

        var outer = result.Value.States.Where(_ => _.Id == "Outer").ToList();
        await Assert.That(outer.Count).IsEqualTo(1).Because($"{name}: Outer should appear once");
        await Assert.That(outer[0].IsComposite).IsTrue().Because($"{name}: Outer should keep its nested states");
    }

    // The description survives the composite block reusing the state that carries it.
    [Test]
    public async Task CompositeKeepsAnEarlierDescription()
    {
        var result = new StateParser().Parse(
            """
            stateDiagram-v2
                state "The outer one" as Outer
                state Outer {
                    [*] --> Inner
                }
            """);

        await Assert.That(result.Success).IsTrue();

        var outer = result.Value.States.Single(_ => _.Id == "Outer");
        await Assert.That(outer.Description).IsEqualTo("The outer one");
        await Assert.That(outer.IsComposite).IsTrue();
    }
}
