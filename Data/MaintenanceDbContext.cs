using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SQLiteEncrypted.MauiMaintenanceLog.Models;

namespace SQLiteEncrypted.MauiMaintenanceLog.Data;

internal sealed class MaintenanceDbContext : DbContext
{
    private readonly SqliteConnection _connection;

    internal MaintenanceDbContext(SqliteConnection connection)
    {
        _connection = connection;
    }

    internal DbSet<MaintenanceRecord> Records => Set<MaintenanceRecord>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite(_connection);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var record = modelBuilder.Entity<MaintenanceRecord>();
        record.ToTable("maintenance_records");
        record.HasKey(item => item.Id);
        record.Property(item => item.Title).IsRequired();
        record.Property(item => item.Location).IsRequired();
        record.Property(item => item.Notes).IsRequired();
    }

    public override void Dispose()
    {
        base.Dispose();
        _connection.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

