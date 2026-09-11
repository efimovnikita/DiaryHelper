using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiaryHelper.Models;
using DiaryHelper.Services.Implementations;
using DiaryHelper.Services.Interfaces;
using DiaryHelper.Views;

namespace DiaryHelper.ViewModels;

public partial class WelcomeSettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IDatabaseService _databaseService;

    public IReadOnlyList<LanguageOption> SupportedLanguages => LanguageOption.SupportedLanguages;
    public IReadOnlyList<PersonaOption> AvailablePersonas { get; } = new List<PersonaOption>
    {
        new(PersonaType.Friend, AppStrings.PersonaFriend),
        new(PersonaType.Reporter, AppStrings.PersonaReporter),
        new(PersonaType.Sage, AppStrings.PersonaSage),
        new(PersonaType.Spark, AppStrings.PersonaSpark)
    };

    // Localized Strings for UI
    public string AppSubtitle => AppStrings.AppSubtitle;
    public string ApiKeysSectionTitle => AppStrings.ApiKeysSectionTitle;
    public string MistralKeyLabel => AppStrings.MistralKeyLabel;
    public string MistralKeyPlaceholder => AppStrings.MistralKeyPlaceholder;
    public string MistralKeyHint => AppStrings.MistralKeyHint;
    public string GoogleKeyLabel => AppStrings.GoogleKeyLabel;
    public string GoogleKeyPlaceholder => AppStrings.GoogleKeyPlaceholder;
    public string GoogleKeyHint => AppStrings.GoogleKeyHint;
    public string LanguageSectionTitle => AppStrings.LanguageSectionTitle;
    public string DiaryLanguageLabel => AppStrings.DiaryLanguageLabel;
    public string TranslationLanguageLabel => AppStrings.TranslationLanguageLabel;
    public string DefaultPersonaLabel => AppStrings.DefaultPersonaLabel;
    public string StartNewEntryButton => AppStrings.StartNewEntryButton;
    public string HistoryButton => AppStrings.HistoryButton;
    public string SaveSettingsButton => AppStrings.SaveSettingsButton;

    [ObservableProperty]
    private string _mistralApiKey = string.Empty;

    [ObservableProperty]
    private string _googleTranslateApiKey = string.Empty;

    [ObservableProperty]
    private LanguageOption? _selectedSourceLanguage;

    [ObservableProperty]
    private LanguageOption? _selectedTargetLanguage;

    [ObservableProperty]
    private PersonaOption? _selectedPersonaOption;

    [ObservableProperty]
    private bool _showBotPromptTranslation = true;

    public string ShowBotPromptTranslationLabel => AppStrings.ShowBotPromptTranslationLabel;
    public string ShowBotPromptTranslationHint => AppStrings.ShowBotPromptTranslationHint;

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

            var defaultSrc = SettingsService.GetSystemDefaultSourceLanguage();
            var srcCode = _settingsService.GetSourceLanguage();
            SelectedSourceLanguage = SupportedLanguages.FirstOrDefault(l => l.Code == srcCode) 
                                     ?? SupportedLanguages.First(l => l.Code == defaultSrc);

            var defaultTgt = SettingsService.GetSystemDefaultTargetLanguage();
            var tgtCode = _settingsService.GetTargetLanguage();
            SelectedTargetLanguage = SupportedLanguages.FirstOrDefault(l => l.Code == tgtCode) 
                                     ?? SupportedLanguages.First(l => l.Code == defaultTgt);

            var defaultPersona = _settingsService.GetDefaultPersona();
            SelectedPersonaOption = AvailablePersonas.FirstOrDefault(p => p.Type == defaultPersona)
                                    ?? AvailablePersonas[0];

            ShowBotPromptTranslation = _settingsService.GetShowBotPromptTranslation();
        }
        catch (Exception ex)
        {
            StatusMessage = $"{AppStrings.ErrorTitle}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveSettingsInternalAsync()
    {
        await _settingsService.SetMistralApiKeyAsync(MistralApiKey);
        await _settingsService.SetGoogleTranslateApiKeyAsync(GoogleTranslateApiKey);

        if (SelectedSourceLanguage != null)
            _settingsService.SetSourceLanguage(SelectedSourceLanguage.Code);

        if (SelectedTargetLanguage != null)
            _settingsService.SetTargetLanguage(SelectedTargetLanguage.Code);

        if (SelectedPersonaOption != null)
            _settingsService.SetDefaultPersona(SelectedPersonaOption.Type);

        _settingsService.SetShowBotPromptTranslation(ShowBotPromptTranslation);
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        await SaveSettingsInternalAsync();

        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlertAsync(AppStrings.SettingsSavedTitle, AppStrings.SettingsSavedMessage, AppStrings.Ok);
        }
    }

    [RelayCommand]
    public async Task StartNewEntryAsync()
    {
        await SaveSettingsInternalAsync();

        if (string.IsNullOrWhiteSpace(MistralApiKey))
        {
            if (Shell.Current != null)
            {
                var proceed = await Shell.Current.DisplayAlertAsync(
                    AppStrings.MissingMistralKeyAlertTitle, 
                    AppStrings.MissingMistralKeyAlertMessage, 
                    AppStrings.Yes, 
                    AppStrings.Cancel);
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
        await SaveSettingsInternalAsync();
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(HistoryPage));
        }
    }
}
