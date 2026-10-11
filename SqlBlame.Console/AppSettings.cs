namespace SqlBlame.Console;

public class AppSettings
{
    public string LogsDirectory { get; init; } = ".logs";
    public bool AutoClose { get; init; }
}