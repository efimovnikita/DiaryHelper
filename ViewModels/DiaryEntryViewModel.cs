using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiaryHelper.Models;
using DiaryHelper.Services.Interfaces;
using System.Collections.ObjectModel;

namespace DiaryHelper.ViewModels;

[QueryProperty(nameof(EntryId), "id")]
public partial class DiaryEntryViewModel : BaseViewModel
{
    private readonly IDatabaseService _databaseService;
    private readonly ISettingsService _settingsService;
    private readonly IMistralService _mistralService;
    private readonly ITranslateService _translateService;

    private DiaryEntry _entry = new();
    private DiarySentence? _editingSentence;

    [ObservableProperty]
    private string _entryId = string.Empty;

    [ObservableProperty]
    private string _currentInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPendingAnalysis))]
    [NotifyPropertyChangedFor(nameof(IsAnalysisCorrect))]
    [NotifyPropertyChangedFor(nameof(HasAnalysisCorrections))]
    [NotifyPropertyChangedFor(nameof(AnalysisHeader))]
    [NotifyPropertyChangedFor(nameof(AnalysisCardBackground))]
    [NotifyPropertyChangedFor(nameof(AnalysisCardStroke))]
    [NotifyPropertyChangedFor(nameof(AnalysisHeaderColor))]
    [NotifyPropertyChangedFor(nameof(AnalysisTranslationColor))]
    private SentenceAnalysis? _pendingAnalysis;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _isGeneratingPrompt;

    [ObservableProperty]
    private string? _currentPromptQuestion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPromptTranslationAndEnabled))]
    private string? _currentPromptQuestionTranslation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPromptTranslationAndEnabled))]
    private bool _showPromptTranslation = true;

    [ObservableProperty]
    private PersonaType _activePersona = PersonaType.Friend;

    [ObservableProperty]
    private bool _isEditingExisting;

    public IReadOnlyList<PersonaType> AvailablePersonas { get; } = Enum.GetValues<PersonaType>();

    public ObservableCollection<DiarySentence> Sentences { get; } = new();

    public bool HasPendingAnalysis => PendingAnalysis != null;
    public bool IsAnalysisCorrect => PendingAnalysis != null && !PendingAnalysis.HasCorrections;
    public bool HasAnalysisCorrections => PendingAnalysis != null && PendingAnalysis.HasCorrections;
    public bool HasActivePrompt => !string.IsNullOrWhiteSpace(CurrentPromptQuestion);
    public bool HasPromptTranslationAndEnabled => ShowPromptTranslation && !string.IsNullOrWhiteSpace(CurrentPromptQuestionTranslation);

    public string CorrectBadgeText => AppStrings.CorrectBadgeText;
    public string FixesBadgeText => AppStrings.FixesBadgeText;

    public string AnalysisHeader => (PendingAnalysis != null && !PendingAnalysis.HasCorrections)
        ? AppStrings.AnalysisCorrectHeader
        : (PendingAnalysis != null && PendingAnalysis.HasCorrections)
            ? AppStrings.AnalysisNeedsFixHeader
            : AppStrings.AnalysisHeader;

    public Color AnalysisCardBackground => (PendingAnalysis != null && !PendingAnalysis.HasCorrections)
        ? Color.FromArgb("#F0FDF4") // Clean light emerald green
        : Color.FromArgb("#FFF7ED"); // Warm amber/orange

    public Brush AnalysisCardStroke => (PendingAnalysis != null && !PendingAnalysis.HasCorrections)
        ? new SolidColorBrush(Color.FromArgb("#BBF7D0")) // Light emerald border
        : new SolidColorBrush(Color.FromArgb("#FED7AA")); // Soft warm orange border

    public Color AnalysisHeaderColor => (PendingAnalysis != null && !PendingAnalysis.HasCorrections)
        ? Color.FromArgb("#15803D") // Vibrant green text
        : Color.FromArgb("#C2410C"); // Vibrant orange text

    public Color AnalysisTranslationColor => (PendingAnalysis != null && !PendingAnalysis.HasCorrections)
        ? Color.FromArgb("#166534") // Dark forest green text
        : Color.FromArgb("#78350F"); // Warm deep brown text

    // Localized UI strings
    public string EmptySentencesHint => AppStrings.EmptySentencesHint;
    public string KickQuestionHeader => AppStrings.KickQuestionHeader;
    public string ApplyFixButton => AppStrings.ApplyFixButton;
    public string BackButtonText => AppStrings.BackButtonText;
    public string InputPlaceholder => AppStrings.InputPlaceholder;
    public string SaveButton => AppStrings.SaveButton;
    public string CheckButton => AppStrings.CheckButton;
    public string AddButton => AppStrings.AddButton;
    public string CopyPureButton => AppStrings.CopyPureButton;
    public string CopyGuidedButton => AppStrings.CopyGuidedButton;
    public string PersonaFriendText => AppStrings.PersonaFriend;
    public string PersonaReporterText => AppStrings.PersonaReporter;
    public string PersonaSageText => AppStrings.PersonaSage;
    public string PersonaSparkText => AppStrings.PersonaSpark;

    public DiaryEntryViewModel(
        IDatabaseService databaseService,
        ISettingsService settingsService,
        IMistralService mistralService,
        ITranslateService translateService)
    {
        _databaseService = databaseService;
        _settingsService = settingsService;
        _mistralService = mistralService;
        _translateService = translateService;
        Title = AppStrings.NewEntryTitle;
    }

    public async Task InitializeAsync()
    {
        ActivePersona = _settingsService.GetDefaultPersona();
        ShowPromptTranslation = _settingsService.GetShowBotPromptTranslation();

        if (!string.IsNullOrWhiteSpace(EntryId))
        {
            var loaded = await _databaseService.GetEntryAsync(EntryId);
            if (loaded != null)
            {
                _entry = loaded;
                Title = !string.IsNullOrWhiteSpace(_entry.Title)
                    ? _entry.Title
                    : $"{AppStrings.EntryFromDatePrefix} {_entry.CreatedAt.ToLocalTime():dd.MM.yyyy HH:mm}";

                var sentences = await _databaseService.GetSentencesAsync(EntryId);
                Sentences.Clear();
                foreach (var s in sentences)
                {
                    Sentences.Add(s);
                }
                return;
            }
        }

        // New entry initialization
        _entry = new DiaryEntry
        {
            SourceLanguage = _settingsService.GetSourceLanguage(),
            TargetLanguage = _settingsService.GetTargetLanguage(),
            DefaultPersona = ActivePersona.ToString()
        };
        Sentences.Clear();
    }

    [RelayCommand]
    public async Task SelectPersonaAsync(string personaName)
    {
        if (Enum.TryParse<PersonaType>(personaName, out var persona))
        {
            ActivePersona = persona;
            await RequestKickQuestionAsync();
        }
    }

    [RelayCommand]
    public async Task CheckSentenceAsync()
    {
        var text = CurrentInput.Trim();
        if (string.IsNullOrWhiteSpace(text) || IsChecking) return;

        IsChecking = true;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var analysisTask = _mistralService.AnalyzeSentenceAsync(text, _entry.SourceLanguage, cts.Token);
            var translateTask = _translateService.TranslateTextAsync(text, _entry.SourceLanguage, _entry.TargetLanguage, cts.Token);

            await Task.WhenAll(analysisTask, translateTask);

            var analysis = await analysisTask;
            var translation = await translateTask;

            analysis.Translation = translation;
            PendingAnalysis = analysis;
            OnPropertyChanged(nameof(HasPendingAnalysis));
        }
        catch (OperationCanceledException)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync(AppStrings.TimeoutTitle, AppStrings.TimeoutMessage, AppStrings.Ok);
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync(AppStrings.ErrorTitle, $"{AppStrings.ErrorTitle}: {ex.Message}", AppStrings.Ok);
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    public void ApplyFix()
    {
        if (PendingAnalysis != null)
        {
            CurrentInput = PendingAnalysis.CorrectedFullText;
            PendingAnalysis = null;
            OnPropertyChanged(nameof(HasPendingAnalysis));
        }
    }

    [RelayCommand]
    public void DismissAnalysis()
    {
        PendingAnalysis = null;
        OnPropertyChanged(nameof(HasPendingAnalysis));
    }

    [RelayCommand]
    public void ConfirmSentence()
    {
        var text = CurrentInput.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        if (_editingSentence != null)
        {
            _editingSentence.Text = text;
            _editingSentence.Segments = PendingAnalysis?.Original == text 
                ? PendingAnalysis.Segments 
                : new List<TextSegment> { new() { Text = text, IsCorrection = false } };
            _editingSentence.TranslationText = PendingAnalysis?.Original == text ? PendingAnalysis.Translation : null;

            var index = Sentences.IndexOf(_editingSentence);
            if (index >= 0)
            {
                Sentences[index] = _editingSentence;
            }

            _editingSentence = null;
            IsEditingExisting = false;
        }
        else
        {
            var sentence = new DiarySentence
            {
                EntryId = _entry.Id,
                OrderIndex = Sentences.Count,
                Text = text,
                PromptQuestion = CurrentPromptQuestion,
                PromptQuestionTranslation = CurrentPromptQuestionTranslation,
                PromptPersona = ActivePersona.ToString(),
                Segments = (PendingAnalysis != null && PendingAnalysis.Original == text) 
                    ? PendingAnalysis.Segments 
                    : new List<TextSegment> { new() { Text = text, IsCorrection = false } },
                TranslationText = (PendingAnalysis != null && PendingAnalysis.Original == text) 
                    ? PendingAnalysis.Translation 
                    : null
            };

            Sentences.Add(sentence);

            // After the first couple of sentences, generate a short meaningful title for the entry
            if (Sentences.Count >= 2 && (string.IsNullOrWhiteSpace(_entry.Title) || Title == AppStrings.NewEntryTitle))
            {
                _ = TryGenerateTitleAsync();
            }
        }

        CurrentInput = string.Empty;
        PendingAnalysis = null;
        CurrentPromptQuestion = null;
        CurrentPromptQuestionTranslation = null;

        OnPropertyChanged(nameof(HasPendingAnalysis));
        OnPropertyChanged(nameof(HasActivePrompt));
    }

    private async Task TryGenerateTitleAsync()
    {
        try
        {
            var context = string.Join(" ", Sentences.Take(3).Select(s => s.Text));
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var generated = await _mistralService.GenerateEntryTitleAsync(context, _entry.SourceLanguage, cts.Token);
            if (!string.IsNullOrWhiteSpace(generated))
            {
                _entry.Title = generated;
                Title = generated;
                await _databaseService.SaveEntryAsync(_entry);
            }
        }
        catch
        {
            // best-effort background generation
        }
    }

    [RelayCommand]
    public async Task RequestKickQuestionAsync()
    {
        if (IsGeneratingPrompt) return;

        IsGeneratingPrompt = true;
        try
        {
            var contextText = string.Join(" ", Sentences.Select(s => s.Text));
            if (!string.IsNullOrWhiteSpace(CurrentInput))
            {
                contextText += " " + CurrentInput.Trim();
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));

            var question = await _mistralService.GenerateKickQuestionAsync(
                diaryContext: string.IsNullOrWhiteSpace(contextText) ? "I am starting to write my diary entry today." : contextText,
                sourceLanguage: _entry.SourceLanguage,
                persona: ActivePersona,
                cancellationToken: cts.Token
            );

            if (!string.IsNullOrWhiteSpace(question))
            {
                CurrentPromptQuestion = question;

                if (ShowPromptTranslation)
                {
                    var translation = await _translateService.TranslateTextAsync(
                        question, 
                        _entry.SourceLanguage, 
                        _entry.TargetLanguage, 
                        cts.Token
                    );

                    CurrentPromptQuestionTranslation = translation;
                }
                else
                {
                    CurrentPromptQuestionTranslation = null;
                }

                OnPropertyChanged(nameof(HasActivePrompt));
                OnPropertyChanged(nameof(HasPromptTranslationAndEnabled));
            }
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync(AppStrings.BotPromptTitle, $"{AppStrings.ErrorTitle}: {ex.Message}", AppStrings.Ok);
        }
        finally
        {
            IsGeneratingPrompt = false;
        }
    }

    [RelayCommand]
    public async Task TogglePromptTranslationAsync()
    {
        ShowPromptTranslation = !ShowPromptTranslation;

        if (ShowPromptTranslation && string.IsNullOrWhiteSpace(CurrentPromptQuestionTranslation) && !string.IsNullOrWhiteSpace(CurrentPromptQuestion))
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                CurrentPromptQuestionTranslation = await _translateService.TranslateTextAsync(
                    CurrentPromptQuestion, 
                    _entry.SourceLanguage, 
                    _entry.TargetLanguage, 
                    cts.Token
                );
            }
            catch
            {
                // ignore
            }
        }

        OnPropertyChanged(nameof(HasPromptTranslationAndEnabled));
    }

    [RelayCommand]
    public void DismissPrompt()
    {
        CurrentPromptQuestion = null;
        CurrentPromptQuestionTranslation = null;
        OnPropertyChanged(nameof(HasActivePrompt));
    }

    [RelayCommand]
    public void MoveUp(DiarySentence sentence)
    {
        var idx = Sentences.IndexOf(sentence);
        if (idx > 0)
        {
            Sentences.Move(idx, idx - 1);
        }
    }

    [RelayCommand]
    public void MoveDown(DiarySentence sentence)
    {
        var idx = Sentences.IndexOf(sentence);
        if (idx < Sentences.Count - 1)
        {
            Sentences.Move(idx, idx + 1);
        }
    }

    [RelayCommand]
    public void EditSentence(DiarySentence sentence)
    {
        _editingSentence = sentence;
        IsEditingExisting = true;
        CurrentInput = sentence.Text;
        PendingAnalysis = null;
        OnPropertyChanged(nameof(HasPendingAnalysis));
    }

    [RelayCommand]
    public void DeleteSentence(DiarySentence sentence)
    {
        if (_editingSentence == sentence)
        {
            _editingSentence = null;
            IsEditingExisting = false;
            CurrentInput = string.Empty;
        }
        Sentences.Remove(sentence);
    }

    [RelayCommand]
    public async Task SaveEntryAsync()
    {
        if (Sentences.Count == 0 && string.IsNullOrWhiteSpace(CurrentInput))
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlertAsync(AppStrings.WarningTitle, AppStrings.EmptyEntryAlert, AppStrings.Ok);
            return;
        }

        // Add unfinished input if present
        if (!string.IsNullOrWhiteSpace(CurrentInput))
        {
            ConfirmSentence();
        }

        _entry.SentenceCount = Sentences.Count;
        _entry.PreviewText = Sentences.FirstOrDefault()?.Text ?? string.Empty;
        _entry.UpdatedAt = DateTime.UtcNow;

        await _databaseService.SaveEntryAsync(_entry);
        await _databaseService.SaveSentencesAsync(_entry.Id, Sentences);

        if (Shell.Current != null)
        {
            await Shell.Current.DisplayAlertAsync(AppStrings.SavedSuccessTitle, AppStrings.SavedSuccessMessage, AppStrings.Ok);
            await GoBackAsync();
        }
    }

    public async Task AutoSaveOnExitAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentInput) && Sentences.Count == 0)
            return;

        // Auto-commit any unfinished text typed into the editor
        if (!string.IsNullOrWhiteSpace(CurrentInput))
        {
            var text = CurrentInput.Trim();
            var sentence = new DiarySentence
            {
                EntryId = _entry.Id,
                OrderIndex = Sentences.Count,
                Text = text,
                PromptQuestion = CurrentPromptQuestion,
                PromptQuestionTranslation = CurrentPromptQuestionTranslation,
                PromptPersona = ActivePersona.ToString(),
                Segments = (PendingAnalysis != null && PendingAnalysis.Original == text) 
                    ? PendingAnalysis.Segments 
                    : new List<TextSegment> { new() { Text = text, IsCorrection = false } },
                TranslationText = (PendingAnalysis != null && PendingAnalysis.Original == text) 
                    ? PendingAnalysis.Translation 
                    : null
            };
            Sentences.Add(sentence);
            CurrentInput = string.Empty;
            PendingAnalysis = null;
        }

        if (Sentences.Count > 0)
        {
            _entry.SentenceCount = Sentences.Count;
            _entry.PreviewText = Sentences.FirstOrDefault()?.Text ?? string.Empty;
            _entry.UpdatedAt = DateTime.UtcNow;

            await _databaseService.SaveEntryAsync(_entry);
            await _databaseService.SaveSentencesAsync(_entry.Id, Sentences);
        }
    }

    [RelayCommand]
    public async Task GoBackAsync()
    {
        await AutoSaveOnExitAsync();
        try
        {
            if (Shell.Current != null && Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync();
            }
            else if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        catch
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("//WelcomeSettingsPage");
            }
        }
    }

    [RelayCommand]
    public async Task CopyPureTextAsync()
    {
        var text = _entry.ToPureText(Sentences);
        if (string.IsNullOrWhiteSpace(text)) return;

        await Clipboard.Default.SetTextAsync(text);
        if (Shell.Current != null)
            await Shell.Current.DisplayAlertAsync(AppStrings.ClipboardTitle, AppStrings.CopiedPureMessage, AppStrings.Ok);
    }

    [RelayCommand]
    public async Task CopyGuidedDialogueAsync()
    {
        var text = _entry.ToGuidedDialogueText(Sentences);
        if (string.IsNullOrWhiteSpace(text)) return;

        await Clipboard.Default.SetTextAsync(text);
        if (Shell.Current != null)
            await Shell.Current.DisplayAlertAsync(AppStrings.ClipboardTitle, AppStrings.CopiedGuidedMessage, AppStrings.Ok);
    }
}
