using DiffPlex.DiffBuilder.Model;
using Microsoft.Data.SqlClient;
using SqlBlame.Core;

var server = "localhost";
var db = "DevAudit";
var user = "sa";
var pass = "";
var connectionString = $"Server={server};Database={db};User ID={user};Password={pass};MultipleActiveResultSets=True;TrustServerCertificate=True";
var connection = new SqlConnection(connectionString);

var repo = new Repository(connection);
var entries = await repo.GetDdlLogEntryBySpName("upZ", "PregledLagerListe");

var commands = entries.Select(x => x.TsqlCommand).ToArray();
for (int i = entries.Length - 1; i > 1; i--)
{
    var diff = DiffBuilder.GetDiff(commands[i - 1], commands[i]);

    Console.WriteLine(entries[i]);
    var savedColor = Console.ForegroundColor;
    foreach (var line in diff.Lines)
    {
        switch (line.Type)
        {
            case ChangeType.Inserted:
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("+ ");
                break;
            case ChangeType.Deleted:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("- ");
                break;
            default:
                Console.ForegroundColor = ConsoleColor.Gray; // compromise for dark or light background
                Console.Write("  ");
                break;
        }

        Console.WriteLine(line.Text);
    }
    Console.ForegroundColor = savedColor;
}