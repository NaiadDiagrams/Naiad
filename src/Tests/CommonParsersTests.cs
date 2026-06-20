using Pidgin;

public class CommonParsersTests
{
    [Test]
    public async Task SignedDecimalAcceptsMermaidNumberForms()
    {
        // Mermaid's numeric token is [+-]?(?:\d+(?:\.\d+)?|\.\d+): a leading + and bare-dot decimals
        // like .5 are valid and must parse (the old per-diagram parsers rejected both).
        await Assert.That(Parse("5")).IsEqualTo(5d);
        await Assert.That(Parse("-5")).IsEqualTo(-5d);
        await Assert.That(Parse("+5")).IsEqualTo(5d);
        await Assert.That(Parse("5.5")).IsEqualTo(5.5d);
        await Assert.That(Parse("-5.5")).IsEqualTo(-5.5d);
        await Assert.That(Parse(".5")).IsEqualTo(0.5d);
        await Assert.That(Parse("+.5")).IsEqualTo(0.5d);
        await Assert.That(Parse("-.5")).IsEqualTo(-0.5d);

        static double Parse(string input)
        {
            var result = CommonParsers.SignedDecimal.Parse(input);
            if (!result.Success)
            {
                throw new($"Failed to parse '{input}': {result.Error}");
            }

            return result.Value;
        }
    }
}
