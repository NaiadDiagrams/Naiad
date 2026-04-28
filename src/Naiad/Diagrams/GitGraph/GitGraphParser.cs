class GitGraphParser : IDiagramParser<GitGraphModel>
{
    public DiagramType DiagramType => DiagramType.GitGraph;

    // Identifiers
    static Parser<char, string> branchName =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-' || _ == '/')
            .AtLeastOnceString()
            .Labelled("branch name");

    static Parser<char, string> commitId =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-')
            .AtLeastOnceString()
            .Labelled("commit id");

    static Parser<char, CommitType> commitTypeParser =
        OneOf(
            Try(CIString("REVERSE")).ThenReturn(CommitType.Reverse),
            Try(CIString("HIGHLIGHT")).ThenReturn(CommitType.Highlight),
            CIString("NORMAL").ThenReturn(CommitType.Normal)
        );

    // Attribute parsers
    public static Parser<char, string> IdAttribute =>
        from _ in Try(CIString("id"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from id in CommonParsers.QuotedString.Or(commitId)
        select id;

    public static Parser<char, string> MessageAttribute =>
        from _ in Try(CIString("msg"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from msg in CommonParsers.QuotedString
        select msg;

    public static Parser<char, string> TagAttribute =>
        from _ in Try(CIString("tag"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from tag in CommonParsers.QuotedString
        select tag;

    public static Parser<char, CommitType> TypeAttribute =>
        from _ in Try(CIString("type"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from type in commitTypeParser
        select type;

    public static Parser<char, int> OrderAttribute =>
        from _ in Try(CIString("order"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from order in CommonParsers.Integer
        select order;

    // Commit: commit id: "abc" msg: "message" tag: "v1.0" type: NORMAL
    public static Parser<char, CommitOperation> commitParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("commit")
        from attrs in ParseCommitAttributes()
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        select CreateCommit(attrs);

    public static Parser<char, IEnumerable<ICommitAttr>> ParseCommitAttributes()
    {
        var attr = OneOf(
            Try(from __ in CommonParsers.InlineWhitespace from a in IdAttribute select (ICommitAttr)new IdAttr(a)),
            Try(from __ in CommonParsers.InlineWhitespace from a in MessageAttribute select (ICommitAttr)new MsgAttr(a)),
            Try(from __ in CommonParsers.InlineWhitespace from a in TagAttribute select (ICommitAttr)new TagAttr(a)),
            Try(from __ in CommonParsers.InlineWhitespace from a in TypeAttribute select (ICommitAttr)new TypeAttr(a))
        );
        return attr.Many();
    }

    static CommitOperation CreateCommit(IEnumerable<ICommitAttr> attrs)
    {
        var commit = new CommitOperation();
        foreach (var attr in attrs)
        {
            switch (attr)
            {
                case IdAttr id: commit.Id = id.Value; break;
                case MsgAttr msg: commit.Message = msg.Value; break;
                case TagAttr tag: commit.Tag = tag.Value; break;
                case TypeAttr type: commit.Type = type.Value; break;
            }
        }
        return commit;
    }

    // Branch: branch develop order: 1
    static Parser<char, BranchOperation> branchParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("branch")
        from ___ in CommonParsers.RequiredWhitespace
        from name in branchName
        from order in Try(
            from ____ in CommonParsers.InlineWhitespace
            from o in OrderAttribute
            select o
        ).Optional()
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new BranchOperation
        {
            Name = name,
            BranchOrder = order.HasValue ? order.Value : null
        };

    // Checkout: checkout develop
    static Parser<char, CheckoutOperation> checkoutParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("checkout")
        from ___ in CommonParsers.RequiredWhitespace
        from name in branchName
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new CheckoutOperation { BranchName = name };

    // Merge: merge develop id: "merge1" tag: "v1.0" type: NORMAL
    static Parser<char, MergeOperation> mergeParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("merge")
        from ___ in CommonParsers.RequiredWhitespace
        from name in branchName
        from attrs in ParseMergeAttributes()
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select CreateMerge(name, attrs);

    public static Parser<char, IEnumerable<ICommitAttr>> ParseMergeAttributes()
    {
        var attr = OneOf(
            Try(from __ in CommonParsers.InlineWhitespace from a in IdAttribute select (ICommitAttr)new IdAttr(a)),
            Try(from __ in CommonParsers.InlineWhitespace from a in TagAttribute select (ICommitAttr)new TagAttr(a)),
            Try(from __ in CommonParsers.InlineWhitespace from a in TypeAttribute select (ICommitAttr)new TypeAttr(a))
        );
        return attr.Many();
    }

    static MergeOperation CreateMerge(string name, IEnumerable<ICommitAttr> attrs)
    {
        var merge = new MergeOperation { BranchName = name };
        foreach (var attr in attrs)
        {
            switch (attr)
            {
                case IdAttr id: merge.Id = id.Value; break;
                case TagAttr tag: merge.Tag = tag.Value; break;
                case TypeAttr type: merge.Type = type.Value; break;
            }
        }
        return merge;
    }

    // Cherry-pick: cherry-pick id: "abc" tag: "v1.0"
    static Parser<char, CherryPickOperation> cherryPickParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("cherry-pick")
        from ___ in CommonParsers.InlineWhitespace
        from id in IdAttribute
        from tag in Try(
            from ____ in CommonParsers.InlineWhitespace
            from t in TagAttribute
            select t
        ).Optional()
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new CherryPickOperation
        {
            CommitId = id,
            Tag = tag.HasValue ? tag.Value : null
        };

    // Skip line (comments, empty lines)
    static Parser<char, Unit> skipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    // Main content parser
    public static Parser<char, List<GitOperation>> ParseContent()
    {
        var operation = OneOf(
            Try(commitParser.Select<GitOperation?>(_ => _)),
            Try(branchParser.Select<GitOperation?>(_ => _)),
            Try(checkoutParser.Select<GitOperation?>(_ => _)),
            Try(mergeParser.Select<GitOperation?>(_ => _)),
            Try(cherryPickParser.Select<GitOperation?>(_ => _)),
            skipLine.ThenReturn<GitOperation?>(null)
        );

        return operation.Many()
            .Select(_ => _.Where(_ => _ != null).Cast<GitOperation>().ToList());
    }

    // Options parser (gitGraph TB: or gitGraph LR:)
    static Parser<char, (string? direction, string? mainBranch)> optionsParser =
        from _ in CommonParsers.InlineWhitespace
        from options in Try(
            from dir in OneOf(
                Try(String("TB")).ThenReturn("TB"),
                Try(String("BT")).ThenReturn("BT"),
                Try(String("LR")).ThenReturn("LR"),
                String("RL").ThenReturn("RL")
            ).Optional()
            from __ in CommonParsers.InlineWhitespace
            from ___ in Char(':').Optional()
            select (dir.HasValue ? dir.Value : null, (string?)null)
        ).Optional()
        select options.HasValue ? options.Value : (null, null);

    public static Parser<char, GitGraphModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from keyword in CIString("gitGraph")
        from options in optionsParser
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        from operations in ParseContent()
        select BuildModel(operations, options);

    static GitGraphModel BuildModel(List<GitOperation> operations, (string? direction, string? mainBranch) options)
    {
        var model = new GitGraphModel();

        if (options.direction != null)
        {
            model.Direction = options.direction switch
            {
                "TB" or "TD" => Direction.TopToBottom,
                "BT" => Direction.BottomToTop,
                "LR" => Direction.LeftToRight,
                "RL" => Direction.RightToLeft,
                _ => Direction.LeftToRight
            };
        }
        else
        {
            model.Direction = Direction.LeftToRight; // Git graphs default to LR
        }

        if (options.mainBranch != null)
        {
            model.MainBranchName = options.mainBranch;
        }

        var order = 0;
        foreach (var op in operations)
        {
            op.Order = order++;
            model.Operations.Add(op);
        }

        return model;
    }

    public Result<char, GitGraphModel> Parse(string input) => Parser.Parse(input);

    internal interface ICommitAttr;
    readonly record struct IdAttr(string Value) : ICommitAttr;
    readonly record struct MsgAttr(string Value) : ICommitAttr;
    readonly record struct TagAttr(string Value) : ICommitAttr;
    readonly record struct TypeAttr(CommitType Value) : ICommitAttr;
}
