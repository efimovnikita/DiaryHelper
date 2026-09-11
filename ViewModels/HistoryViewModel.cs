using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiaryHelper.Models;
using DiaryHelper.Services.Interfaces;
using DiaryHelper.Views;
using System.Collections.ObjectModel;

namespace DiaryHelper.ViewModels;

public partial class HistoryViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;

    public ObservableCollection<DiaryEntry> Entries { get; } = new();

    public HistoryViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = "История записей";
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _databaseService.GetEntriesAsync();
            Entries.Clear();
            foreach (var item in list)
            {
                Entries.Add(item);
            }
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync("Ошибка", $"Не удалось загрузить историю: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task OpenEntryAsync(DiaryEntry entry)
    {
        if (entry == null) return;
        await Shell.Current.GoToAsync($"{nameof(DiaryEntryPage)}?id={entry.Id}");
    }

    [RelayCommand]
    public async Task DeleteEntryAsync(DiaryEntry entry)
    {
        if (entry == null) return;

        var confirm = Shell.Current != null && await Shell.Current.DisplayAlertAsync("Удаление", "Удалить эту запись из истории безвозвратно?", "Удалить", "Отмена");
        if (!confirm) return;

        await _databaseService.DeleteEntryAsync(entry.Id);
        Entries.Remove(entry);
    }

    [RelayCommand]
    public async Task StartNewEntryAsync()
    {
        await Shell.Current.GoToAsync(nameof(DiaryEntryPage));
    }
}
