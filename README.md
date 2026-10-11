# SqlBlame

A small .NET console tool for browsing the change history of SQL Server stored procedures, similar to `git log` / `git show`.

It reads a DDL log table filled by a server-level DDL trigger, lists the recorded changes and shows a colored diff between a selected version and the previous one.

> Early prototype

## How it works

- A server-level DDL trigger writes every `CREATE` / `ALTER` / `DROP` into a log table (`dbo.DdlLog` in an audit database).
- SqlBlame only **reads** that table. It never writes to the database.
- Pick one or more databases, browse the changes (arrow keys, type to search), press Enter to see the diff.
