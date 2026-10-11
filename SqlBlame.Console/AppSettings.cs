namespace SqlBlame.Console;

public class AppSettings
{
    public string LogsDirectory { get; init; } = ".logs";
    public bool AutoClose { get; init; }
    public ServerSettings ServerSettings { get; init; } = new();
}

public class ServerSettings
{
    public string ServerName { get; set; } = "localhost";
    public string Database { get; set; } = "DevAudit";
    public string User { get; set; } = "sa";
    public string Password { get; set; } = "";
}