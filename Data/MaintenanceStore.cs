using Microsoft.EntityFrameworkCore;
using SQLiteEncrypted.MauiMaintenanceLog.Models;

namespace SQLiteEncrypted.MauiMaintenanceLog.Data;

internal sealed class MaintenanceStore
{
    private readonly string _databasePath;
    private readonly string _activationKey;
    private readonly string _databaseKey;

    internal MaintenanceStore(string activationKey, string databaseKey)
    {
        _activationKey = activationKey;
        _databaseKey = databaseKey;
        _databasePath = Path.Combine(FileSystem.AppDataDirectory, "maintenance.db");
    }

    internal async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    internal async Task<IReadOnlyList<MaintenanceRecord>> ListAsync(string? search)
    {
        await using var context = CreateContext();
        var query = context.Records.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.Like(item.Title, pattern) ||
                EF.Functions.Like(item.Location, pattern) ||
                EF.Functions.Like(item.Notes, pattern));
        }
        return await query
            .OrderBy(item => item.IsComplete)
            .ThenByDescending(item => item.UpdatedAt)
            .ToListAsync();
    }

    internal async Task<MaintenanceRecord> AddAsync(
        string title,
        string location,
        string notes,
        bool isComplete)
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;
        var record = new MaintenanceRecord
        {
            Title = title,
            Location = location,
            Notes = notes,
            IsComplete = isComplete,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.Records.Add(record);
        await context.SaveChangesAsync();
        return record;
    }

    internal async Task<bool> UpdateAsync(
        int id,
        string title,
        string location,
        string notes,
        bool isComplete)
    {
        await using var context = CreateContext();
        var record = await context.Records.SingleOrDefaultAsync(item => item.Id == id);
        if (record is null)
        {
            return false;
        }
        record.Title = title;
        record.Location = location;
        record.Notes = notes;
        record.IsComplete = isComplete;
        record.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }

    internal async Task<bool> DeleteAsync(int id)
    {
        await using var context = CreateContext();
        var deleted = await context.Records
            .Where(item => item.Id == id)
            .ExecuteDeleteAsync();
        return deleted == 1;
    }

    private MaintenanceDbContext CreateContext() => new(
        EncryptedConnection.Open(_databasePath, _activationKey, _databaseKey));
}
