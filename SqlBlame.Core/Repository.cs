using Dapper;
using Microsoft.Data.SqlClient;

namespace SqlBlame.Core;

public class Repository(SqlConnection connection)
{
    private static readonly string[] s_procedureEvents = ["CREATE_PROCEDURE", "ALTER_PROCEDURE"];

    private readonly SqlConnection _connection = connection;

    public async Task<DdlLogEntry[]> GetProcedureChanges(long afterId, List<string> dbs)
    {
        var entries = await _connection.QueryAsync<DdlLogEntry>("""
            select *
            from dbo.DdlLog
            where EventType in @eventTypes and Id > @afterId and DatabaseName in @dbs
            order by Id
            """, new { eventTypes = s_procedureEvents, afterId, dbs });

        return [.. entries];
    }

    public async Task<string[]> GetDbs()
    {
        var dbs = await _connection.QueryAsync<string>("""
            select distinct(DatabaseName)
            from dbo.DdlLog
            where EventType in @eventTypes
            """, new { eventTypes = s_procedureEvents });

        return [.. dbs];
    }

    public async Task<DdlLogEntry?> GetPreviousVersion(DdlLogEntry entry)
    {
        return await _connection.QueryFirstOrDefaultAsync<DdlLogEntry>("""
            select top 1 *
            from dbo.DdlLog
            where DatabaseName = @DatabaseName and SchemaName = @SchemaName and ObjectName = @ObjectName
              and EventType in @eventTypes
              and Id < @Id
            order by Id desc
            """, new { entry.DatabaseName, entry.SchemaName, entry.ObjectName, entry.Id, eventTypes = s_procedureEvents });
    }
}
