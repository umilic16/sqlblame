using System.Reflection;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using SqlBlame.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;
const string AppName = "SqlBlame";
try
{
    var config = new ConfigurationBuilder()
        .AddJsonFile(SqlBlameConfig.FilePath, optional: true)
        .Build();

    var sqlBlameConfig = config.Get<SqlBlameConfig>() ?? new();
    var figlet = new FigletText(AppName).Centered().Color(Theme.Logo);
    var fullVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown";
    var semVersion = fullVersion.Split('+')[0];
    var versionText = new Markup($"{Theme.AsMuted("Version")} {Theme.AsAccent(semVersion)}\n").Centered();
    AnsiConsole.Write(figlet);
    AnsiConsole.Write(versionText);
    AnsiConsole.Record();

    var (repo, selectedDbs) = await ConnectionPrompt.Run(sqlBlameConfig);

    await ChangeLogView.Run(repo, selectedDbs);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine(Theme.AsError($"{AppName} encountered an unexpected error."));
    AnsiConsole.WriteException(ex);
    await ExportLog(SqlBlameConfig.DefaultLogsDirectory);
    AnsiConsole.Markup(Theme.AsMuted("Press any key to exit..."));
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
        AnsiConsole.MarkupLine($"{Theme.AsMuted("Logs exported to:")} [link]{Markup.Escape(logFilePath)}[/]");
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine(Theme.AsError("Failed to export error log."));
        AnsiConsole.WriteException(ex);
    }
    finally
    {
        Environment.ExitCode = 1;
    }
}
