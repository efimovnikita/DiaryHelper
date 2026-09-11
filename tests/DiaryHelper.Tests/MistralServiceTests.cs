using DiaryHelper.Models;
using DiaryHelper.Services.Implementations;
using DiaryHelper.Services.Interfaces;
using System.Net;
using System.Text;

namespace DiaryHelper.Tests;

public class MistralServiceTests
{
    private class TestSettingsService : ISettingsService
    {
        public string? MistralApiKey { get; set; } = "fake-test-key-12345";
        public Task<string?> GetMistralApiKeyAsync() => Task.FromResult(MistralApiKey);
        public Task SetMistralApiKeyAsync(string? key) { MistralApiKey = key; return Task.CompletedTask; }
        public Task<string?> GetGoogleTranslateApiKeyAsync() => Task.FromResult<string?>("fake-google-key");
        public Task SetGoogleTranslateApiKeyAsync(string? key) => Task.CompletedTask;
        public string GetSourceLanguage() => "it";
        public void SetSourceLanguage(string code) { }
        public string GetTargetLanguage() => "en";
        public void SetTargetLanguage(string code) { }
        public PersonaType GetDefaultPersona() => PersonaType.Friend;
        public void SetDefaultPersona(PersonaType persona) { }
        public bool GetShowBotPromptTranslation() => true;
        public void SetShowBotPromptTranslation(bool show) { }
        public Task<bool> HasRequiredKeysAsync() => Task.FromResult(true);
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK);

        public void SetJsonResponse(string jsonContent)
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return ResponseToReturn;
        }
    }

    private static (MistralService service, FakeHttpMessageHandler handler) CreateService()
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var settings = new TestSettingsService();
        var service = new MistralService(httpClient, settings);
        return (service, handler);
    }

    private static string WrapInMistralResponse(string innerJson)
    {
        // Mistral API returns { "choices": [ { "message": { "content": "<json_string>" } } ] }
        var escaped = innerJson.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", "\\n");
        return $"{{\"choices\":[{{\"message\":{{\"content\":\"{escaped}\"}}}}]}}";
    }

    [Fact]
    public async Task AnalyzeSentence_IncludesExplicitLanguageNameAndRules_InSystemPrompt()
    {
        var (service, handler) = CreateService();
        handler.SetJsonResponse(WrapInMistralResponse("{\"original\":\"Io vado.\",\"segments\":[{\"text\":\"Io vado.\",\"isCorrection\":false}]}"));

        await service.AnalyzeSentenceAsync("Io vado.", "it");

        var body = handler.LastRequestBody;
        Assert.NotNull(body);
        Assert.Contains("Italian", body);
        Assert.Contains("TARGET LANGUAGE: Italian", body);
        Assert.Contains("MANDATORY CORRECTION OF ALL OBJECTIVE ERRORS", body);
        Assert.Contains("Foreign words / code-switching", body);
        Assert.Contains("FORBIDDEN TO SUGGEST TRIVIAL, IDENTICAL, OR PURELY STYLISTIC EDITS", body);
    }

    [Fact]
    public async Task AnalyzeSentence_WhenReturnedWordsAreIdentical_OverridesFalsePositiveCorrection()
    {
        var (service, handler) = CreateService();
        // Model erroneously returned isCorrection: true on identical text
        var modelJson = "{\"original\":\"Io voglio andare al mare.\",\"segments\":[{\"text\":\"Io voglio andare \",\"isCorrection\":false},{\"text\":\"al mare.\",\"isCorrection\":true}]}";
        handler.SetJsonResponse(WrapInMistralResponse(modelJson));

        var result = await service.AnalyzeSentenceAsync("Io voglio andare al mare.", "it");

        // Should be cleansed because no actual word changed
        Assert.False(result.HasCorrections);
        Assert.True(result.IsCorrect);
        Assert.Single(result.Segments);
        Assert.False(result.Segments[0].IsCorrection);
    }

    [Fact]
    public async Task AnalyzeSentence_WhenOnlyTrailingPeriodAdded_MarksAsCorrect()
    {
        var (service, handler) = CreateService();
        // User wrote no period, model added period and marked as correction
        var modelJson = "{\"original\":\"Io voglio andare al mare\",\"segments\":[{\"text\":\"Io voglio andare al mare.\",\"isCorrection\":true}]}";
        handler.SetJsonResponse(WrapInMistralResponse(modelJson));

        var result = await service.AnalyzeSentenceAsync("Io voglio andare al mare", "it");

        // Adding a period is not a grammar error; sentence should be correct
        Assert.False(result.HasCorrections);
        Assert.True(result.IsCorrect);
    }

    [Fact]
    public async Task AnalyzeSentence_WhenEnglishForeignWordInItalian_DetectsCorrection()
    {
        var (service, handler) = CreateService();
        var modelJson = "{\"original\":\"Oggi ho mangiato una delicious mela.\",\"segments\":[{\"text\":\"Oggi ho mangiato una \",\"isCorrection\":false},{\"text\":\"deliziosa\",\"isCorrection\":true},{\"text\":\" mela.\",\"isCorrection\":false}]}";
        handler.SetJsonResponse(WrapInMistralResponse(modelJson));

        var result = await service.AnalyzeSentenceAsync("Oggi ho mangiato una delicious mela.", "it");

        Assert.True(result.HasCorrections);
        Assert.False(result.IsCorrect);
        var correctedSegment = result.Segments.FirstOrDefault(s => s.IsCorrection);
        Assert.NotNull(correctedSegment);
        Assert.Equal("deliziosa", correctedSegment.Text);
        Assert.Equal("Oggi ho mangiato una deliziosa mela.", result.CorrectedFullText);
    }

    [Fact]
    public async Task AnalyzeSentence_WhenRussianForeignWordInItalian_DetectsCorrection()
    {
        var (service, handler) = CreateService();
        var modelJson = "{\"original\":\"Io voglio comprare una машина.\",\"segments\":[{\"text\":\"Io voglio comprare una \",\"isCorrection\":false},{\"text\":\"macchina.\",\"isCorrection\":true}]}";
        handler.SetJsonResponse(WrapInMistralResponse(modelJson));

        var result = await service.AnalyzeSentenceAsync("Io voglio comprare una машина.", "it");

        Assert.True(result.HasCorrections);
        Assert.False(result.IsCorrect);
        Assert.Contains(result.Segments, s => s.IsCorrection && s.Text == "macchina.");
    }

    [Fact]
    public async Task AnalyzeSentence_WhenGrammarConjugationError_DetectsCorrection()
    {
        var (service, handler) = CreateService();
        var modelJson = "{\"original\":\"Io andare a casa.\",\"segments\":[{\"text\":\"Io \",\"isCorrection\":false},{\"text\":\"vado\",\"isCorrection\":true},{\"text\":\" a casa.\",\"isCorrection\":false}]}";
        handler.SetJsonResponse(WrapInMistralResponse(modelJson));

        var result = await service.AnalyzeSentenceAsync("Io andare a casa.", "it");

        Assert.True(result.HasCorrections);
        Assert.False(result.IsCorrect);
        Assert.Equal("vado", result.Segments[1].Text);
    }

    [Fact]
    public async Task AnalyzeSentence_WhenCapitalizationError_DetectsCorrection()
    {
        var (service, handler) = CreateService();
        var modelJson = "{\"original\":\"io vado a roma.\",\"segments\":[{\"text\":\"Io \",\"isCorrection\":true},{\"text\":\"vado a \",\"isCorrection\":false},{\"text\":\"Roma.\",\"isCorrection\":true}]}";
        handler.SetJsonResponse(WrapInMistralResponse(modelJson));

        var result = await service.AnalyzeSentenceAsync("io vado a roma.", "it");

        Assert.True(result.HasCorrections);
        Assert.False(result.IsCorrect);
    }

    [Theory]
    [InlineData("it", "Italian")]
    [InlineData("en", "English")]
    [InlineData("ru", "Russian")]
    [InlineData("es", "Spanish")]
    [InlineData("fr", "French")]
    [InlineData("de", "German")]
    [InlineData("pt", "Portuguese")]
    [InlineData("zh", "Chinese")]
    [InlineData("ja", "Japanese")]
    [InlineData("ko", "Korean")]
    [InlineData("ar", "Arabic")]
    [InlineData("nl", "Dutch")]
    [InlineData("pl", "Polish")]
    [InlineData("tr", "Turkish")]
    public void LanguageOption_GetLanguageName_MapsAllSupportedLanguagesCorrectly(string code, string expectedName)
    {
        var name = LanguageOption.GetLanguageName(code);
        Assert.Equal(expectedName, name);
    }

    [Fact]
    public async Task GenerateKickQuestion_UsesTargetLanguageNameInPrompt()
    {
        var (service, handler) = CreateService();
        handler.SetJsonResponse(WrapInMistralResponse("{\"question\":\"Come stai oggi?\"}"));

        var question = await service.GenerateKickQuestionAsync("Oggi è una bella giornata.", "it", PersonaType.Friend);

        Assert.Equal("Come stai oggi?", question);
        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("Italian", handler.LastRequestBody);
        Assert.DoesNotContain("diary in: it.", handler.LastRequestBody);
    }

    [Fact]
    public async Task GenerateEntryTitle_UsesTargetLanguageNameInPrompt()
    {
        var (service, handler) = CreateService();
        handler.SetJsonResponse(WrapInMistralResponse("{\"title\":\"Una bella giornata\"}"));

        var title = await service.GenerateEntryTitleAsync("Oggi sono andato a passeggiare.", "it");

        Assert.Equal("Una bella giornata", title);
        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("Italian", handler.LastRequestBody);
        Assert.DoesNotContain("written in it:", handler.LastRequestBody);
    }
}
