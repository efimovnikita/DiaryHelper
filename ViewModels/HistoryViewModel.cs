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

    // Localized UI strings
    public string ArchiveHeader => AppStrings.ArchiveHeader;
    public string ArchiveSubtitle => AppStrings.ArchiveSubtitle;
    public string NewButtonShort => AppStrings.NewButtonShort;
    public string EmptyArchiveHint => AppStrings.EmptyArchiveHint;
    public string OpenButton => AppStrings.OpenButton;
    public string DeleteButton => AppStrings.DeleteButton;
    public string SentencesCountSuffix => AppStrings.SentencesCountSuffix;

    public HistoryViewModel(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        Title = AppStrings.HistoryTitle;
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
                await Shell.Current.DisplayAlertAsync(AppStrings.ErrorTitle, $"{AppStrings.ErrorTitle}: {ex.Message}", AppStrings.Ok);
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

        var confirm = Shell.Current != null && await Shell.Current.DisplayAlertAsync(
            AppStrings.DeleteConfirmTitle, 
            AppStrings.DeleteConfirmMessage, 
            AppStrings.DeleteButton, 
            AppStrings.Cancel);
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
