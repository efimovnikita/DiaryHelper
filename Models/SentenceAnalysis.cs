namespace DiaryHelper.Models;

public class SentenceAnalysis
{
    public string Original { get; set; } = string.Empty;
    public List<TextSegment> Segments { get; set; } = new();
    public string? Translation { get; set; }

    public bool HasCorrections => Segments.Any(s => s.IsCorrection);

    public string CorrectedFullText => string.Concat(Segments.Select(s => s.Text));
}
