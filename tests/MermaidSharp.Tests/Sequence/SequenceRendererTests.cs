public class SequenceRendererTests : TestBase
{
    [Test]
    public Task SimpleSequence()
    {
        const string input =
            """
            sequenceDiagram
                Alice->>Bob: Hello Bob
                Bob-->>Alice: Hi Alice
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task SequenceWithParticipants()
    {
        const string input =
            """
            sequenceDiagram
                participant A as Alice
                participant B as Bob
                A->>B: Hello
                B->>A: Hi
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task SequenceWithActors()
    {
        const string input =
            """
            sequenceDiagram
                actor User
                participant Server
                User->>Server: Request
                Server-->>User: Response
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task SequenceWithActivation()
    {
        const string input =
            """
            sequenceDiagram
                Alice->>+Bob: Hello
                Bob-->>-Alice: Hi
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task SequenceWithNotes()
    {
        const string input =
            """
            sequenceDiagram
                Alice->>Bob: Hello
                Note right of Bob: Bob thinks
                Bob-->>Alice: Hi
                Note over Alice,Bob: Conversation
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task SequenceWithAutoNumber()
    {
        const string input =
            """
            sequenceDiagram
                autonumber
                Alice->>Bob: Hello
                Bob->>Alice: Hi
                Alice->>Bob: How are you?
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task SequenceWithDifferentArrows()
    {
        const string input =
            """
            sequenceDiagram
                A->>B: Solid arrow
                A-->>B: Dotted arrow
                A->B: Solid open
                A-->B: Dotted open
                A-xB: Solid cross
                A--xB: Dotted cross
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task SequenceWithTitle()
    {
        const string input =
            """
            sequenceDiagram
                title Authentication Flow
                Client->>Server: Login request
                Server-->>Client: Token
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task CompleteSequence()
    {
        const string input =
            """
            sequenceDiagram
                title Complete Authentication Flow
                autonumber

                actor User
                participant Client as Web Client
                participant Auth as Auth Service
                participant DB as Database
                participant Email as Email Service

                User->>+Client: Enter credentials
                Client->>+Auth: POST /login
                Auth->>+DB: Query user
                DB-->>-Auth: User data
                Note right of Auth: Validate credentials
                Auth->>Auth: Generate JWT
                Note right of Auth: Token expires in 24h
                Auth-->>-Client: 200 OK + Token
                Client->>+Email: Send welcome email
                Email-->>-Client: Email sent
                Client-->>-User: Show dashboard
                Note over User,DB: Session established
            """;

        return VerifySvg(input);
    }
}
