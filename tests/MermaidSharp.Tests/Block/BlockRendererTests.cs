public class BlockRendererTests
{
    [Test]
    public Task Render_SimpleBlock()
    {
        const string input = """
            block-beta
                columns 3
                a["Block A"] b["Block B"] c["Block C"]
            """;


        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_WithSpan()
    {
        const string input = """
            block-beta
                columns 3
                a["Wide Block"]:2 b["Normal"]
                c["Full Width"]:3
            """;


        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_DifferentShapes()
    {
        const string input = """
            block-beta
                columns 3
                a["Rectangle"] b("Rounded") c(["Stadium"])
                d(("Circle")) e{"Diamond"} f{{"Hexagon"}}
            """;


        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_SingleColumn()
    {
        const string input = """
            block-beta
                columns 1
                a["First"]
                b["Second"]
                c["Third"]
            """;


        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_ManyColumns()
    {
        const string input = """
            block-beta
                columns 5
                a b c d e
            """;


        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_MixedLayout()
    {
        const string input = """
            block-beta
                columns 4
                header["Header"]:4
                nav["Nav"] content["Content"]:2 side["Sidebar"]
                footer["Footer"]:4
            """;


        return SvgVerify.Verify(input);
    }
}
