public class BlockRendererTests : TestBase
{
    [Test]
    public Task SimpleBlock()
    {
        const string input =
            """
                             block-beta
                                 columns 3
                                 a["Block A"] b["Block B"] c["Block C"]
                             """;

        return VerifySvg(input);
    }

    [Test]
    public Task WithSpan()
    {
        const string input =
            """
                             block-beta
                                 columns 3
                                 a["Wide Block"]:2 b["Normal"]
                                 c["Full Width"]:3
                             """;

        return VerifySvg(input);
    }

    [Test]
    public Task DifferentShapes()
    {
        const string input =
            """
                             block-beta
                                 columns 3
                                 a["Rectangle"] b("Rounded") c(["Stadium"])
                                 d(("Circle")) e{"Diamond"} f{{"Hexagon"}}
                             """;

        return VerifySvg(input);
    }

    [Test]
    public Task SingleColumn()
    {
        const string input =
            """
                             block-beta
                                 columns 1
                                 a["First"]
                                 b["Second"]
                                 c["Third"]
                             """;

        return VerifySvg(input);
    }

    [Test]
    public Task ManyColumns()
    {
        const string input =
            """
            block-beta
                columns 5
                a b c d e
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task MixedLayout()
    {
        const string input =
            """
            block-beta
                columns 4
                header["Header"]:4
                nav["Nav"] content["Content"]:2 side["Sidebar"]
                footer["Footer"]:4
            """;

        return VerifySvg(input);
    }
}
