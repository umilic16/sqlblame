using Microsoft.Data.SqlClient;
using Spectre.Console;
using SqlBlame.Core;

namespace SqlBlame.Console;

internal static class ConnectionPrompt
{
    public static async Task<(Repository Repository, List<string> SelectedDbs)> Run(SqlBlameConfig sqlBlameConfig)
    {
        var serverPrompt = new TextPrompt<string>("server:").DefaultValue(sqlBlameConfig.ServerName).ShowDefaultValue();
        var server = AnsiConsole.Prompt(serverPrompt);

        var dbPrompt = new TextPrompt<string>("log database:").DefaultValue(sqlBlameConfig.Database).ShowDefaultValue();
        var db = AnsiConsole.Prompt(dbPrompt);

        var userPrompt = new TextPrompt<string>("user:").DefaultValue(sqlBlameConfig.User).ShowDefaultValue();
        var user = AnsiConsole.Prompt(userPrompt);

        var passPrompt = new TextPrompt<string>("pass:").DefaultValue(sqlBlameConfig.Password).ShowDefaultValue().Secret();
        var pass = AnsiConsole.Prompt(passPrompt);

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = db,
            UserID = user,
            Password = pass,
            MultipleActiveResultSets = true,
            TrustServerCertificate = true
        };
        var connection = new SqlConnection(builder.ConnectionString);

        var repo = new Repository(connection);

        var dbs = await repo.GetDbs();

        var dbsPrompt = new MultiSelectionPrompt<string>().Title("Select one or more DBs").AddChoices(dbs);
        foreach (var savedDb in sqlBlameConfig.SelectedDatabases.Intersect(dbs))
        {
            dbsPrompt.Select(savedDb);
        }
        var selectedDbs = AnsiConsole.Prompt(dbsPrompt);

        var defaultsChanged = server != sqlBlameConfig.ServerName || db != sqlBlameConfig.Database || user != sqlBlameConfig.User
            || !selectedDbs.ToHashSet().SetEquals(sqlBlameConfig.SelectedDatabases);
        var defaultsSaved = false;
        if (defaultsChanged && AnsiConsole.Confirm("Save server, user and selected DBs as defaults?", defaultValue: false))
        {
            await SqlBlameConfig.SaveDefaults(server, db, user, selectedDbs);
            defaultsSaved = true;
            AnsiConsole.MarkupLineInterpolated($"[dim]Saved to {SqlBlameConfig.FilePath}[/]");
        }

        // only offer it when the saved server and user match, otherwise the password would end up next to another login
        var configMatchesLogin = defaultsSaved || (server == sqlBlameConfig.ServerName && user == sqlBlameConfig.User);
        var passChanged = pass.Length > 0 && pass != sqlBlameConfig.Password;
        if (configMatchesLogin && passChanged
            && AnsiConsole.Confirm("Save password too? [yellow](stored as plain text)[/]", defaultValue: false))
        {
            await SqlBlameConfig.SaveDefaultPassword(pass);
            AnsiConsole.MarkupLineInterpolated($"[dim]Saved to {SqlBlameConfig.FilePath}[/]");
        }

        return (repo, selectedDbs);
    }
}
