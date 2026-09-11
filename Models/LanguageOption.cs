namespace DiaryHelper.Models;

public record LanguageOption(string Code, string Name, string Flag)
{
    public string DisplayName => $"{Flag} {Name}";

    public static readonly IReadOnlyList<LanguageOption> SupportedLanguages = new List<LanguageOption>
    {
        new("en", "English", "🇬🇧"),
        new("ru", "Русский", "🇷🇺"),
        new("it", "Italiano", "🇮🇹"),
        new("es", "Español", "🇪🇸"),
        new("fr", "Français", "🇫🇷"),
        new("de", "Deutsch", "🇩🇪"),
        new("pt", "Português", "🇵🇹"),
        new("zh", "中文 (Chinese)", "🇨🇳"),
        new("ja", "日本語 (Japanese)", "🇯🇵"),
        new("ko", "한국어 (Korean)", "🇰🇷"),
        new("ar", "العربية (Arabic)", "🇸🇦"),
        new("nl", "Nederlands", "🇳🇱"),
        new("pl", "Polski", "🇵🇱"),
        new("tr", "Türkçe", "🇹🇷")
    };
}
