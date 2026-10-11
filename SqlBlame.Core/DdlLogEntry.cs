namespace SqlBlame.Core;

public record DdlLogEntry(long Id, DateTime EventTime, string OriginalLogin, string HostName, string SchemaName, string ObjectName, string TsqlCommand)
{
    private DdlLogEntry() : this(default, default, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty) { }
    public override string ToString()
    {
        return $"""

            ---------------------------------------------------
            [{EventTime}] {OriginalLogin} / {HostName}
            """;
    }

    public string User => $"{OriginalLogin} ({HostName})";
}
