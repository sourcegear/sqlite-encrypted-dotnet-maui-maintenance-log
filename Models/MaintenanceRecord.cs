namespace SQLiteEncrypted.MauiMaintenanceLog.Models;

internal sealed class MaintenanceRecord
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Location { get; set; }
    public required string Notes { get; set; }
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string StatusDisplay => IsComplete ? "Complete" : "Open";
    public string UpdatedDisplay =>
        DateTime.SpecifyKind(UpdatedAt, DateTimeKind.Utc).ToLocalTime().ToString("g");
}

