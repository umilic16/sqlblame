using DiffPlex.DiffBuilder.Model;
using Spectre.Console;
using SqlBlame.Core;

namespace SqlBlame.Cli;

internal static class ChangeLogView
{
    public static async Task Run(Repository repo, List<string> selectedDbs)
    {
        const long RefreshChoice = -1;
        const long ExitChoice = -2;

        var showDbColumn = selectedDbs.Count > 1;
        var entries = new List<DdlLogEntry>();
        long lastId = 0;
        long selectedId = RefreshChoice;

        while (true)
        {
            var newEntries = await repo.GetAlterProcedureEntries(lastId, selectedDbs);
            foreach (var entry in newEntries)
            {
                entries.Insert(0, entry);
            }
            if (newEntries.Length > 0)
                lastId = newEntries[^1].Id;

            var byId = entries.ToDictionary(e => e.Id);
            var userWidth = entries.Select(e => e.User.Length).Append("user".Length).Max();
            var dbWidth = entries.Select(e => e.DatabaseName.Length).Append("db".Length).Max();
            string FormatRow(string time, string user, string db, string sp) => showDbColumn
                ? $"{time,-19}  {user.PadRight(userWidth)}  {db.PadRight(dbWidth)}  {sp}"
                : $"{time,-19}  {user.PadRight(userWidth)}  {sp}";

            var header = FormatRow("event-time", "user", "db", "sp-name");
            var prompt = new SelectionPrompt<long>()
                .Title($"{Theme.AsMuted("↑↓ move · type to search · Enter open · Esc quit")}\n  {Theme.AsAccent(header)}")
                .PageSize(Math.Max(3, Console.WindowHeight - 6))
                .EnableSearch()
                .HighlightStyle(Theme.Selected)
                .AddCancelResult(ExitChoice)
                .AddChoices(RefreshChoice)
                .AddChoices(entries.Select(e => e.Id))
                .DefaultValue(byId.ContainsKey(selectedId) ? selectedId : entries.FirstOrDefault()?.Id ?? RefreshChoice)
                .UseConverter(id => id == RefreshChoice
                    ? Theme.AsMuted("↻ refresh")
                    : Markup.Escape(FormatRow(
                        $"{byId[id].EventTime:yyyy-MM-dd HH:mm:ss}",
                        byId[id].User,
                        byId[id].DatabaseName,
                        $"{byId[id].SchemaName}.{byId[id].ObjectName}")));

            AnsiConsole.Clear();
            var choice = AnsiConsole.Prompt(prompt);
            if (choice == ExitChoice)
                break;

            selectedId = choice;
            if (choice == RefreshChoice)
                continue;

            var selected = byId[choice];
            AnsiConsole.Clear();
            var ruleLabel = Theme.AsAccent($"{selected.DatabaseName}.{selected.SchemaName}.{selected.ObjectName} · {selected.EventTime} · {selected.User}");
            AnsiConsole.Write(new Rule(ruleLabel).RuleStyle(Theme.Divider));
            var previous = await repo.GetPreviousVersion(selected);
            if (previous is null)
            {
                AnsiConsole.MarkupLine(Theme.AsMuted("first recorded version, nothing to compare with"));
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Text(selected.TsqlCommand));
            }
            else
            {
                AnsiConsole.MarkupLine(Theme.AsMuted($"compared to {previous.EventTime} · {previous.User}"));
                AnsiConsole.WriteLine();
                AnsiConsole.Write(RenderDiff(previous.TsqlCommand, selected.TsqlCommand));
            }
            AnsiConsole.WriteLine();
            AnsiConsole.Markup(Theme.AsMuted("Press any key to go back..."));
            Console.ReadKey(intercept: true);
        }
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
