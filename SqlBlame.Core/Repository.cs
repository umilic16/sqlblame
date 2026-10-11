using Dapper;
using Microsoft.Data.SqlClient;

namespace SqlBlame.Core;

public class Repository(SqlConnection connection)
{
    private readonly SqlConnection _connection = connection;

    public async Task<IEnumerable<string>> GetTsqlCommandBySpName(string schema, string sp)
    {
        var tsqlList = await _connection.QueryAsync<string>("""
            select TsqlCommand
            from dbo.DdlLog
            where SchemaName=@schema and ObjectName=@sp
            """, new { schema, sp });

        return tsqlList;
    }

    public async Task<DdlLogEntry[]> GetDdlLogEntryBySpName(string schema, string sp)
    {
        var tsqlList = await _connection.QueryAsync<DdlLogEntry>("""
            select *
            from dbo.DdlLog
            where SchemaName=@schema and ObjectName=@sp
            """, new { schema, sp });

        return [.. tsqlList];
    }

    public async Task<DdlLogEntry[]> GetAlterProcedureEntries(long afterId, List<string> dbs)
    {
        var entries = await _connection.QueryAsync<DdlLogEntry>("""
            select *
            from dbo.DdlLog
            where EventType in ('CREATE_PROCEDURE', 'ALTER_PROCEDURE') and Id > @afterId and DatabaseName in @dbs
            order by Id
            """, new { afterId, dbs });

        return [.. entries];
    }

    public async Task<string[]> GetDbs()
    {
        var dbs = await _connection.QueryAsync<string>("""
            select distinct(DatabaseName)
            from dbo.DdlLog
            where EventType in ('CREATE_PROCEDURE', 'ALTER_PROCEDURE')
            """);

        return [.. dbs];
    }

    public async Task<DdlLogEntry?> GetPreviousVersion(DdlLogEntry entry)
    {
        return await _connection.QueryFirstOrDefaultAsync<DdlLogEntry>("""
            select top 1 *
            from dbo.DdlLog
            where DatabaseName = @DatabaseName and SchemaName = @SchemaName and ObjectName = @ObjectName
              and EventType in ('CREATE_PROCEDURE', 'ALTER_PROCEDURE')
              and Id < @Id
            order by Id desc
            """, entry);
    }
}
