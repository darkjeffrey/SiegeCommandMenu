using TaleWorlds.Localization;

namespace SiegeCommandMenu
{
    /// <summary>
    /// Player-facing text. Every string is written as "{=scm_id}English text"; translations go in
    /// _Module/ModuleData/Languages (kept up to date with the localization tool).
    /// </summary>
    internal static class Text
    {
        /// <summary>The text in the player's language, with {NAME} variables given as name/value pairs.</summary>
        public static string Get(string text, params object[] variables)
        {
            var textObject = new TextObject(text);
            for (int i = 0; i + 1 < variables.Length; i += 2)
                textObject.SetTextVariable((string)variables[i], variables[i + 1]?.ToString() ?? "");
            return textObject.ToString();
        }
    }
}
