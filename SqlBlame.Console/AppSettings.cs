namespace SqlBlame.Console;

public class AppSettings
{
    public string LogsDirectory { get; init; } = ".logs";
    public bool AutoClose { get; init; }
    public DefaultValues DefaultValues { get; init; } = new();
}

public class DefaultValues
{
    public string Server { get; set; } = "localhost";
    public string Database { get; set; } = "DevAudit";
    public string User { get; set; } = "sa";
    public string Password { get; set; } = "";
}