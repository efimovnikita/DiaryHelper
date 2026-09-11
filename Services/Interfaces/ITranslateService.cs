namespace DiaryHelper.Services.Interfaces;

public interface ITranslateService
{
    Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang, CancellationToken cancellationToken = default);
}
