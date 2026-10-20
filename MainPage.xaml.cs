using System.Collections.ObjectModel;
using SQLiteEncrypted.MauiMaintenanceLog.Data;
using SQLiteEncrypted.MauiMaintenanceLog.Models;

namespace SQLiteEncrypted.MauiMaintenanceLog;

public partial class MainPage : ContentPage
{
    private readonly ObservableCollection<MaintenanceRecord> _records = [];
    private MaintenanceStore? _store;
    private int? _selectedId;

    public MainPage()
    {
        InitializeComponent();
        RecordsView.ItemsSource = _records;
        ActivationKeyEntry.Text = Environment.GetEnvironmentVariable(
            "SQLITE_ENCRYPTED_ACTIVATION_KEY") ?? string.Empty;
    }

    private async void UnlockClicked(object? sender, EventArgs e)
    {
        UnlockErrorBorder.IsVisible = false;
        var activationKey = ActivationKeyEntry.Text?.Trim() ?? string.Empty;
        var databaseKey = DatabaseKeyEntry.Text ?? string.Empty;
        if (activationKey.Length == 0 || databaseKey.Length == 0)
        {
            ShowUnlockError("Both the activation key and database passphrase are required.");
            return;
        }

        UnlockButton.IsEnabled = false;
        var retryAllowed = true;
        try
        {
            _store = new MaintenanceStore(activationKey, databaseKey);
            await _store.InitializeAsync();
            ActivationKeyEntry.Text = string.Empty;
            DatabaseKeyEntry.Text = string.Empty;
            UnlockPanel.IsVisible = false;
            Workspace.IsVisible = true;
            await ReloadAsync();
            NewRecord();
        }
        catch (SeeActivationException failure)
        {
            retryAllowed = false;
            ShowUnlockError(failure.Message);
            return;
        }
        catch (Exception failure)
        {
            ShowUnlockError(
                "The log could not be opened. If it already exists, verify the " +
                $"database passphrase. Details: {failure.Message}");
        }
        finally
        {
            if (!Workspace.IsVisible)
            {
                UnlockButton.IsEnabled = retryAllowed;
            }
        }
    }

    private async void SearchChanged(object? sender, TextChangedEventArgs e) =>
        await ReloadAsync();

    private void NewClicked(object? sender, EventArgs e) => NewRecord();

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not MaintenanceRecord record)
        {
            return;
        }
        _selectedId = record.Id;
        EditorHeading.Text = "Edit maintenance item";
        TitleEntry.Text = record.Title;
        LocationEntry.Text = record.Location;
        NotesEditor.Text = record.Notes;
        CompleteSwitch.IsToggled = record.IsComplete;
        DeleteButton.IsVisible = true;
        StatusLabel.Text = $"Updated {record.UpdatedDisplay}";
    }

    private async void SaveClicked(object? sender, EventArgs e)
    {
        if (_store is null)
        {
            return;
        }
        var title = TitleEntry.Text?.Trim() ?? string.Empty;
        var location = LocationEntry.Text?.Trim() ?? string.Empty;
        var notes = NotesEditor.Text?.Trim() ?? string.Empty;
        if (title.Length == 0 || location.Length == 0)
        {
            StatusLabel.Text = "Title and location are required.";
            return;
        }

        try
        {
            if (_selectedId is int id)
            {
                await _store.UpdateAsync(id, title, location, notes, CompleteSwitch.IsToggled);
                StatusLabel.Text = "Item updated.";
            }
            else
            {
                var record = await _store.AddAsync(
                    title, location, notes, CompleteSwitch.IsToggled);
                _selectedId = record.Id;
                StatusLabel.Text = "Item added.";
            }
            await ReloadAsync(_selectedId);
        }
        catch (Exception failure)
        {
            StatusLabel.Text = $"Save failed: {failure.Message}";
        }
    }

    private async void DeleteClicked(object? sender, EventArgs e)
    {
        if (_store is null || _selectedId is not int id)
        {
            return;
        }
        try
        {
            await _store.DeleteAsync(id);
            await ReloadAsync();
            NewRecord();
            StatusLabel.Text = "Item deleted.";
        }
        catch (Exception failure)
        {
            StatusLabel.Text = $"Delete failed: {failure.Message}";
        }
    }

    private async Task ReloadAsync(int? selectId = null)
    {
        if (_store is null)
        {
            return;
        }
        var records = await _store.ListAsync(SearchBar.Text);
        _records.Clear();
        foreach (var record in records)
        {
            _records.Add(record);
        }
        if (selectId is int id)
        {
            RecordsView.SelectedItem = _records.FirstOrDefault(item => item.Id == id);
        }
    }

    private void NewRecord()
    {
        _selectedId = null;
        RecordsView.SelectedItem = null;
        EditorHeading.Text = "New maintenance item";
        TitleEntry.Text = string.Empty;
        LocationEntry.Text = string.Empty;
        NotesEditor.Text = string.Empty;
        CompleteSwitch.IsToggled = false;
        DeleteButton.IsVisible = false;
        StatusLabel.Text = "Ready";
    }

    private void ShowUnlockError(string message)
    {
        UnlockErrorLabel.Text = message;
        UnlockErrorBorder.IsVisible = true;
    }
}

