public class ClassRendererTests
{
    [Test]
    public Task Render_SimpleClass()
    {
        const string input =
            """
            classDiagram
                class Animal
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_ClassWithMembers()
    {
        const string input =
            """
            classDiagram
                class Animal {
                    +String name
                    +int age
                }
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_ClassWithMethods()
    {
        const string input =
            """
            classDiagram
                class Animal {
                    +makeSound()
                    +move() : void
                }
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_ClassWithMembersAndMethods()
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

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_Inheritance()
    {
        const string input =
            """
            classDiagram
                Animal <|-- Dog
                Animal <|-- Cat
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_Composition()
    {
        const string input =
            """
            classDiagram
                Car *-- Engine
                Car *-- Wheel
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_Aggregation()
    {
        const string input =
            """
            classDiagram
                Library o-- Book
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_Association()
    {
        const string input =
            """
            classDiagram
                Student --> Course : enrolls
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_InterfaceAnnotation()
    {
        const string input =
            """
            classDiagram
                class IFlyable {
                    <<interface>>
                    +fly() : void
                }
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_CompleteClassDiagram()
    {
        const string input =
            """
            classDiagram
                class Animal {
                    +String name
                    +makeSound() : void
                }
                class Dog {
                    +bark() : void
                }
                class Cat {
                    +meow() : void
                }
                Animal <|-- Dog
                Animal <|-- Cat
            """;

        return SvgVerify.Verify(input);
    }

    [Test]
    public Task Render_FullClassDiagram()
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

        return SvgVerify.Verify(input);
    }
}