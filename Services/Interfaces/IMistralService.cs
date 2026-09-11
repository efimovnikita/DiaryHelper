using DiaryHelper.Models;

namespace DiaryHelper.Services.Interfaces;

public interface IMistralService
{
    Task<SentenceAnalysis> AnalyzeSentenceAsync(string sentence, string language, CancellationToken cancellationToken = default);

    Task<string> GenerateKickQuestionAsync(string diaryContext, string sourceLanguage, PersonaType persona, CancellationToken cancellationToken = default);
    Task<string> GenerateEntryTitleAsync(string diaryContext, string language, CancellationToken cancellationToken = default);
}
