using DiaryHelper.Models;
using DiaryHelper.Services.Interfaces;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiaryHelper.Services.Implementations;

public class MistralService : IMistralService
{
    private const string MistralApiUrl = "https://api.mistral.ai/v1/chat/completions";
    private const string ModelName = "mistral-small-latest";

    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;

    public MistralService(HttpClient httpClient, ISettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    public async Task<SentenceAnalysis> AnalyzeSentenceAsync(string sentence, string language, CancellationToken cancellationToken = default)
    {
        var apiKey = await _settingsService.GetMistralApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return CreateFallback(sentence);
        }

        var prompt = $@"
You are a language tutor and grammar corrector for {language}.
Analyze this sentence: ""{sentence}"".

Your task:
1. If the sentence is grammatically correct and uses natural vocabulary in {language}, return it as a single segment with isCorrection: false.
2. If there are errors (grammar, spelling, unnatural word choice), return the CORRECTED version of the sentence in {language}, broken into segments.
3. Mark segments that were CHANGED or CORRECTED as 'isCorrection: true'.
4. Mark segments that remain the SAME as 'isCorrection: false'.
5. IGNORE minor punctuation differences. Do not mark a segment as a correction if only punctuation changed.

Return strictly JSON with this structure:
{{
  ""original"": ""{sentence}"",
  ""segments"": [
    {{ ""text"": ""string"", ""isCorrection"": false }}
  ]
}}
";

        try
        {
            var requestBody = new
            {
                model = ModelName,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.1
            };

            var request = new HttpRequestMessage(HttpMethod.Post, MistralApiUrl)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                System.Diagnostics.Debug.WriteLine($"Mistral API Error: {response.StatusCode} - {errorContent}");
                return CreateFallback(sentence);
            }

            var chatResponse = await response.Content.ReadFromJsonAsync<MistralChatResponse>(cancellationToken: cancellationToken);
            var content = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                return CreateFallback(sentence);
            }

            var analysis = JsonSerializer.Deserialize<SentenceAnalysisDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (analysis == null || analysis.Segments == null || analysis.Segments.Count == 0)
            {
                return CreateFallback(sentence);
            }

            return new SentenceAnalysis
            {
                Original = sentence,
                Segments = analysis.Segments.Select(s => new TextSegment
                {
                    Text = s.Text ?? string.Empty,
                    IsCorrection = s.IsCorrection
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error analyzing sentence: {ex.Message}");
            return CreateFallback(sentence);
        }
    }

    public async Task<string> GenerateKickQuestionAsync(string diaryContext, string sourceLanguage, PersonaType persona, CancellationToken cancellationToken = default)
    {
        var apiKey = await _settingsService.GetMistralApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return string.Empty;
        }

        var personaDescription = persona.GetPromptDescription();

        var prompt = $@"
You are an introspective AI diary writing coach.
The user is writing their personal diary in: {sourceLanguage}.
Active persona: {personaDescription}.

Current diary entry text so far:
""""""
{diaryContext}
""""""

Task:
1. Analyze the context, theme, and language complexity of the user's written diary entry.
2. Formulate EXACTLY ONE short, targeted, open-ended question that helps the user overcome writer's block and expand their thought according to the active persona.
3. Match the language difficulty to the user's demonstrated proficiency level (Krashen's i+1 principle: natural and accessible, not overly complex).
4. Do NOT include greetings, praise, introductory phrases, conversational filler, or translations. Formulate strictly the single question in {sourceLanguage}.

Return strictly JSON with this structure:
{{
  ""question"": ""Single question in {sourceLanguage}""
}}
";

        try
        {
            var requestBody = new
            {
                model = ModelName,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.5
            };

            var request = new HttpRequestMessage(HttpMethod.Post, MistralApiUrl)
            {
                Content = JsonContent.Create(requestBody)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var chatResponse = await response.Content.ReadFromJsonAsync<MistralChatResponse>(cancellationToken: cancellationToken);
            var content = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            var questionResult = JsonSerializer.Deserialize<KickQuestionDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return questionResult?.Question?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating kick question: {ex.Message}");
            return string.Empty;
        }
    }

    private static SentenceAnalysis CreateFallback(string sentence)
    {
        return new SentenceAnalysis
        {
            Original = sentence,
            Segments = new List<TextSegment>
            {
                new() { Text = sentence, IsCorrection = false }
            }
        };
    }

    private class MistralChatResponse
    {
        [JsonPropertyName("choices")]
        public List<MistralChoice>? Choices { get; set; }
    }

    private class MistralChoice
    {
        [JsonPropertyName("message")]
        public MistralMessage? Message { get; set; }
    }

    private class MistralMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private class SentenceAnalysisDto
    {
        [JsonPropertyName("original")]
        public string? Original { get; set; }

        [JsonPropertyName("segments")]
        public List<TextSegmentDto>? Segments { get; set; }
    }

    private class TextSegmentDto
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("isCorrection")]
        public bool IsCorrection { get; set; }
    }

    private class KickQuestionDto
    {
        [JsonPropertyName("question")]
        public string? Question { get; set; }
    }
}
