public class TreemapTests
{
    [Test]
    public Task BasicTreemap()
    {
        var input = """
                    treemap-beta
                    "Section A"
                        "Item 1": 30
                        "Item 2": 20
                    "Section B"
                        "Item 3": 50
                    """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task SingleLevel()
    {
        var input = """
                    treemap-beta
                    "Alpha": 40
                    "Beta": 30
                    "Gamma": 20
                    "Delta": 10
                    """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task NestedSections()
    {
        var input = """
                    treemap-beta
                    "Root"
                        "Branch 1"
                            "Leaf 1.1": 15
                            "Leaf 1.2": 25
                        "Branch 2"
                            "Leaf 2.1": 30
                            "Leaf 2.2": 10
                            "Leaf 2.3": 20
                    """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task MixedHierarchy()
    {
        var input = """
                    treemap-beta
                    "Products": 100
                    "Services"
                        "Consulting": 50
                        "Support": 30
                    "Other": 20
                    """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task LargeValues()
    {
        var input = """
                    treemap-beta
                    "Category A"
                        "Sub A1": 1000
                        "Sub A2": 500
                    "Category B"
                        "Sub B1": 750
                        "Sub B2": 250
                    """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task ManyItems()
    {
        var input = """
                    treemap-beta
                    "Group 1"
                        "A": 10
                        "B": 15
                        "C": 20
                    "Group 2"
                        "D": 25
                        "E": 30
                    "Group 3"
                        "F": 12
                        "G": 18
                        "H": 22
                        "I": 8
                    """;

        return SvgVerify.Verify(input);
    }
}
