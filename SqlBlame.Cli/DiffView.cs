using DiffPlex.DiffBuilder.Model;
using Spectre.Console;
using SqlBlame.Core;

namespace SqlBlame.Cli;

internal static class DiffView
{
    public static async Task Show(Repository repo, DdlLogEntry entry)
    {
        AnsiConsole.Clear();
        var title = Theme.AsAccent($"{entry.DatabaseName}.{entry.SchemaName}.{entry.ObjectName} · {entry.EventTime} · {entry.User}");
        AnsiConsole.Write(new Rule(title).RuleStyle(Theme.Divider));

        var previous = await repo.GetPreviousVersion(entry);
        if (previous is null)
        {
            AnsiConsole.MarkupLine(Theme.AsMuted("first recorded version, nothing to compare with"));
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Text(entry.TsqlCommand));
        }
        else
        {
            AnsiConsole.MarkupLine(Theme.AsMuted($"compared to {previous.EventTime} · {previous.User}"));
            AnsiConsole.WriteLine();
            AnsiConsole.Write(RenderDiff(previous.TsqlCommand, entry.TsqlCommand));
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Markup(Theme.AsMuted("Press any key to go back..."));
        Console.ReadKey(intercept: true);
    }

    private static Rows RenderDiff(string oldText, string newText)
    {
        var rows = DiffBuilder.GetDiff(oldText, newText).Lines
            .Select(line => line.Type switch
            {
                ChangeType.Inserted => new Text("+ " + line.Text, Theme.DiffAdded),
                ChangeType.Deleted => new Text("- " + line.Text, Theme.DiffRemoved),
                _ => new Text("  " + line.Text, Theme.DiffContext),
            });

        return new Rows(rows);
    }
}
