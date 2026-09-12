using DiaryHelper.Models;
using DiaryHelper.Services.Implementations;
using DiaryHelper.Services.Interfaces;
using System.Text;
using Xunit.Abstractions;

namespace DiaryHelper.Tests;

public class MistralLiveIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public MistralLiveIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public record LiveTestCase(
        string Sentence,
        string LanguageCode,
        bool ExpectHasCorrections,
        string Category,
        string? ExpectedKeywordOrMeaning = null
    );

    private class LiveSettingsService : ISettingsService
    {
        private readonly string _apiKey;

        public LiveSettingsService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public Task<string?> GetMistralApiKeyAsync() => Task.FromResult<string?>(_apiKey);
        public Task SetMistralApiKeyAsync(string? key) => Task.CompletedTask;
        public Task<string?> GetGoogleTranslateApiKeyAsync() => Task.FromResult<string?>("dummy");
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

    public static readonly List<LiveTestCase> TestCases = new()
    {
        // Category 1: Valid sentences (must NOT be falsely flagged as errors across languages)
        new("Oggi fa bel tempo e voglio fare una passeggiata.", "it", false, "Valid Italian"),
        new("Oggi vado al lavoro in bicicletta", "it", false, "Valid Italian (no trailing period)"),
        new("Io voglio bere acqua perché ho molta sete.", "it", false, "Valid Italian simple phrasing"),
        new("Ieri ho cucinato un piatto di pasta con pomodori freschi.", "it", false, "Valid Italian past tense"),
        new("I decided to write in my diary every single day.", "en", false, "Valid English"),
        new("She likes drinking green tea in the morning.", "en", false, "Valid English"),
        new("Yesterday I read an interesting article about astronomy.", "en", false, "Valid English past tense"),
        new("Hoy es un día maravilloso para aprender algo nuevo.", "es", false, "Valid Spanish"),
        new("Me gusta caminar por el parque cuando no llueve.", "es", false, "Valid Spanish complex"),
        new("Je veux apprendre une nouvelle langue cette année.", "fr", false, "Valid French"),
        new("Ich trinke jeden Morgen eine Tasse Kaffee.", "de", false, "Valid German"),

        // Category 2: Foreign words / code-switching (MUST be replaced and flagged)
        new("Oggi ho mangiato una delicious mela.", "it", true, "Foreign Word (English in Italian)", "deliziosa"),
        new("Io voglio comprare una машина.", "it", true, "Foreign Word (Russian in Italian)", "macchina"),
        new("Domani devo andare to the airport.", "it", true, "Foreign Phrase (English in Italian)", "aeroporto"),
        new("Oggi ho molto work da finire prima di uscire.", "it", true, "Foreign Word (English in Italian)", "lavoro"),
        new("Yesterday I went to the магазин to buy bread.", "en", true, "Foreign Word (Russian in English)", "store"),
        new("I want to buy a new coche next year.", "en", true, "Foreign Word (Spanish in English)", "car"),
        new("Ayer compré un book muy interesante.", "es", true, "Foreign Word (English in Spanish)", "libro"),
        new("Ella trabaja en una company muy grande.", "es", true, "Foreign Word (English in Spanish)", "empresa"),
        new("Je veux acheter un телефон demain.", "fr", true, "Foreign Word (Russian in French)", "téléphone"),

        // Category 3: Grammatical errors (conjugation, agreement, auxiliary, prepositions, modal syntax)
        new("Io andare a casa adesso.", "it", true, "Grammar (Infinitive verb: andare -> vado)", "vado"),
        new("Noi volere mangiare una pizza stasera.", "it", true, "Grammar (Infinitive plural: volere -> vogliamo)", "vogliamo"),
        new("Ho comprato una libro nuovo.", "it", true, "Grammar (Gender agreement: una libro -> un libro)", "un"),
        new("Ho visto tre gatto neri nel giardino.", "it", true, "Grammar (Number agreement: tre gatto -> tre gatti)", "gatti"),
        new("Io ho andato al cinema ieri sera.", "it", true, "Grammar (Auxiliary verb: ho andato -> sono andato)", "sono"),
        new("Vado in il centro con i miei amici.", "it", true, "Grammar (Preposition contraction: in il -> nel/al)", "nel"),
        new("She go to school by bus every day.", "en", true, "Grammar (Subject-verb agreement: go -> goes)", "goes"),
        new("He goed to the cinema yesterday.", "en", true, "Grammar (Irregular past tense: goed -> went)", "went"),
        new("I can to speak three languages.", "en", true, "Grammar (Modal syntax: can to speak -> can speak)", "speak"),
        new("Him went to the supermarket yesterday.", "en", true, "Grammar (Subject pronoun: Him -> He)", "He"),
        new("Yo querer comer una ensalada fresca.", "es", true, "Grammar (Spanish infinitive -> quiero)", "quiero"),
        new("Ella tiene dos casas bonitos en la playa.", "es", true, "Grammar (Spanish gender agreement: casas bonitos -> bonitas)", "bonitas"),

        // Category 4: Spelling / typo errors
        new("Domani vado al teatrro con i miei amici.", "it", true, "Spelling Typo (teatrro -> teatro)", "teatro"),
        new("Oggi è domanica e voglio riposare.", "it", true, "Spelling Typo (domanica -> domenica)", "domenica"),
        new("I am vary happy today.", "en", true, "Spelling Typo (vary -> very)", "very"),
        new("I will definately visit Italy next summer.", "en", true, "Spelling Typo (definately -> definitely)", "definitely")
    };

    [Fact]
    public async Task RunLiveMistralEvaluationSuite()
    {
        var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _output.WriteLine("SKIPPED: MISTRAL_API_KEY environment variable is not set.");
            _output.WriteLine("Provide the API key to run live cloud verification against api.mistral.ai.");
            return;
        }

        using var httpClient = new HttpClient();
        var settings = new LiveSettingsService(apiKey);
        var service = new MistralService(httpClient, settings);

        int passedCount = 0;
        int falsePositives = 0;
        int falseNegatives = 0;
        int total = TestCases.Count;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("==========================================================================================");
        sb.AppendLine("                    MISTRAL LIVE CLOUD VERIFICATION BENCHMARK                             ");
        sb.AppendLine("==========================================================================================");

        for (int i = 0; i < TestCases.Count; i++)
        {
            var tc = TestCases[i];
            var analysis = await service.AnalyzeSentenceAsync(tc.Sentence, tc.LanguageCode);

            bool matched = analysis.HasCorrections == tc.ExpectHasCorrections;
            string status;

            if (matched)
            {
                passedCount++;
                status = "PASS";
            }
            else if (!tc.ExpectHasCorrections && analysis.HasCorrections)
            {
                falsePositives++;
                status = "FAIL (False Positive - Correct sentence flagged as error)";
            }
            else
            {
                falseNegatives++;
                status = "FAIL (False Negative - Defect not caught)";
            }

            sb.AppendLine($"[{i + 1:D2}/{total:D2}] [{status}] Category: {tc.Category}");
            sb.AppendLine($"      Input:       \"{tc.Sentence}\" (Lang: {tc.LanguageCode})");
            sb.AppendLine($"      Corrected:   \"{analysis.CorrectedFullText}\"");
            sb.AppendLine($"      HasErrors:   {analysis.HasCorrections} (Expected: {tc.ExpectHasCorrections})");
            if (analysis.HasCorrections)
            {
                var correctedSegments = analysis.Segments.Where(s => s.IsCorrection).Select(s => $"\"{s.Text}\"");
                sb.AppendLine($"      Edits:       [{string.Join(", ", correctedSegments)}]");
            }
            sb.AppendLine();
        }

        sb.AppendLine("------------------------------------------------------------------------------------------");
        sb.AppendLine($"TOTAL TESTS:        {total}");
        sb.AppendLine($"PASSED:             {passedCount} ({passedCount * 100.0 / total:F1}%)");
        sb.AppendLine($"FALSE POSITIVES:    {falsePositives} (Trivial edits / identical false flags)");
        sb.AppendLine($"FALSE NEGATIVES:    {falseNegatives} (Missed foreign words / missed errors)");
        sb.AppendLine("==========================================================================================");

        _output.WriteLine(sb.ToString());
        Console.WriteLine(sb.ToString());

        Assert.Equal(0, falsePositives);
        Assert.Equal(0, falseNegatives);
        Assert.Equal(total, passedCount);
    }

    [Fact]
    public async Task ReproduceReportedMistralIssues()
    {
        var apiKey = Environment.GetEnvironmentVariable("MISTRAL_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _output.WriteLine("SKIPPED: MISTRAL_API_KEY environment variable is not set.");
            return;
        }

        using var httpClient = new HttpClient();
        var settings = new LiveSettingsService(apiKey);
        var service = new MistralService(httpClient, settings);

        // Case 1: Screenshot 1 - "Quasi instantaneamente ho trovato il modello molto bello di Casio."
        var sent1 = "Quasi instantaneamente ho trovato il modello molto bello di Casio.";
        var res1 = await service.AnalyzeSentenceAsync(sent1, "it");

        _output.WriteLine("=== CASE 1: Typo in single word (instantaneamente -> istantaneamente) ===");
        _output.WriteLine($"Original:  \"{sent1}\"");
        _output.WriteLine($"Corrected: \"{res1.CorrectedFullText}\"");
        _output.WriteLine($"HasErrors: {res1.HasCorrections}");
        for (int i = 0; i < res1.Segments.Count; i++)
        {
            _output.WriteLine($"  Segment[{i}]: Text=\"{res1.Segments[i].Text}\", IsCorrection={res1.Segments[i].IsCorrection}");
        }

        // Case 2: Screenshot 2 - "L'azienda di Casio ha deciso di usare il modello di Seiko per non fallire con il proprio meccanismo, perché non hanno un'esperienza con i meccanismi meccanici."
        var sent2 = "L'azienda di Casio ha deciso di usare il modello di Seiko per non fallire con il proprio meccanismo, perché non hanno un'esperienza con i meccanismi meccanici.";
        var res2 = await service.AnalyzeSentenceAsync(sent2, "it");

        _output.WriteLine("=== CASE 2: Long sentence with Casio/Seiko ===");
        _output.WriteLine($"Original:  \"{sent2}\"");
        _output.WriteLine($"Corrected: \"{res2.CorrectedFullText}\"");
        _output.WriteLine($"HasErrors: {res2.HasCorrections}");
        for (int i = 0; i < res2.Segments.Count; i++)
        {
            _output.WriteLine($"  Segment[{i}]: Text=\"{res2.Segments[i].Text}\", IsCorrection={res2.Segments[i].IsCorrection}");
        }

        // Assertions for Case 1:
        // 1. Must catch typo instantaneamente -> istantaneamente
        Assert.True(res1.HasCorrections, "Should detect typo in instantaneamente");
        Assert.Contains("istantaneamente", res1.CorrectedFullText);
        // 2. Unchanged words like "Quasi", "modello", "Casio" must NOT be marked as corrections!
        Assert.False(res1.Segments.Any(s => s.IsCorrection && s.Text.Trim() == "Quasi"), "Unchanged word 'Quasi' should NOT be marked as correction!");
        Assert.False(res1.Segments.Any(s => s.IsCorrection && s.Text.Contains("modello")), "Unchanged word 'modello' should NOT be marked as correction!");
        Assert.False(res1.Segments.Any(s => s.IsCorrection && s.Text.Contains("Casio")), "Unchanged word 'Casio' should NOT be marked as correction!");

        // Assertions for Case 2:
        // Must NOT hallucinate consecutive duplicate or conflicting verbs like "hanno hanno" or "hanno ha"!
        Assert.DoesNotContain("hanno hanno", res2.CorrectedFullText);
        Assert.DoesNotContain("hanno ha", res2.CorrectedFullText);
        Assert.DoesNotContain("ha hanno", res2.CorrectedFullText);
    }
}
