using Dapper;
using Microsoft.Data.SqlClient;

namespace SqlBlame.Core;

public class Repository(SqlConnection connection)
{
    private readonly SqlConnection _connection = connection;

    public async Task<IEnumerable<string>> GetBySpName(string schema, string sp)
    {
        var tsqlList = await _connection.QueryAsync<string>("""
            select TsqlCommand
            from dbo.DdlLog
            where SchemaName=@schema and ObjectName=@sp
            order by EventTime desc
            """, new { schema, sp });

        return tsqlList;
    }
}
