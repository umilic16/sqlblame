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
    var serverPrompt = new TextPrompt<string>("server:").DefaultValue("localhost").ShowDefaultValue();
    var server = AnsiConsole.Prompt(serverPrompt);

    var dbPrompt = new TextPrompt<string>("database:").DefaultValue("DevAudit").ShowDefaultValue();
    var db = AnsiConsole.Prompt(dbPrompt);

    var userPrompt = new TextPrompt<string>("user:").DefaultValue("sa").ShowDefaultValue();
    var user = AnsiConsole.Prompt(userPrompt);

    var passPrompt = new TextPrompt<string>("pass:").DefaultValue("").ShowDefaultValue().Secret();
    var pass = AnsiConsole.Prompt(passPrompt);

    var connectionString = $"Server={server};Database={db};User ID={user};Password={pass};MultipleActiveResultSets=True;TrustServerCertificate=True";
    var connection = new SqlConnection(connectionString);

    var repo = new Repository(connection);
    var entries = await repo.GetAlterProcedureEntries();

    var table = new Table().AddColumn("event-time").AddColumn("user").AddColumn("schema-name").AddColumn("sp-name");
    foreach (var entry in entries)
    {
        table.AddRow(entry.EventTime.ToString(), entry.User, entry.SchemaName, entry.ObjectName);
    }

    AnsiConsole.Live(table)
        .Start(ctx =>
        {
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