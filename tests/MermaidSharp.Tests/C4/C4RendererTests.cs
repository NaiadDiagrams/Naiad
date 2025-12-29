public class C4RendererTests : TestBase
{
    [Test]
    public Task SimpleContext()
    {
        const string input =
            """
            C4Context
                title System Context diagram
                Person(user, "User", "A user of the system")
                System(system, "System", "The main system")
                Rel(user, system, "Uses")
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task ContextWithExternal()
    {
        const string input =
            """
            C4Context
                title Banking System Context
                Person(customer, "Banking Customer", "A customer of the bank")
                System(banking, "Internet Banking", "Allows customers to manage accounts")
                System_Ext(email, "E-mail System", "External email provider")
                Rel(customer, banking, "Views account info")
                Rel(banking, email, "Sends emails", "SMTP")
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task ContainerDiagram()
    {
        const string input =
            """
            C4Container
                title Container diagram for Banking System
                Person(customer, "Customer", "Bank customer")
                Container(web, "Web Application", "React", "Provides banking UI")
                Container(api, "API Server", "Node.js", "Handles requests")
                ContainerDb(db, "Database", "PostgreSQL", "Stores user data")
                Rel(customer, web, "Uses", "HTTPS")
                Rel(web, api, "Calls", "JSON/HTTPS")
                Rel(api, db, "Reads/Writes", "SQL")
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task ComponentDiagram()
    {
        const string input =
            """
            C4Component
                title Component diagram for API
                Component(auth, "Auth Controller", "Express", "Handles authentication")
                Component(user, "User Controller", "Express", "Manages users")
                Component(service, "User Service", "TypeScript", "Business logic")
                Rel(auth, service, "Uses")
                Rel(user, service, "Uses")
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task MixedElements()
    {
        const string input =
            """
            C4Context
                title E-commerce Platform
                Person(buyer, "Buyer", "Online shopper")
                Person(seller, "Seller", "Product vendor")
                System(platform, "E-commerce Platform", "Online marketplace")
                System_Ext(payment, "Payment Gateway", "Processes payments")
                System_Ext(shipping, "Shipping Service", "Handles delivery")
                Rel(buyer, platform, "Browses and buys")
                Rel(seller, platform, "Lists products")
                Rel(platform, payment, "Processes payments")
                Rel(platform, shipping, "Ships orders")
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task NoRelationships()
    {
        const string input =
            """
            C4Context
                title Standalone Systems
                System(a, "System A", "First system")
                System(b, "System B", "Second system")
                System(c, "System C", "Third system")
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task CompleteC4()
    {
        const string input =
            """
            C4Context
                title Enterprise Architecture Overview

                Person(admin, "Administrator", "System administrator with full access")
                Person(user, "Regular User", "End user of the application")

                System(core, "Core System", "Main application server")
                System(auth, "Auth Service", "Authentication and authorization")
                System(db, "Database", "PostgreSQL database cluster")

                System_Ext(payment, "Payment Gateway", "Third-party payment processor")
                System_Ext(email, "Email Service", "SendGrid email delivery")
                System_Ext(cdn, "CDN", "Content delivery network")

                Rel(admin, core, "Manages", "HTTPS")
                Rel(user, core, "Uses", "HTTPS")
                Rel(core, auth, "Authenticates via")
                Rel(core, db, "Reads/Writes", "TCP/5432")
                Rel(core, payment, "Processes payments", "HTTPS")
                Rel(core, email, "Sends notifications", "SMTP")
                Rel(core, cdn, "Serves assets", "HTTPS")
            """;

        return VerifySvg(input);
    }
}