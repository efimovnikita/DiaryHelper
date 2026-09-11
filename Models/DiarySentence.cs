using SQLite;
using System.Text.Json;

namespace DiaryHelper.Models;

[Table("diary_sentences")]
public class DiarySentence
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    public string EntryId { get; set; } = string.Empty;

    public int OrderIndex { get; set; }

    public string Text { get; set; } = string.Empty;

    public string? PromptQuestion { get; set; }

    public string? PromptQuestionTranslation { get; set; }

    public string? PromptPersona { get; set; }

    public string? SegmentsJson { get; set; }

    public string? TranslationText { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Ignore]
    public bool HasPrompt => !string.IsNullOrWhiteSpace(PromptQuestion);

    [Ignore]
    public bool HasPromptTranslation => !string.IsNullOrWhiteSpace(PromptQuestionTranslation);

    [Ignore]
    public bool HasTranslation => !string.IsNullOrWhiteSpace(TranslationText);

    [Ignore]
    public List<TextSegment> Segments
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SegmentsJson))
                return new List<TextSegment> { new TextSegment { Text = Text, IsCorrection = false } };

            try
            {
                return JsonSerializer.Deserialize<List<TextSegment>>(SegmentsJson) 
                    ?? new List<TextSegment> { new TextSegment { Text = Text, IsCorrection = false } };
            }
            catch
            {
                return new List<TextSegment> { new TextSegment { Text = Text, IsCorrection = false } };
            }
        }
        set
        {
            SegmentsJson = JsonSerializer.Serialize(value);
        }
    }
}
