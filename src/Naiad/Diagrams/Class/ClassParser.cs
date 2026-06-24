class ClassParser : IDiagramParser<ClassModel>
{
    // Identifier for class names (alphanumeric and underscore)
    static readonly Parser<char, string> classIdentifier;

    // Visibility modifier
    static readonly Parser<char, Visibility> visibilityParser;

    // Type annotation like : String or : int
    static readonly Parser<char, string> typeAnnotation;

    // Method parameters like (String name, int age)
    static readonly Parser<char, List<MethodParameter>> parametersParser;

    // Member: +String name (type first) or +name : String (type after colon)
    static readonly Parser<char, ClassMember> memberParser;

    // Method: +makeSound() void or +makeSound(String s) : void
    static readonly Parser<char, ClassMethod> methodParser;

    // Class annotation: <<interface>>, <<abstract>>, etc.
    static readonly Parser<char, ClassAnnotation> annotationParser;

    // Class body content: { ... }
    static readonly Parser<char, (ClassAnnotation? annotation, List<ClassMember> members, List<ClassMethod> methods)> ParseClassBody;

    // Class definition: class ClassName { ... } or class ClassName
    static readonly Parser<char, ClassDefinition> classDefinitionParser;

    // Relationship arrows
    static readonly Parser<char, RelationshipType> relationshipArrowParser;

    // Cardinality like "1", "0..1", "1..*", "*"
    static readonly Parser<char, string> cardinalityParser;

    // Relationship: ClassA <|-- ClassB : label
    static readonly Parser<char, ClassRelationship> relationshipParser;

    // Direction directive
    static readonly Parser<char, Direction> directionDirectiveParser;

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> skipLine;

    static readonly Parser<char, IEnumerable<IClassContent?>> ParseContent;

    static readonly Parser<char, ClassModel> Parser;

    static ClassParser()
    {
        classIdentifier =
            Token(_ => char.IsLetterOrDigit(_) || _ == '_')
                .AtLeastOnceString()
                .Labelled("class identifier");

        visibilityParser =
            OneOf(
                Char('+').ThenReturn(Visibility.Public),
                Char('-').ThenReturn(Visibility.Private),
                Char('#').ThenReturn(Visibility.Protected),
                Char('~').ThenReturn(Visibility.PackagePrivate)
            );

        typeAnnotation =
            CommonParsers.InlineWhitespace
                .Then(Char(':'))
                .Then(CommonParsers.InlineWhitespace)
                .Then(Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '<' || _ == '>' || _ == '[' || _ == ']' || _ == ',')
                    .AtLeastOnceString());

        parametersParser =
            Char('(')
                .Then(
                    Token(_ => _ != ')' && _ != '\r' && _ != '\n')
                        .ManyString()
                )
                .Before(Char(')'))
                .Select(ParseParameters);

        memberParser =
            from _ in CommonParsers.InlineWhitespace
            from visibility in visibilityParser.Optional()
            from isStatic in Char('$').Optional()
            from firstWord in Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '<' || _ == '>' || _ == '[' || _ == ']').AtLeastOnceString()
            from rest in Try(
                // Type first format: +String name
                from __ in CommonParsers.RequiredWhitespace
                from memberName in Token(_ => char.IsLetterOrDigit(_) || _ == '_').AtLeastOnceString()
                select (Type: firstWord, Name: memberName)
            ).Or(
                // Name only or name : Type format
                from typeAnnotation in typeAnnotation.Optional()
                select (Type: typeAnnotation.HasValue ? typeAnnotation.Value : null, Name: firstWord)
            )
            from ___ in CommonParsers.InlineWhitespace
            from ____ in CommonParsers.LineEnd
            select new ClassMember
            {
                Name = rest.Name,
                Type = rest.Type,
                Visibility = visibility.HasValue ? visibility.Value : Visibility.Public,
                IsStatic = isStatic.HasValue
            };

        methodParser =
            from _ in CommonParsers.InlineWhitespace
            from visibility in visibilityParser.Optional()
            from isStatic in Char('$').Optional()
            from isAbstract in Char('*').Optional()
            from name in Token(_ => char.IsLetterOrDigit(_) || _ == '_').AtLeastOnceString()
            from parameters in parametersParser
            from returnType in typeAnnotation.Optional()
            from __ in CommonParsers.InlineWhitespace
            from ___ in CommonParsers.LineEnd
            select new ClassMethod
            {
                Name = name,
                ReturnType = returnType.HasValue ? returnType.Value : null,
                Visibility = visibility.HasValue ? visibility.Value : Visibility.Public,
                IsStatic = isStatic.HasValue,
                IsAbstract = isAbstract.HasValue
            };

        annotationParser =
            CommonParsers.InlineWhitespace
                .Then(String("<<"))
                .Then(OneOf(
                    Try(String("interface")).ThenReturn(ClassAnnotation.Interface),
                    Try(String("abstract")).ThenReturn(ClassAnnotation.Abstract),
                    Try(String("service")).ThenReturn(ClassAnnotation.Service),
                    String("enumeration").ThenReturn(ClassAnnotation.Enumeration)
                ))
                .Before(String(">>"))
                .Before(CommonParsers.InlineWhitespace)
                .Before(CommonParsers.LineEnd);

        {
            var annotationLine = Try(annotationParser.Select<IClassBodyContent?>(_ => new AnnotationItem(_)));
            var methodLine = Try(methodParser.Select<IClassBodyContent?>(_ => new MethodItem(_)));
            var memberLine = Try(memberParser.Select<IClassBodyContent?>(_ => new MemberItem(_)));
            var emptyLine = CommonParsers.InlineWhitespace.Then(CommonParsers.LineEnd)
                .ThenReturn<IClassBodyContent?>(null);

            var contentLine = OneOf(annotationLine, methodLine, memberLine, emptyLine);

            ParseClassBody = contentLine.Many().Select(items =>
            {
                ClassAnnotation? annotation = null;
                var members = new List<ClassMember>();
                var methods = new List<ClassMethod>();

                foreach (var item in items)
                {
                    switch (item)
                    {
                        case AnnotationItem a:
                            annotation = a.Value;
                            break;
                        case MemberItem m:
                            members.Add(m.Value);
                            break;
                        case MethodItem m:
                            methods.Add(m.Value);
                            break;
                    }
                }

                return (annotation, members, methods);
            });
        }

        classDefinitionParser =
            from _ in CommonParsers.InlineWhitespace
            from keyword in String("class")
            from __ in CommonParsers.RequiredWhitespace
            from id in classIdentifier
            from ___ in CommonParsers.InlineWhitespace
            from body in Try(
                from open in Char('{')
                from ____ in CommonParsers.LineEnd
                from content in ParseClassBody
                from _____ in CommonParsers.InlineWhitespace
                from close in Char('}')
                from ______ in CommonParsers.LineEnd
                select content
            ).Optional()
            from _______ in CommonParsers.LineEnd.Optional()
            select CreateClassDefinition(id, body);

        relationshipArrowParser =
            OneOf(
                Try(String("<|--")).ThenReturn(RelationshipType.Inheritance),
                Try(String("--|>")).ThenReturn(RelationshipType.Inheritance),
                Try(String("*--")).ThenReturn(RelationshipType.Composition),
                Try(String("--*")).ThenReturn(RelationshipType.Composition),
                Try(String("o--")).ThenReturn(RelationshipType.Aggregation),
                Try(String("--o")).ThenReturn(RelationshipType.Aggregation),
                Try(String("<..")).ThenReturn(RelationshipType.DependencyLeft),
                Try(String("..>")).ThenReturn(RelationshipType.DependencyRight),
                Try(String("..|>")).ThenReturn(RelationshipType.Realization),
                Try(String("<|..")).ThenReturn(RelationshipType.Realization),
                Try(String("-->")).ThenReturn(RelationshipType.Association),
                Try(String("<--")).ThenReturn(RelationshipType.Association),
                String("--").ThenReturn(RelationshipType.Link)
            );

        cardinalityParser =
            Char('"')
                .Then(Token(_ => _ != '"').AtLeastOnceString())
                .Before(Char('"'));

        relationshipParser =
            from _ in CommonParsers.InlineWhitespace
            from fromCardinality in Try(cardinalityParser.Before(CommonParsers.InlineWhitespace)).Optional()
            from fromId in classIdentifier
            from __ in CommonParsers.InlineWhitespace
            from arrow in relationshipArrowParser
            from ___ in CommonParsers.InlineWhitespace
            from toId in classIdentifier
            from ____ in CommonParsers.InlineWhitespace
            from toCardinality in Try(cardinalityParser).Optional()
            from label in Try(
                CommonParsers.InlineWhitespace
                    .Then(Char(':'))
                    .Then(CommonParsers.InlineWhitespace)
                    .Then(Token(_ => _ != '\r' && _ != '\n').ManyString())
            ).Optional()
            from lineEnd in CommonParsers.LineEnd
            select new ClassRelationship
            {
                FromId = fromId,
                ToId = toId,
                Type = arrow,
                Label = label.HasValue ? label.Value : null,
                FromCardinality = fromCardinality.HasValue ? fromCardinality.Value : null,
                ToCardinality = toCardinality.HasValue ? toCardinality.Value : null
            };

        directionDirectiveParser =
            CommonParsers.InlineWhitespace
                .Then(String("direction"))
                .Then(CommonParsers.RequiredWhitespace)
                .Then(CommonParsers.DirectionParser)
                .Before(CommonParsers.LineEnd);

        skipLine =
            CommonParsers.InlineWhitespace
                .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

        ParseContent =
            OneOf(
                Try(directionDirectiveParser.Select<IClassContent?>(_ => new DirectionItem(_))),
                Try(classDefinitionParser.Select<IClassContent?>(_ => new ClassDefinitionItem(_))),
                Try(relationshipParser.Select<IClassContent?>(_ => new RelationshipItem(_))),
                skipLine.ThenReturn<IClassContent?>(null)
            ).Many();

        Parser =
            from _ in CommonParsers.InlineWhitespace
            from keyword in String("classDiagram")
            from __ in CommonParsers.InlineWhitespace
            from ___ in CommonParsers.LineEnd
            from content in ParseContent
            select BuildModel(content);
    }

    static List<MethodParameter> ParseParameters(string paramStr)
    {
        var parameters = new List<MethodParameter>();
        if (string.IsNullOrWhiteSpace(paramStr))
            return parameters;

        foreach (var param in paramStr.Split(','))
        {
            var parts = param.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1)
            {
                parameters.Add(
                    new()
                    {
                        Name = parts.Length >= 2 ? parts[1] : parts[0],
                        Type = parts.Length >= 2 ? parts[0] : null
                    });
            }
        }

        return parameters;
    }

    static ClassDefinition CreateClassDefinition(
        string id,
        Maybe<(ClassAnnotation? annotation, List<ClassMember> members, List<ClassMethod> methods)> body)
    {
        var classDef = new ClassDefinition {Id = id};

        if (body.HasValue)
        {
            if (body.Value.annotation.HasValue)
                classDef.Annotation = body.Value.annotation;
            classDef.Members.AddRange(body.Value.members);
            classDef.Methods.AddRange(body.Value.methods);
        }

        return classDef;
    }

    static ClassModel BuildModel(IEnumerable<IClassContent?> content)
    {
        var model = new ClassModel();
        var classIds = new HashSet<string>();

        foreach (var item in content)
        {
            switch (item)
            {
                case DirectionItem d:
                    model.Direction = d.Value;
                    break;

                case ClassDefinitionItem cdef:
                    var c = cdef.Value;
                    if (!classIds.Contains(c.Id))
                    {
                        model.Classes.Add(c);
                        classIds.Add(c.Id);
                    }

                    break;

                case RelationshipItem rel:
                    var r = rel.Value;
                    // Auto-add classes from relationships
                    if (!classIds.Contains(r.FromId))
                    {
                        model.Classes.Add(
                            new()
                            {
                                Id = r.FromId
                            });
                        classIds.Add(r.FromId);
                    }

                    if (!classIds.Contains(r.ToId))
                    {
                        model.Classes.Add(
                            new()
                            {
                                Id = r.ToId
                            });
                        classIds.Add(r.ToId);
                    }

                    model.Relationships.Add(r);
                    break;
            }
        }

        return model;
    }

    public Result<char, ClassModel> Parse(string input) => Parser.Parse(input);

    interface IClassBodyContent;

    readonly record struct AnnotationItem(ClassAnnotation Value) : IClassBodyContent;

    readonly record struct MemberItem(ClassMember Value) : IClassBodyContent;

    readonly record struct MethodItem(ClassMethod Value) : IClassBodyContent;

    internal interface IClassContent;

    readonly record struct DirectionItem(Direction Value) : IClassContent;

    readonly record struct ClassDefinitionItem(ClassDefinition Value) : IClassContent;

    readonly record struct RelationshipItem(ClassRelationship Value) : IClassContent;
}
