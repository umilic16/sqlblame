using Microsoft.Data.SqlClient;
using SqlBlame.Core;

var server = "localhost";
var db = "DevAudit";
var user = "sa";
var pass = "";
var connectionString = $"Server={server};Database={db};User ID={user};Password={pass};MultipleActiveResultSets=True;TrustServerCertificate=True";
var connection = new SqlConnection(connectionString);

var repo = new Repository(connection);
var tsqlList = await repo.GetBySpName("upZ", "PregledLagerListe");

foreach (var tsql in tsqlList)
{
    Console.WriteLine(tsql);
}