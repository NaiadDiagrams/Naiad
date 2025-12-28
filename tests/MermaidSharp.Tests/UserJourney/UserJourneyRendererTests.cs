public class UserJourneyRendererTests
{
    [Test]
    public Task SimpleJourney()
    {
        const string input =
            """
            journey
                title My Working Day
                section Morning
                    Make coffee: 5: Me
                    Check emails: 3: Me
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task MultipleSections()
    {
        const string input =
            """
            journey
                title Customer Journey
                section Discovery
                    Visit website: 4: Customer
                    Browse products: 3: Customer
                section Purchase
                    Add to cart: 4: Customer
                    Checkout: 2: Customer
                section Delivery
                    Track order: 5: Customer
                    Receive package: 5: Customer
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task MultipleActors()
    {
        const string input =
            """
            journey
                title Team Collaboration
                section Planning
                    Define requirements: 4: PM, Dev
                    Create design: 3: Designer, PM
                section Development
                    Write code: 4: Dev
                    Code review: 3: Dev, Lead
                section Testing
                    Test features: 4: QA, Dev
                    Fix bugs: 2: Dev
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task AllScores()
    {
        const string input =
            """
            journey
                title Score Examples
                section Experience
                    Terrible: 1: User
                    Bad: 2: User
                    Okay: 3: User
                    Good: 4: User
                    Great: 5: User
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task WithoutTitle()
    {
        const string input =
            """
            journey
                section Tasks
                    First task: 4: Alice
                    Second task: 5: Bob
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task SingleSection()
    {
        const string input =
            """
            journey
                title Quick Journey
                section Main
                    Start: 3: User
                    Process: 4: User
                    End: 5: User
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task ManyActors()
    {
        const string input =
            """
            journey
                title Big Team Project
                section Kickoff
                    Initial meeting: 4: PM, Dev, QA, Designer, Lead, Stakeholder
                section Execution
                    Development: 3: Dev, Lead
                    Testing: 4: QA
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task CompleteUserJourney()
    {
        const string input =
            """
            journey
                title Complete E-commerce Experience
                
                section Discovery
                    Search for product: 4: Customer
                    Browse categories: 3: Customer
                    Read reviews: 5: Customer
                    Compare prices: 4: Customer
                
                section Shopping
                    Add to wishlist: 5: Customer
                    Add to cart: 4: Customer
                    Apply coupon: 2: Customer, Support
                    Update quantity: 3: Customer
                
                section Checkout
                    Enter shipping: 3: Customer
                    Select payment: 4: Customer
                    Confirm order: 5: Customer
                    Receive confirmation: 5: Customer, System
                
                section Fulfillment
                    Order processing: 4: Warehouse, System
                    Package shipping: 4: Warehouse, Courier
                    Track delivery: 5: Customer, Courier
                    Receive package: 5: Customer, Courier
                
                section Post-Purchase
                    Leave review: 4: Customer
                    Contact support: 2: Customer, Support
                    Request return: 1: Customer, Support
                    Receive refund: 3: Customer, Finance
            """;

        return SvgVerify.Verify(input);
    }
}
