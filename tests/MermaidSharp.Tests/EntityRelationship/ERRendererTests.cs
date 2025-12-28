public class ERRendererTests
{
    [Test]
    public Task SimpleRelationship()
    {
        const string input =
            """
            erDiagram
                CUSTOMER ||--o{ ORDER : places
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task MultipleRelationships()
    {
        const string input =
            """
            erDiagram
                CUSTOMER ||--o{ ORDER : places
                ORDER ||--|{ LINE-ITEM : contains
                PRODUCT ||--o{ LINE-ITEM : includes
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task EntityWithAttributes()
    {
        const string input =
            """
            erDiagram
                CUSTOMER {
                    string name
                    string email
                    int age
                }
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task EntityWithKeyTypes()
    {
        const string input =
            """
            erDiagram
                CUSTOMER {
                    int id PK
                    string name
                    string email UK
                }
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task EntityWithComments()
    {
        const string input =
            """
            erDiagram
                CUSTOMER {
                    int id PK "Primary key"
                    string name "Customer name"
                }
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task OneToOne()
    {
        const string input =
            """
            erDiagram
                PERSON ||--|| PASSPORT : has
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task ZeroOrOne()
    {
        const string input =
            """
            erDiagram
                EMPLOYEE |o--o| PARKING-SPACE : uses
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task NonIdentifying()
    {
        const string input =
            """
            erDiagram
                CUSTOMER ||..o{ ORDER : places
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task CompleteERDiagram()
    {
        const string input =
            """
            erDiagram
                CUSTOMER {
                    int id PK
                    string name
                    string email
                }
                ORDER {
                    int id PK
                    int customer_id FK
                    date order_date
                }
                CUSTOMER ||--o{ ORDER : places
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task FullERDiagram()
    {
        const string input =
            """
            erDiagram
                CUSTOMER {
                    int customer_id PK "Primary key"
                    string first_name "Customer first name"
                    string last_name "Customer last name"
                    string email UK "Unique email address"
                    date date_of_birth
                    string phone
                    boolean is_active
                }
                
                ADDRESS {
                    int address_id PK
                    int customer_id FK
                    string street
                    string city
                    string state
                    string postal_code
                    string country
                    string address_type "billing or shipping"
                }
                
                ORDER {
                    int order_id PK
                    int customer_id FK
                    int shipping_address_id FK
                    int billing_address_id FK
                    datetime order_date
                    datetime shipped_date
                    string status
                    decimal total_amount
                }
                
                ORDER_ITEM {
                    int item_id PK
                    int order_id FK
                    int product_id FK
                    int quantity
                    decimal unit_price
                    decimal discount
                }
                
                PRODUCT {
                    int product_id PK
                    int category_id FK
                    string name
                    string description
                    decimal price
                    int stock_quantity
                    string sku UK
                }
                
                CATEGORY {
                    int category_id PK
                    int parent_id FK "Self-referencing"
                    string name
                    string description
                }
                
                CUSTOMER ||--o{ ORDER : places
                CUSTOMER ||--o{ ADDRESS : has
                ORDER ||--|{ ORDER_ITEM : contains
                ORDER }o--|| ADDRESS : "ships to"
                ORDER }o--|| ADDRESS : "bills to"
                PRODUCT ||--o{ ORDER_ITEM : "included in"
                CATEGORY ||--o{ PRODUCT : categorizes
                CATEGORY |o--o| CATEGORY : "parent of"
            """;

        return SvgVerify.Verify(input);
    }
}
