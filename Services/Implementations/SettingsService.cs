using DiaryHelper.Models;
using DiaryHelper.Services.Interfaces;

namespace DiaryHelper.Services.Implementations;

public class SettingsService : ISettingsService
{
    private const string MistralKeySetting = "mistral_api_key";
    private const string GoogleTranslateKeySetting = "google_translate_api_key";
    private const string SourceLanguageSetting = "source_language";
    private const string TargetLanguageSetting = "target_language";
    private const string DefaultPersonaSetting = "default_persona";

    public async Task<string?> GetMistralApiKeyAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(MistralKeySetting);
        }
        catch
        {
            return Preferences.Default.Get(MistralKeySetting, string.Empty);
        }
    }

    public async Task SetMistralApiKeyAsync(string? key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                SecureStorage.Default.Remove(MistralKeySetting);
            }
            else
            {
                await SecureStorage.Default.SetAsync(MistralKeySetting, key.Trim());
            }
        }
        catch
        {
            Preferences.Default.Set(MistralKeySetting, key?.Trim() ?? string.Empty);
        }
    }

    public async Task<string?> GetGoogleTranslateApiKeyAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(GoogleTranslateKeySetting);
        }
        catch
        {
            return Preferences.Default.Get(GoogleTranslateKeySetting, string.Empty);
        }
    }

    public async Task SetGoogleTranslateApiKeyAsync(string? key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                SecureStorage.Default.Remove(GoogleTranslateKeySetting);
            }
            else
            {
                await SecureStorage.Default.SetAsync(GoogleTranslateKeySetting, key.Trim());
            }
        }
        catch
        {
            Preferences.Default.Set(GoogleTranslateKeySetting, key?.Trim() ?? string.Empty);
        }
    }

    public static string GetSystemDefaultTargetLanguage()
    {
        var sys = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        var supported = LanguageOption.SupportedLanguages.Select(l => l.Code).ToHashSet();
        return supported.Contains(sys) ? sys : "en";
    }

    public static string GetSystemDefaultSourceLanguage()
    {
        var target = GetSystemDefaultTargetLanguage();
        return target == "en" ? "it" : "en";
    }

    public string GetSourceLanguage()
    {
        return Preferences.Default.Get(SourceLanguageSetting, GetSystemDefaultSourceLanguage());
    }

    public void SetSourceLanguage(string code)
    {
        Preferences.Default.Set(SourceLanguageSetting, code);
    }

    public string GetTargetLanguage()
    {
        return Preferences.Default.Get(TargetLanguageSetting, GetSystemDefaultTargetLanguage());
    }

    public void SetTargetLanguage(string code)
    {
        Preferences.Default.Set(TargetLanguageSetting, code);
    }

    public PersonaType GetDefaultPersona()
    {
        var val = Preferences.Default.Get(DefaultPersonaSetting, nameof(PersonaType.Friend));
        return Enum.TryParse<PersonaType>(val, out var res) ? res : PersonaType.Friend;
    }

    public void SetDefaultPersona(PersonaType persona)
    {
        Preferences.Default.Set(DefaultPersonaSetting, persona.ToString());
    }

    public async Task<bool> HasRequiredKeysAsync()
    {
        var mistralKey = await GetMistralApiKeyAsync();
        return !string.IsNullOrWhiteSpace(mistralKey);
    }
}
