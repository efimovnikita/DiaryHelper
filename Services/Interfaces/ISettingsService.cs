using DiaryHelper.Models;

namespace DiaryHelper.Services.Interfaces;

public interface ISettingsService
{
    Task<string?> GetMistralApiKeyAsync();
    Task SetMistralApiKeyAsync(string? key);

    Task<string?> GetGoogleTranslateApiKeyAsync();
    Task SetGoogleTranslateApiKeyAsync(string? key);

    string GetSourceLanguage();
    void SetSourceLanguage(string code);

    string GetTargetLanguage();
    void SetTargetLanguage(string code);

    PersonaType GetDefaultPersona();
    void SetDefaultPersona(PersonaType persona);

    bool GetShowBotPromptTranslation();
    void SetShowBotPromptTranslation(bool show);

    Task<bool> HasRequiredKeysAsync();
}
