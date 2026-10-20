using SQLitePCL;

namespace SQLiteEncrypted.MauiMaintenanceLog;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        raw.SetProvider(new SQLite3Provider_e_sqlite3());
        raw.FreezeProvider();

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        return builder.Build();
    }
}

