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
You are a tolerant, encouraging language tutor and grammar verifier for {language}.
Analyze this sentence written by a language learner: ""{sentence}"".

PRIMARY PRINCIPLE - AVOID UNNECESSARY STYLISTIC EDITS:
- If the sentence is grammatically valid, understandable, and free of actual grammatical, spelling, agreement, or broken syntax errors, ACCEPT IT AS CORRECT!
- Even if the sentence sounds somewhat simple, textbook-like, literal, or slightly unidiomatic (not 100% how a native speaker might say it), DO NOT REWRITE IT! Do NOT paraphrase or replace words with fancy synonyms!
- The user must NEVER get trapped in an endless loop of stylistic rephrasing for a sentence that is already grammatically correct.

WHEN TO MARK AS CORRECT (isCorrection: false):
- The sentence follows the grammatical rules of {language}.
- Word order is valid (even if alternative word orders exist).
- Subject-verb, gender, and number agreements are correct.
- Words are spelled correctly.
-> In this case, return EXACTLY ONE segment containing the exact original sentence with isCorrection: false.

WHEN TO SUGGEST CORRECTIONS (isCorrection: true):
- Only when there is an OBJECTIVE ERROR:
  1. Grammar errors: wrong tense, wrong verb conjugation, wrong grammatical case/gender/number agreement, missing required preposition or article.
  2. Spelling errors: typos or misspelled words.
  3. Ungrammatical word order: word order that is grammatically incorrect or breaks the syntactic rules of {language}.
- When correcting, preserve as much of the user's original words and structure as possible. Correct ONLY the broken parts.

CRITICAL RULES FOR SPACING:
- If the sentence has NO errors, return EXACTLY ONE segment containing the full original sentence with isCorrection: false. Do NOT split correct sentences into words!
- When correcting, preserve all spaces, punctuation, and capitalization so that joining all segments' texts (string concatenation) produces the complete, grammatically correct sentence WITH ALL SPACES INTACT.
- Do NOT output bare words without spaces! Include trailing or leading spaces in the segments as needed.

Example 1 (Objective grammar error in verb):
Input: ""Io andare a casa.""
Output JSON:
{{
  ""original"": ""Io andare a casa."",
  ""segments"": [
    {{ ""text"": ""Io "", ""isCorrection"": false }},
    {{ ""text"": ""vado"", ""isCorrection"": true }},
    {{ ""text"": "" a casa."", ""isCorrection"": false }}
  ]
}}

Example 2 (Ungrammatical word order):
Input: ""Yesterday to the store went I.""
Output JSON:
{{
  ""original"": ""Yesterday to the store went I."",
  ""segments"": [
    {{ ""text"": ""Yesterday "", ""isCorrection"": false }},
    {{ ""text"": ""I went to the store."", ""isCorrection"": true }}
  ]
}}

Example 3 (Simple or literal, but grammatically valid - MUST BE ACCEPTED AS CORRECT):
Input: ""I want to drink water because I have thirst.""
Output JSON:
{{
  ""original"": ""I want to drink water because I have thirst."",
  ""segments"": [
    {{ ""text"": ""I want to drink water because I have thirst."", ""isCorrection"": false }}
  ]
}}

Example 4 (Completely correct sentence):
Input: ""Io voglio andare al mare.""
Output JSON:
{{
  ""original"": ""Io voglio andare al mare."",
  ""segments"": [
    {{ ""text"": ""Io voglio andare al mare."", ""isCorrection"": false }}
  ]
}}

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

            bool hasFlaggedCorrections = analysis.Segments.Any(s => s.IsCorrection);
            var concatenatedText = string.Concat(analysis.Segments.Select(s => s.Text ?? string.Empty)).Trim();

            // Defensive step 1: If no segment was marked as a correction
            if (!hasFlaggedCorrections)
            {
                // Verify if Mistral altered the sentence anyway (e.g. reordered words without flagging isCorrection)
                if (string.Equals(concatenatedText, sentence.Trim(), StringComparison.OrdinalIgnoreCase))
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
                else
                {
                    // Text was modified/reordered: treat as correction
                    return new SentenceAnalysis
                    {
                        Original = sentence,
                        Segments = new List<TextSegment>
                        {
                            new() { Text = concatenatedText, IsCorrection = true }
                        }
                    };
                }
            }

            // Defensive step 2: If there ARE corrections, ensure spaces between adjacent segments are preserved
            var cleanedSegments = new List<TextSegment>();
            for (int i = 0; i < analysis.Segments.Count; i++)
            {
                var current = analysis.Segments[i];
                var currentText = current.Text ?? string.Empty;
                if (string.IsNullOrEmpty(currentText)) continue;

                if (i < analysis.Segments.Count - 1)
                {
                    var next = analysis.Segments[i + 1];
                    var nextText = next.Text ?? string.Empty;

                    bool currentEndsWithSpaceOrApostrophe = currentText.EndsWith(' ') || currentText.EndsWith('\'') || currentText.EndsWith('’') || currentText.EndsWith('-');
                    bool nextStartsWithSpaceOrPunct = string.IsNullOrEmpty(nextText) || nextText.StartsWith(' ') || (char.IsPunctuation(nextText[0]) && nextText[0] != '¿' && nextText[0] != '¡');

                    if (!currentEndsWithSpaceOrApostrophe && !nextStartsWithSpaceOrPunct)
                    {
                        currentText += " ";
                    }
                }

                cleanedSegments.Add(new TextSegment
                {
                    Text = currentText,
                    IsCorrection = current.IsCorrection
                });
            }

            if (cleanedSegments.Count == 0)
            {
                return CreateFallback(sentence);
            }

            return new SentenceAnalysis
            {
                Original = sentence,
                Segments = cleanedSegments
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
