public class ClassTests : TestBase
{
    [Test]
    public Task Simple()
    {
        const string input =
            """
            classDiagram
                class Animal
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task Members()
    {
        const string input =
            """
            classDiagram
                class Animal {
                    +String name
                    +int age
                }
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task Methods()
    {
        const string input =
            """
            classDiagram
                class Animal {
                    +makeSound()
                    +move() : void
                }
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task MembersAndMethods()
    {
        const string input =
            """
            classDiagram
                class Animal {
                    +String name
                    +int age
                    +makeSound() : void
                    +move(int distance) : void
                }
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task Inheritance()
    {
        const string input =
            """
            classDiagram
                Animal <|-- Dog
                Animal <|-- Cat
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task Composition()
    {
        const string input =
            """
            classDiagram
                Car *-- Engine
                Car *-- Wheel
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task Aggregation()
    {
        const string input =
            """
            classDiagram
                Library o-- Book
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task Association()
    {
        const string input =
            """
            classDiagram
                Student --> Course : enrolls
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task InterfaceAnnotation()
    {
        const string input =
            """
            classDiagram
                class IFlyable {
                    <<interface>>
                    +fly() : void
                }
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task Complex()
    {
        const string input =
            """
            classDiagram
            class IRepository~T~ {
                <<interface>>
                +get(id: int) T
                +save(entity: T) void
                +delete(id: int) void
            }
            
            class AbstractEntity {
                <<abstract>>
                #int id
                #DateTime createdAt
                #DateTime updatedAt
                +getId() int
            }
            
            class UserService {
                <<service>>
                -IUserRepository repository
                -ILogger logger
                +createUser(name: String) User
                +findUser(id: int) User
                +deleteUser(id: int) void
            }
            
            class Status {
                <<enumeration>>
                ACTIVE
                INACTIVE
                PENDING
                DELETED
            }
            
            class User {
                +String name
                +String email
                -String passwordHash
                ~Status status
                +validate()$ bool
                +hashPassword(password: String)$ String
            }
            
            class Address {
                +String street
                +String city
                +String zipCode
            }
            
            class Order {
                +int orderId
                +List~Item~ items
                +calculateTotal() Decimal
            }
            
            class Item {
                +String name
                +Decimal price
                +int quantity
            }
            
            IRepository~T~ <|.. UserRepository : implements
            AbstractEntity <|-- User : extends
            UserService ..> IRepository~T~ : uses
            User "1" --> "1..*" Address : has
            User "1" o-- "*" Order : places
            Order "1" *-- "1..*" Item : contains
            """;

        return VerifySvg(input);
    }
}