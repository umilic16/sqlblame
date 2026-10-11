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

    public async Task<DdlLogEntry[]> GetAlterProcedureEntries()
    {
        var tsqlList = await _connection.QueryAsync<DdlLogEntry>("""
            select *
            from dbo.DdlLog
            where EventType = 'ALTER_PROCEDURE'
            order by EventTime desc
            """);

        return [.. tsqlList];
    }

}
