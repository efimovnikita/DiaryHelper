using System.Globalization;

namespace DiaryHelper.Models;

public static class AppStrings
{
    public static bool IsRussian => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ru", StringComparison.OrdinalIgnoreCase);

    // Common
    public static string Ok => "OK";
    public static string Yes => IsRussian ? "Да" : "Yes";
    public static string Cancel => IsRussian ? "Отмена" : "Cancel";
    public static string ErrorTitle => IsRussian ? "Ошибка" : "Error";
    public static string WarningTitle => IsRussian ? "Внимание" : "Warning";

    // Welcome Screen
    public static string AppSubtitle => IsRussian 
        ? "Персональный дневник для языковой практики с AI-собеседником" 
        : "Personal language practice diary with an AI companion";

    public static string ApiKeysSectionTitle => IsRussian ? "🔑 Облачные сервисы (API Keys)" : "🔑 Cloud Services (API Keys)";
    public static string MistralKeyLabel => "Mistral AI API Key";
    public static string MistralKeyPlaceholder => IsRussian ? "Вставьте ключ Mistral API..." : "Paste Mistral API key...";
    public static string MistralKeyHint => IsRussian ? "Используется для проверки грамматики и наводящих вопросов" : "Used for grammar check and kick questions";
    
    public static string GoogleKeyLabel => "Google Cloud Translation API Key";
    public static string GoogleKeyPlaceholder => IsRussian ? "Вставьте ключ Google Translate API..." : "Paste Google Translate API key...";
    public static string GoogleKeyHint => IsRussian ? "Используется для перевода предложений и вопросов бота" : "Used for sentence and question translations";
    
    public static string LanguageSectionTitle => IsRussian ? "🌍 Языковые настройки" : "🌍 Language Settings";
    public static string DiaryLanguageLabel => IsRussian ? "Язык дневника" : "Diary Language";
    public static string TranslationLanguageLabel => IsRussian ? "Язык перевода" : "Translation Language";
    public static string DefaultPersonaLabel => IsRussian ? "Архетип AI-собеседника по умолчанию" : "Default AI Companion Persona";
    public static string StartNewEntryButton => IsRussian ? "✍️ Начать новую запись" : "✍️ Start New Entry";
    public static string HistoryButton => IsRussian ? "📖 История записей" : "📖 Diary History";
    public static string SaveSettingsButton => IsRussian ? "Сохранить настройки" : "Save Settings";
    public static string SettingsSavedTitle => IsRussian ? "Настройки" : "Settings";
    public static string SettingsSavedMessage => IsRussian ? "Настройки успешно сохранены!" : "Settings saved successfully!";
    public static string MissingMistralKeyAlertTitle => IsRussian ? "Внимание" : "Warning";
    public static string MissingMistralKeyAlertMessage => IsRussian 
        ? "Не указан API-ключ Mistral. Без него проверка грамматики и подсказки бота будут недоступны. Продолжить?" 
        : "Mistral API key is not set. Grammar check and bot suggestions will be disabled. Continue?";

    // Diary Entry Screen
    public static string NewEntryTitle => IsRussian ? "Новая запись" : "New Entry";
    public static string EntryFromDatePrefix => IsRussian ? "Запись от" : "Entry from";
    public static string EmptySentencesHint => IsRussian ? "Пока нет добавленных предложений. Напишите первое предложение ниже." : "No sentences yet. Write your first sentence below.";
    public static string KickQuestionHeader => IsRussian ? "💡 Вопрос AI для вдохновения:" : "💡 AI Kick-Question:";
    public static string AnalysisHeader => IsRussian ? "🔎 Анализ предложения" : "🔎 Sentence Analysis";
    public static string AnalysisCorrectHeader => IsRussian ? "✅ Предложение составлено верно!" : "✅ Sentence is correct!";
    public static string AnalysisNeedsFixHeader => IsRussian ? "✏️ Предложены исправления:" : "✏️ Suggested corrections:";
    public static string BackButtonText => IsRussian ? "Назад" : "Back";
    public static string ApplyFixButton => IsRussian ? "🪄 Применить" : "🪄 Apply Fix";
    public static string InputPlaceholder => IsRussian ? "Напишите следующее предложение..." : "Write your next sentence...";
    public static string SaveButton => IsRussian ? "💾 Сохранить" : "💾 Save";
    public static string CheckButton => IsRussian ? "🔎 Проверить" : "🔎 Check";
    public static string AddButton => IsRussian ? "➕ Добавить" : "➕ Add";
    public static string CopyPureButton => IsRussian ? "📋 Текст дневника" : "📋 Diary Text";
    public static string CopyGuidedButton => IsRussian ? "📋 Текст + Вопросы AI" : "📋 Diary + AI Questions";
    public static string EmptyEntryAlert => IsRussian ? "Запись пуста. Напишите хотя бы одно предложение." : "Entry is empty. Please write at least one sentence.";
    public static string SavedSuccessTitle => IsRussian ? "Сохранено" : "Saved";
    public static string SavedSuccessMessage => IsRussian ? "Запись дневника успешно сохранена в локальной базе данных!" : "Diary entry saved successfully!";
    public static string CopiedPureMessage => IsRussian ? "Текст дневника скопирован без вопросов бота!" : "Diary text copied to clipboard!";
    public static string CopiedGuidedMessage => IsRussian ? "Текст дневника вместе с вопросами AI скопирован!" : "Diary text with AI questions copied!";
    public static string ClipboardTitle => IsRussian ? "Буфер обмена" : "Clipboard";
    public static string TimeoutTitle => IsRussian ? "Таймаут" : "Timeout";
    public static string TimeoutMessage => IsRussian ? "Проверка заняла больше 10 секунд. Пожалуйста, попробуйте снова." : "Checking took longer than 10 seconds. Please try again.";
    public static string BotPromptTitle => IsRussian ? "Подсказка AI" : "AI Prompt";

    // Personas
    public static string PersonaFriend => IsRussian ? "🧘 Друг" : "🧘 Friend";
    public static string PersonaReporter => IsRussian ? "🕵️ Хроникёр" : "🕵️ Reporter";
    public static string PersonaSage => IsRussian ? "🦉 Философ" : "🦉 Sage";
    public static string PersonaSpark => IsRussian ? "⚡ Провокатор" : "⚡ Spark";

    // History Screen
    public static string HistoryTitle => IsRussian ? "История записей" : "Diary History";
    public static string ArchiveHeader => IsRussian ? "Архив записей дневника" : "Diary Entries Archive";
    public static string ArchiveSubtitle => IsRussian ? "Просматривайте и продолжайте свои записи" : "Review and continue your entries";
    public static string NewButtonShort => IsRussian ? "✍️ Новая" : "✍️ New";
    public static string EmptyArchiveHint => IsRussian ? "Архив пуст. Создайте свою первую запись!" : "Archive is empty. Write your first entry!";
    public static string OpenButton => IsRussian ? "Открыть" : "Open";
    public static string DeleteButton => IsRussian ? "Удалить" : "Delete";
    public static string DeleteConfirmTitle => IsRussian ? "Удаление" : "Delete Entry";
    public static string DeleteConfirmMessage => IsRussian ? "Удалить эту запись из истории безвозвратно?" : "Delete this entry permanently?";
    public static string SentencesCountSuffix => IsRussian ? "предл." : "sent.";
    public static string LanguagePrefix => IsRussian ? "Язык: {0}" : "Lang: {0}";
}
