using DiaryHelper.Models;

namespace DiaryHelper.Services.Interfaces;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<List<DiaryEntry>> GetEntriesAsync();
    Task<DiaryEntry?> GetEntryAsync(string id);
    Task SaveEntryAsync(DiaryEntry entry);
    Task DeleteEntryAsync(string id);

    Task<List<DiarySentence>> GetSentencesAsync(string entryId);
    Task SaveSentencesAsync(string entryId, IEnumerable<DiarySentence> sentences);
    Task DeleteSentenceAsync(string sentenceId);
}
