using DiaryHelper.Services.Interfaces;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DiaryHelper.Services.Implementations;

public class GoogleTranslateService : ITranslateService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;

    public GoogleTranslateService(HttpClient httpClient, ISettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var apiKey = await _settingsService.GetGoogleTranslateApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return string.Empty;
        }

        try
        {
            var url = $"https://translation.googleapis.com/language/translate/v2?key={apiKey}";
            var requestBody = new
            {
                q = text,
                source = sourceLang,
                target = targetLang,
                format = "text"
            };

            using var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var result = await response.Content.ReadFromJsonAsync<GoogleTranslateResponse>(cancellationToken: cancellationToken);
            return result?.Data?.Translations?.FirstOrDefault()?.TranslatedText ?? string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google Translate Error: {ex.Message}");
            return string.Empty;
        }
    }

    private class GoogleTranslateResponse
    {
        [JsonPropertyName("data")]
        public TranslationData? Data { get; set; }
    }

    private class TranslationData
    {
        [JsonPropertyName("translations")]
        public List<TranslationItem>? Translations { get; set; }
    }

    private class TranslationItem
    {
        [JsonPropertyName("translatedText")]
        public string TranslatedText { get; set; } = string.Empty;
    }
}
