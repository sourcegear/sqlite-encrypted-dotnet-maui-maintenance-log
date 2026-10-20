# SQLite.Encrypted .NET MAUI maintenance log

This Android and iOS sample combines the published `SQLite.Encrypted` NuGet
package with .NET MAUI and Entity Framework Core. It tracks private home or
equipment maintenance records with encrypted CRUD, search, and completion
status.

The project deliberately references `Microsoft.EntityFrameworkCore.Sqlite.Core`
instead of `Microsoft.EntityFrameworkCore.Sqlite`. The `Core` package does not
select a bundled native SQLite library; `SQLitePCLRaw.provider.e_sqlite3`
selects the `e_sqlite3` binary supplied by `SQLite.Encrypted`.

## Requirements

- .NET 10 SDK with the MAUI Android and/or iOS workload
- a SQLite.Encrypted activation key valid for the device platform and SQLite 3.54

Free fully-featured demo keys are available from
[sqlite.sourcegear.com](https://sqlite.sourcegear.com/#evaluation).

## Run on Android

```powershell
cd sqlite-encrypted-dotnet-maui-maintenance-log
dotnet restore -p:TargetFramework=net10.0-android
dotnet build -f net10.0-android
dotnet build -t:Run -f net10.0-android
```

For iOS, run `dotnet build -t:Run -f net10.0-ios` on a Mac with an available
simulator or device.

Enter the activation key without the `see-` prefix and choose a database
passphrase on the unlock screen. The sample keeps both values only in memory.
The encrypted database is stored as `maintenance.db` in the app's private
application-data directory.

## How EF Core reaches SQLite.Encrypted

1. `MauiProgram` selects and freezes `SQLite3Provider_e_sqlite3` before MAUI starts.
2. Every context receives a non-pooled `Microsoft.Data.Sqlite` connection.
3. The connection activates SEE, calls `PRAGMA key`, and requires the result `ok`.
4. A schema query verifies the database passphrase before EF performs any work.
5. `MaintenanceDbContext` maps the model and EF Core performs CRUD and search.

Every new connection is explicitly keyed before Entity Framework sees it; the
sample does not use SQLite URI key parameters.

