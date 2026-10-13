# SqlBlame

A small .NET console tool for browsing the change history of SQL Server stored procedures, similar to `git log` / `git show`.

It reads a DDL log table filled by a server-level DDL trigger, lists the recorded changes and shows a colored diff between a selected version and the previous one.

> Early prototype

## How it works

- A server-level DDL trigger writes every `CREATE` / `ALTER` / `DROP` into a log table (`dbo.DdlLog` in an audit database).
- SqlBlame only **reads** that table. It never writes to the database.
- Pick one or more databases, browse the changes (arrow keys, type to search), press Enter to see the diff.

## Configuration

Connection values are entered on start. After connecting, SqlBlame offers to save them as defaults in:

- Windows: `%LOCALAPPDATA%\sqlblame\config.json`
- Linux / macOS: `~/.local/share/sqlblame/config.json`

```json
{
  "ServerName": "localhost",
  "Database": "DevAudit",
  "User": "sa",
  "SelectedDatabases": [ "MyDb" ]
}
```

The password is saved only if you confirm it, and it is stored as plain text.

Values can also be overridden with environment variables (`SQLBLAME_ServerName`) or command-line arguments (`--ServerName=srv01`).
