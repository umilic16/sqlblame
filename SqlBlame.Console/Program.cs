using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Spectre.Console;
using SqlBlame.Console;
using SqlBlame.Core;

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

    var table = new Table().AddColumn("event-time").AddColumn("user");
    if (selectedDbs.Count > 1)
        table.AddColumn("db").AddColumn("schema-name").AddColumn("sp-name");
    else
        table.AddColumn("schema-name").AddColumn("sp-name");

    long lastId = 0;
    var entries = await repo.GetAlterProcedureEntries(lastId, selectedDbs);
    var showDbColumn = selectedDbs.Count > 1;
    AddEntriesToTable(table, entries, showDbColumn);
    if (entries.Length > 0)
        lastId = entries[^1].Id;

    await AnsiConsole.Live(table)
        .StartAsync(async ctx =>
        {
            ctx.Refresh();
            while (true)
            {
                var entries = await repo.GetAlterProcedureEntries(lastId, selectedDbs);
                AddEntriesToTable(table, entries, showDbColumn);
                if (entries.Length > 0)
                {
                    lastId = entries[^1].Id;
                    ctx.Refresh();
                }
                await Task.Delay(1000);
            }
        });

    //var commands = entries.Select(x => x.TsqlCommand).ToArray();
    //for (int i = entries.Length - 1; i > 1; i--)
    //{
    //    var diff = DiffBuilder.GetDiff(commands[i - 1], commands[i]);

    //    Console.WriteLine(entries[i]);
    //    var savedColor = Console.ForegroundColor;
    //    foreach (var line in diff.Lines)
    //    {
    //        switch (line.Type)
    //        {
    //            case ChangeType.Inserted:
    //                Console.ForegroundColor = ConsoleColor.Green;
    //                Console.Write("+ ");
    //                break;
    //            case ChangeType.Deleted:
    //                Console.ForegroundColor = ConsoleColor.Red;
    //                Console.Write("- ");
    //                break;
    //            default:
    //                Console.ForegroundColor = ConsoleColor.Gray; // compromise for dark or light background
    //                Console.Write("  ");
    //                break;
    //        }

    //        Console.WriteLine(line.Text);
    //    }
    //    Console.ForegroundColor = savedColor;
    //}
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

static void AddEntriesToTable(Table table, DdlLogEntry[] entries, bool showDbColumn)
{
    foreach (var entry in entries)
    {
        string[] cells;
        if (showDbColumn)
            cells = [entry.EventTime.ToString(), entry.User, entry.DatabaseName, entry.SchemaName, entry.ObjectName];
        else
            cells = [entry.EventTime.ToString(), entry.User, entry.SchemaName, entry.ObjectName];


        table.InsertRow(0, [.. cells.Select(Markup.Escape)]);
    }
}