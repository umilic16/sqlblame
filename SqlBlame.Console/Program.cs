using System.Reflection;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Rendering;
using SqlBlame.Console;
using SqlBlame.Core;

Console.OutputEncoding = System.Text.Encoding.UTF8;
AppSettings appSettings = new();
const string AppName = "SqlBlame";
try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddOptions<AppSettings>()
                    .Bind(builder.Configuration)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

    var app = builder.Build();
    appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>().Value;
    var figlet = new FigletText(AppName).Centered().Color(Theme.Primary);
    var fullVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
    var semVersion = fullVersion.Split('+')[0];
    var versionText = new Markup($"[dim]Version [bold {Theme.Primary}]{semVersion}[/][/]\n").Centered();
    AnsiConsole.Write(figlet);
    AnsiConsole.Write(versionText);
    AnsiConsole.Record();
    var serverPrompt = new TextPrompt<string>("server:").DefaultValue(appSettings.ServerSettings.ServerName).ShowDefaultValue();
    var server = AnsiConsole.Prompt(serverPrompt);

    var dbPrompt = new TextPrompt<string>("log database:").DefaultValue(appSettings.ServerSettings.Database).ShowDefaultValue();
    var db = AnsiConsole.Prompt(dbPrompt);

    var userPrompt = new TextPrompt<string>("user:").DefaultValue(appSettings.ServerSettings.User).ShowDefaultValue();
    var user = AnsiConsole.Prompt(userPrompt);

    var passPrompt = new TextPrompt<string>("pass:").DefaultValue(appSettings.ServerSettings.Password).ShowDefaultValue().Secret();
    var pass = AnsiConsole.Prompt(passPrompt);

    var connectionString = $"Server={server};Database={db};User ID={user};Password={pass};MultipleActiveResultSets=True;TrustServerCertificate=True";
    var connection = new SqlConnection(connectionString);

    var repo = new Repository(connection);

    var dbs = await repo.GetDbs();

    var selectedDbs = AnsiConsole.Prompt(
        new MultiSelectionPrompt<string>().Title("Select one or more DBs").AddChoices(dbs));

    AnsiConsole.WriteLine("selected DBs: {0}", string.Join(", ", selectedDbs));

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
            .Title($"[dim]↑↓ move · type to search · Enter open · Esc quit[/]\n[bold]  {Markup.Escape(header)}[/]")
            .PageSize(Math.Max(3, Console.WindowHeight - 6))
            .EnableSearch()
            .AddCancelResult(ExitChoice)
            .AddChoices(RefreshChoice)
            .AddChoices(entries.Select(e => e.Id))
            .DefaultValue(byId.ContainsKey(selectedId) ? selectedId : entries.FirstOrDefault()?.Id ?? RefreshChoice)
            .UseConverter(id => id == RefreshChoice
                ? "[dim]↻ refresh[/]"
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
        AnsiConsole.Write(new Rule(Markup.Escape($"{selected.DatabaseName}.{selected.SchemaName}.{selected.ObjectName} · {selected.EventTime} · {selected.User}")));
        var previous = await repo.GetPreviousVersion(selected);
        if (previous is null)
        {
            AnsiConsole.MarkupLine("[dim]first recorded version, nothing to compare with[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Text(selected.TsqlCommand));
        }
        else
        {
            AnsiConsole.MarkupLine($"[dim]compared to {previous.EventTime} · {Markup.Escape(previous.User)}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.Write(RenderDiff(previous.TsqlCommand, selected.TsqlCommand));
        }
        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[dim]Press any key to go back...[/]");
        Console.ReadKey(intercept: true);
    }
}
catch (Exception ex)
{
    AnsiConsole.MarkupLineInterpolated($"[{Theme.Error}]{AppName} encountered an unexpected error.[/]");
    AnsiConsole.WriteException(ex);
    await ExportLog(appSettings.LogsDirectory);
}
if (!appSettings.AutoClose && Environment.UserInteractive && !Console.IsOutputRedirected && !Console.IsInputRedirected)
{
    AnsiConsole.Markup("[dim]Press any key to exit...[/]");
    Console.ReadKey(intercept: true);
}

static async Task ExportLog(string directory)
{
    try
    {
        var logContent = AnsiConsole.ExportText();
        var logDirectory = Directory.CreateDirectory(directory);
        var logFilePath = Path.Combine(logDirectory.FullName, $"{DateTime.Now:yyyyMMddHHmmss}.txt");

        await File.WriteAllTextAsync(logFilePath, logContent);
        AnsiConsole.MarkupLineInterpolated($"[dim]Logs exported to:[/] [white link]{Markup.Escape(logFilePath)}[/]");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLineInterpolated($"[{Theme.Error}]Failed to export error log.[/]");
        AnsiConsole.WriteException(ex);
    }
    finally
    {
        Environment.ExitCode = 1;
    }
}

static IRenderable RenderDiff(string oldText, string newText)
{
    var rows = DiffBuilder.GetDiff(oldText, newText).Lines
        .Select(line => line.Type switch
        {
            ChangeType.Inserted => new Text("+ " + line.Text, new Style(Color.Green)),
            ChangeType.Deleted => new Text("- " + line.Text, new Style(Color.Red)),
            _ => new Text("  " + line.Text, new Style(Color.Grey)),
        });

    return new Rows(rows);
}