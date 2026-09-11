namespace DiaryHelper.Models;

public enum PersonaType
{
    Friend,
    Reporter,
    Sage,
    Spark
}

public record PersonaOption(PersonaType Type, string DisplayName);

public static class PersonaExtensions
{
    public static string GetDisplayName(this PersonaType persona) => persona switch
    {
        PersonaType.Friend => AppStrings.PersonaFriend,
        PersonaType.Reporter => AppStrings.PersonaReporter,
        PersonaType.Sage => AppStrings.PersonaSage,
        PersonaType.Spark => AppStrings.PersonaSpark,
        _ => AppStrings.PersonaFriend
    };

    public static string GetEmoji(this PersonaType persona) => persona switch
    {
        PersonaType.Friend => "🧘",
        PersonaType.Reporter => "🕵️",
        PersonaType.Sage => "🦉",
        PersonaType.Spark => "⚡",
        _ => "✨"
    };

    public static string GetPromptDescription(this PersonaType persona) => persona switch
    {
        PersonaType.Friend => "The Empathetic Friend: focus on author's feelings, mood, emotional wellbeing and internal state.",
        PersonaType.Reporter => "The Inquisitive Reporter: focus on facts, chronology, sensory surroundings (sounds, sights, weather) and concrete actions.",
        PersonaType.Sage => "The Reflective Sage: focus on deeper meaning, personal values, life lessons, why things happened and conclusions for the future.",
        PersonaType.Spark => "The Creative Spark: focus on hypothetical 'what if' twists, unexpected angles, analogies, or thought-provoking creative challenges.",
        _ => "Supportive and curious diary companion."
    };
}

