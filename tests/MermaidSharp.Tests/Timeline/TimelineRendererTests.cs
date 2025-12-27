namespace MermaidSharp.Tests.Timeline;

public class TimelineRendererTests
{
    [Test]
    public Task Render_SimpleTimeline()
    {
        const string input = """
            timeline
                2020 : Event One
                2021 : Event Two
                2022 : Event Three
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_TimelineWithTitle()
    {
        const string input = """
            timeline
                title History of Computing
                1940 : First Computer
                1970 : Personal Computers
                2000 : Internet Era
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MultipleEventsPerPeriod()
    {
        const string input = """
            timeline
                2004 : Facebook
                     : Gmail
                2005 : YouTube
                2006 : Twitter
                     : Spotify
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_WithSections()
    {
        const string input = """
            timeline
                title Technology Timeline
                section Early Era
                    1990 : World Wide Web
                    1995 : Windows 95
                section Modern Era
                    2007 : iPhone
                    2010 : iPad
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_TextPeriods()
    {
        const string input = """
            timeline
                title Project Phases
                Planning : Define scope
                Design : Create mockups
                Development : Build features
                Testing : Quality assurance
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MultipleSections()
    {
        const string input = """
            timeline
                section Ancient History
                    3000 BC : Writing invented
                    500 BC : Democracy
                section Medieval
                    500 AD : Dark Ages
                    1400 : Renaissance
                section Modern
                    1800 : Industrial Revolution
                    2000 : Digital Age
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CompleteTimeline()
    {
        const string input = """
            timeline
                title Social Media Evolution
                section Web 1.0
                    1997 : Six Degrees
                    1999 : LiveJournal
                section Web 2.0
                    2003 : MySpace
                         : LinkedIn
                    2004 : Facebook
                    2005 : YouTube
                    2006 : Twitter
                section Mobile Era
                    2010 : Instagram
                    2011 : Snapchat
                    2016 : TikTok
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
