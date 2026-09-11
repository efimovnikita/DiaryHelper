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

        var langName = LanguageOption.GetLanguageName(language);

        var prompt = $@"
You are an encouraging, expert language tutor and grammar verifier for the {langName} language.
Analyze this diary sentence written by a language learner: ""{sentence}"".

TARGET LANGUAGE: {langName} (language code: {language})
The entire sentence MUST be in {langName}.

CRITICAL PRINCIPLE 1: MANDATORY CORRECTION OF ALL OBJECTIVE ERRORS
- You MUST correct all grammatical, agreement, lexical, and spelling defects:
  1. Grammatical & agreement errors: wrong grammatical gender or article (e.g. Italian 'una libro' MUST be corrected to 'un libro', Spanish 'la problema' -> 'el problema'), wrong verb conjugation/form (e.g. 'Io andare' -> 'Io vado', 'She go' -> 'She goes'), wrong auxiliary verb (e.g. Italian 'ho andato' -> 'sono andato'), wrong plural/singular agreement (e.g. 'tre gatto' -> 'tre gatti'), missing required preposition or article.
  2. Foreign words / code-switching: When learners do not know a word in {langName}, they insert words in another language (e.g. English, Russian, Spanish). YOU MUST REPLACE all foreign words/phrases with their natural equivalent in {langName} and mark with isCorrection: true.
  3. Spelling errors & typos: misspelled words in {langName}.
- When correcting, preserve as much of the user's original words and sentence structure as possible. Correct ONLY the broken word(s)!

CRITICAL PRINCIPLE 2: FORBIDDEN TO SUGGEST TRIVIAL, IDENTICAL, OR PURELY STYLISTIC EDITS
- If the sentence has NO objective grammar, agreement, spelling, or foreign-word errors, ACCEPT IT AS CORRECT!
- DO NOT rewrite sentences just to sound 'more natural', 'more poetic', or 'more native' if the original is already grammatically valid.
- NEVER replace a word with the EXACT SAME word or an equivalent synonym and mark it as a correction!
- DO NOT flag missing trailing punctuation (e.g. adding a period at the end) as an error! If the words and grammar are valid, accept the sentence as correct!
- If the sentence is correct, return EXACTLY ONE segment containing the full original sentence with isCorrection: false. Do NOT split a correct sentence into multiple segments!

SPACING AND SEGMENT RULES:
- When correcting, preserve all spaces, punctuation, and capitalization so that string concatenation of all segments produces the complete, grammatically correct sentence WITH ALL SPACES INTACT.
- Do NOT output bare words without spaces! Include trailing or leading spaces in the segments as needed.

Example 1 (Foreign word in English inserted into Italian):
Target language: Italian
Input: ""Oggi ho mangiato una delicious mela.""
Output JSON:
{{
  ""original"": ""Oggi ho mangiato una delicious mela."",
  ""segments"": [
    {{ ""text"": ""Oggi ho mangiato una "", ""isCorrection"": false }},
    {{ ""text"": ""deliziosa"", ""isCorrection"": true }},
    {{ ""text"": "" mela."", ""isCorrection"": false }}
  ]
}}

Example 2 (Foreign word in Russian inserted into Italian):
Target language: Italian
Input: ""Io voglio comprare una машина.""
Output JSON:
{{
  ""original"": ""Io voglio comprare una машина."",
  ""segments"": [
    {{ ""text"": ""Io voglio comprare una "", ""isCorrection"": false }},
    {{ ""text"": ""macchina."", ""isCorrection"": true }}
  ]
}}

Example 3 (Foreign word inserted into English):
Target language: English
Input: ""Yesterday I went to the магазин.""
Output JSON:
{{
  ""original"": ""Yesterday I went to the магазин."",
  ""segments"": [
    {{ ""text"": ""Yesterday I went to the "", ""isCorrection"": false }},
    {{ ""text"": ""store."", ""isCorrection"": true }}
  ]
}}

Example 4 (Objective grammar error in verb):
Target language: Italian
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

Example 5 (Objective grammar error in article / gender agreement):
Target language: Italian
Input: ""Ho comprato una libro nuovo.""
Output JSON:
{{
  ""original"": ""Ho comprato una libro nuovo."",
  ""segments"": [
    {{ ""text"": ""Ho comprato "", ""isCorrection"": false }},
    {{ ""text"": ""un"", ""isCorrection"": true }},
    {{ ""text"": "" libro nuovo."", ""isCorrection"": false }}
  ]
}}

Example 6 (Simple or literal, but grammatically valid - MUST BE ACCEPTED AS CORRECT):
Target language: English
Input: ""I want to drink water because I am thirsty.""
Output JSON:
{{
  ""original"": ""I want to drink water because I am thirsty."",
  ""segments"": [
    {{ ""text"": ""I want to drink water because I am thirsty."", ""isCorrection"": false }}
  ]
}}

Example 7 (Completely correct sentence without trailing period - MUST BE ACCEPTED AS CORRECT):
Target language: Italian
Input: ""Io voglio andare al mare""
Output JSON:
{{
  ""original"": ""Io voglio andare al mare"",
  ""segments"": [
    {{ ""text"": ""Io voglio andare al mare"", ""isCorrection"": false }}
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

            // If all words in the returned text are identical to the original sentence,
            // no actual words, grammar, spelling or foreign words were modified.
            // In this case, always treat the sentence as fully correct!
            if (AreWordsIdentical(sentence, concatenatedText))
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

            var cleanedFullText = string.Concat(cleanedSegments.Select(s => s.Text ?? string.Empty)).Trim();
            if (!cleanedSegments.Any(s => s.IsCorrection) || AreWordsIdentical(sentence, cleanedFullText))
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

        var langName = LanguageOption.GetLanguageName(sourceLanguage);
        var personaDescription = persona.GetPromptDescription();

        var prompt = $@"
You are an introspective AI diary writing coach.
The user is writing their personal diary in: {langName} (language code: {sourceLanguage}).
Active persona: {personaDescription}.

Current diary entry text so far:
""""""
{diaryContext}
""""""

Task:
1. Analyze the context, theme, and language complexity of the user's written diary entry.
2. Formulate EXACTLY ONE short, targeted, open-ended question that helps the user overcome writer's block and expand their thought according to the active persona.
3. Match the language difficulty to the user's demonstrated proficiency level (Krashen's i+1 principle: natural and accessible, not overly complex).
4. Do NOT include greetings, praise, introductory phrases, conversational filler, or translations. Formulate strictly the single question in {langName}.

Return strictly JSON with this structure:
{{
  ""question"": ""Single question in {langName}""
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

    public async Task<string> GenerateEntryTitleAsync(string diaryContext, string language, CancellationToken cancellationToken = default)
    {
        var apiKey = await _settingsService.GetMistralApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(diaryContext))
        {
            return string.Empty;
        }

        var langName = LanguageOption.GetLanguageName(language);

        var prompt = $@"
You are an AI diary assistant.
Analyze these first sentences from a personal diary written in {langName}:
""""""
{diaryContext}
""""""

Task:
Generate a very concise, meaningful, and engaging title for this diary entry.
Rules:
1. Formulate strictly in {langName}.
2. Maximum 2 to 5 words. No quotation marks, no period at the end.
3. Capture the essence or main topic of what the writer is talking about.

Return strictly JSON with this structure:
{{
  ""title"": ""Short Title in {langName}""
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
                temperature = 0.3
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

            var titleResult = JsonSerializer.Deserialize<TitleDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return titleResult?.Title?.Trim(' ', '"', '.', '«', '»') ?? string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating title: {ex.Message}");
            return string.Empty;
        }
    }

    private static readonly char[] WordTrimChars = new[]
    {
        '.', '!', '?', ',', ';', ':', '—', '-', '"', '\'', '»', '«', '”', '“', '(', ')', '[', ']', '¿', '¡'
    };

    private static bool AreWordsIdentical(string s1, string s2)
    {
        if (string.Equals(s1.Trim(), s2.Trim(), StringComparison.Ordinal))
            return true;

        var w1 = s1.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(w => w.Trim(WordTrimChars))
                   .Where(w => !string.IsNullOrEmpty(w))
                   .ToList();

        var w2 = s2.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(w => w.Trim(WordTrimChars))
                   .Where(w => !string.IsNullOrEmpty(w))
                   .ToList();

        if (w1.Count != w2.Count) return false;

        for (int i = 0; i < w1.Count; i++)
        {
            if (!string.Equals(w1[i], w2[i], StringComparison.Ordinal))
                return false;
        }

        return true;
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

    private class TitleDto
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }
}
