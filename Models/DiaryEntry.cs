using SQLite;
using System.Text;

namespace DiaryHelper.Models;

[Table("diary_entries")]
public class DiaryEntry
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string SourceLanguage { get; set; } = "en";

    public string TargetLanguage { get; set; } = "ru";

    public string DefaultPersona { get; set; } = "Friend";

    public int SentenceCount { get; set; }

    public string PreviewText { get; set; } = string.Empty;

    public string ToPureText(IEnumerable<DiarySentence> sentences)
    {
        var ordered = sentences.OrderBy(s => s.OrderIndex).Select(s => s.Text.Trim());
        return string.Join(" ", ordered.Where(t => !string.IsNullOrEmpty(t)));
    }

    public string ToGuidedDialogueText(IEnumerable<DiarySentence> sentences)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Дневник от {CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Язык записи: {SourceLanguage.ToUpperInvariant()} | Перевод: {TargetLanguage.ToUpperInvariant()}");
        sb.AppendLine();

        foreach (var s in sentences.OrderBy(s => s.OrderIndex))
        {
            if (!string.IsNullOrWhiteSpace(s.PromptQuestion))
            {
                sb.AppendLine($"🤖 [Вопрос AI]: {s.PromptQuestion}");
                if (!string.IsNullOrWhiteSpace(s.PromptQuestionTranslation))
                {
                    sb.AppendLine($"   ({s.PromptQuestionTranslation})");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"✍️ {s.Text}");
            if (!string.IsNullOrWhiteSpace(s.TranslationText))
            {
                sb.AppendLine($"   ({s.TranslationText})");
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
