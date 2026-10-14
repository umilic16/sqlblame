using Microsoft.Data.SqlClient;
using Spectre.Console;
using SqlBlame.Core;

namespace SqlBlame.Cli;

internal static class ConnectionPrompt
{
    public static async Task<(Repository Repository, List<string> SelectedDbs)> Run(SqlBlameConfig sqlBlameConfig)
    {
        // after a failed attempt, the values just entered become the defaults for the next one
        var (server, db, user) = (sqlBlameConfig.ServerName, sqlBlameConfig.Database, sqlBlameConfig.User);
        while (true)
        {
            var serverPrompt = new TextPrompt<string>(Theme.AsAccent("server"))
                .DefaultValue(server)
                .DefaultValueStyle(Theme.DefaultValue)
                .ShowDefaultValue();

            server = AnsiConsole.Prompt(serverPrompt);

            var dbPrompt = new TextPrompt<string>(Theme.AsAccent("log database"))
                .DefaultValue(db)
                .DefaultValueStyle(Theme.DefaultValue)
                .ShowDefaultValue();

            db = AnsiConsole.Prompt(dbPrompt);

            var userPrompt = new TextPrompt<string>(Theme.AsAccent("user"))
                .DefaultValue(user)
                .DefaultValueStyle(Theme.DefaultValue)
                .ShowDefaultValue();

            user = AnsiConsole.Prompt(userPrompt);

            var passPrompt = new TextPrompt<string>(Theme.AsAccent("password"))
                .DefaultValue(sqlBlameConfig.Password)
                .DefaultValueStyle(Theme.DefaultValue)
                .ShowDefaultValue(sqlBlameConfig.Password.Length > 0)
                .Secret();

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
            try
            {
                await connection.OpenAsync();
                await connection.CloseAsync();
            }
            catch (SqlException ex)
            {
                await connection.DisposeAsync();
                ShowError("Cannot connect.", ex.Message);
                continue;
            }

            var repo = new Repository(connection);
            string[] dbs;
            try
            {
                dbs = await repo.GetDbs();
            }
            catch (SqlException ex)
            {
                await connection.DisposeAsync();
                ShowError("Connected, but reading the DDL log failed.", ex.Message);
                continue;
            }

            if (dbs.Length == 0)
            {
                await connection.DisposeAsync();
                ShowError("No procedure changes found in the DDL log.");
                continue;
            }

            var dbsPrompt = new MultiSelectionPrompt<string>()
                .Title("\n" + Theme.AsAccent("Select one or more DBs"))
                .AddChoices(dbs)
                .HighlightStyle(Theme.Selected);

            foreach (var savedDb in sqlBlameConfig.SelectedDatabases.Intersect(dbs))
            {
                dbsPrompt.Select(savedDb);
            }
            var selectedDbs = AnsiConsole.Prompt(dbsPrompt);

            var defaultsChanged = server != sqlBlameConfig.ServerName || db != sqlBlameConfig.Database || user != sqlBlameConfig.User
                || !selectedDbs.ToHashSet().SetEquals(sqlBlameConfig.SelectedDatabases);
            var defaultsSaved = false;
            if (defaultsChanged && Confirm("Save server, user and selected DBs as defaults?"))
            {
                await SqlBlameConfig.SaveDefaults(server, db, user, selectedDbs);
                defaultsSaved = true;
                ShowSaved();
            }

            // only offer it when the saved server and user match, otherwise the password would end up next to another login
            var configMatchesLogin = defaultsSaved || (server == sqlBlameConfig.ServerName && user == sqlBlameConfig.User);
            var passChanged = pass.Length > 0 && pass != sqlBlameConfig.Password;
            if (configMatchesLogin && passChanged
                && Confirm($"Save password too? {Theme.AsWarning("(stored as plain text)")}"))
            {
                await SqlBlameConfig.SaveDefaultPassword(pass);
                ShowSaved();
            }

            return (repo, selectedDbs);
        }
    }

    private static void ShowError(string message, string? details = null)
    {
        AnsiConsole.MarkupLine(Theme.AsError(message));
        if (details is not null)
            AnsiConsole.MarkupLine(Theme.AsMuted(details));
    }

    private static void ShowSaved()
    {
        AnsiConsole.MarkupLine($"{Theme.AsSuccess("✓ Saved")} {Theme.AsMuted($"to {SqlBlameConfig.FilePath}")}");
    }

    private static bool Confirm(string question)
    {
        var prompt = new ConfirmationPrompt(question)
        {
            DefaultValue = false,
            ChoicesStyle = Theme.Choices,
            DefaultValueStyle = Theme.DefaultValue,
            InvalidChoiceMessage = Theme.AsError("Please press y or n"),
        };
        return AnsiConsole.Prompt(prompt);
    }
}
