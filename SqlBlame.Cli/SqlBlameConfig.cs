using System.Text.Json;
using System.Text.Json.Nodes;

namespace SqlBlame.Cli;

public class SqlBlameConfig
{
    public string ServerName { get; set; } = "localhost";
    public string Database { get; set; } = "DevAudit";
    public string User { get; set; } = "sa";
    public string Password { get; set; } = "";
    public string[] SelectedDatabases { get; init; } = [];

    private static readonly string s_configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sqlblame");
    private static readonly JsonSerializerOptions s_writeOptions = new() { WriteIndented = true };

    internal static string FilePath { get; } = Path.Combine(s_configDirectory, "config.json");
    internal static string DefaultLogsDirectory { get; } = Path.Combine(s_configDirectory, "logs");

    internal static async Task SaveDefaults(string server, string database, string user, IEnumerable<string> selectedDatabases)
    {
        var root = await Load();
        root[nameof(ServerName)] = server;
        root[nameof(Database)] = database;
        root[nameof(User)] = user;

        var databases = new JsonArray();
        foreach (var name in selectedDatabases)
        {
            databases.Add(JsonValue.Create(name));
        }
        root[nameof(SelectedDatabases)] = databases;

        await Save(root);
    }

    internal static async Task SaveDefaultPassword(string password)
    {
        var root = await Load();
        root[nameof(Password)] = password;
        await Save(root);
    }

    private static async Task<JsonObject> Load()
    {
        return File.Exists(FilePath)
            ? JsonNode.Parse(await File.ReadAllTextAsync(FilePath)) as JsonObject ?? []
            : [];
    }

    private static async Task Save(JsonObject root)
    {
        Directory.CreateDirectory(s_configDirectory);
        await File.WriteAllTextAsync(FilePath, root.ToJsonString(s_writeOptions));
    }
}
