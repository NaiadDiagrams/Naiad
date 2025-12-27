public class KanbanRendererTests
{
    [Test]
    public Task Render_SimpleKanban()
    {
        const string input = """
            kanban
            todo[Todo]
                task1[First Task]
                task2[Second Task]
            done[Done]
                task3[Completed Task]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_ThreeColumns()
    {
        const string input = """
            kanban
            todo[To Do]
                t1[Research]
                t2[Design]
            wip[In Progress]
                t3[Development]
            done[Done]
                t4[Testing]
                t5[Review]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_EmptyColumns()
    {
        const string input = """
            kanban
            backlog[Backlog]
            todo[To Do]
            done[Done]
                t1[Task 1]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_ManyTasks()
    {
        const string input = """
            kanban
            col1[Sprint Backlog]
                t1[User Story 1]
                t2[User Story 2]
                t3[User Story 3]
                t4[User Story 4]
                t5[User Story 5]
            col2[In Progress]
                t6[Feature A]
                t7[Feature B]
            col3[Review]
                t8[Bug Fix 1]
            col4[Done]
                t9[Setup]
                t10[Configuration]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_SingleColumn()
    {
        const string input = """
            kanban
            tasks[All Tasks]
                t1[Task One]
                t2[Task Two]
                t3[Task Three]
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
