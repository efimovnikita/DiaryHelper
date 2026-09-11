using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiaryHelper.Models;
using DiaryHelper.Services.Interfaces;
using DiaryHelper.Views;

namespace DiaryHelper.ViewModels;

public partial class WelcomeSettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IDatabaseService _databaseService;

    public IReadOnlyList<LanguageOption> SupportedLanguages => LanguageOption.SupportedLanguages;
    public IReadOnlyList<PersonaType> AvailablePersonas { get; } = Enum.GetValues<PersonaType>();

    [ObservableProperty]
    private string _mistralApiKey = string.Empty;

    [ObservableProperty]
    private string _googleTranslateApiKey = string.Empty;

    [ObservableProperty]
    private LanguageOption? _selectedSourceLanguage;

    [ObservableProperty]
    private LanguageOption? _selectedTargetLanguage;

    [ObservableProperty]
    private PersonaType _selectedPersona = PersonaType.Friend;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public WelcomeSettingsViewModel(ISettingsService settingsService, IDatabaseService databaseService)
    {
        _settingsService = settingsService;
        _databaseService = databaseService;
        Title = "DiaryHelper";
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            await _databaseService.InitializeAsync();

            MistralApiKey = await _settingsService.GetMistralApiKeyAsync() ?? string.Empty;
            GoogleTranslateApiKey = await _settingsService.GetGoogleTranslateApiKeyAsync() ?? string.Empty;

            var srcCode = _settingsService.GetSourceLanguage();
            SelectedSourceLanguage = SupportedLanguages.FirstOrDefault(l => l.Code == srcCode) 
                                     ?? SupportedLanguages.First(l => l.Code == "en");

            var tgtCode = _settingsService.GetTargetLanguage();
            SelectedTargetLanguage = SupportedLanguages.FirstOrDefault(l => l.Code == tgtCode) 
                                     ?? SupportedLanguages.First(l => l.Code == "ru");

            SelectedPersona = _settingsService.GetDefaultPersona();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        await _settingsService.SetMistralApiKeyAsync(MistralApiKey);
        await _settingsService.SetGoogleTranslateApiKeyAsync(GoogleTranslateApiKey);

        if (SelectedSourceLanguage != null)
            _settingsService.SetSourceLanguage(SelectedSourceLanguage.Code);

        if (SelectedTargetLanguage != null)
            _settingsService.SetTargetLanguage(SelectedTargetLanguage.Code);

        _settingsService.SetDefaultPersona(SelectedPersona);

        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlertAsync("Настройки", "Настройки успешно сохранены!", "OK");
        }
    }

    [RelayCommand]
    public async Task StartNewEntryAsync()
    {
        await SaveSettingsAsync();

        if (string.IsNullOrWhiteSpace(MistralApiKey))
        {
            if (Shell.Current != null)
            {
                var proceed = await Shell.Current.DisplayAlertAsync("Внимание", "Не указан API-ключ Mistral. Без него проверка грамматики и подсказки бота будут недоступны. Продолжить?", "Да", "Отмена");
                if (!proceed) return;
            }
        }

        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(DiaryEntryPage));
        }
    }

    [RelayCommand]
    public async Task OpenHistoryAsync()
    {
        await SaveSettingsAsync();
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(HistoryPage));
        }
    }
}
