using System.Collections.Generic;

namespace SiegeCommandMenu
{
    /// <summary>The game's own siege picture for each engine (the set its siege screen uses).</summary>
    internal static class EngineSprites
    {
        private static readonly Dictionary<string, string> ByEngineId = new Dictionary<string, string>
        {
            { "ballista", "SPGeneral\\Siege\\Ballista" },
            { "fire_ballista", "SPGeneral\\Siege\\FireBallista" },
            { "catapult", "SPGeneral\\Siege\\Catapult" },
            { "fire_catapult", "SPGeneral\\Siege\\FireCatapult" },
            { "onager", "SPGeneral\\Siege\\Onager" },
            { "fire_onager", "SPGeneral\\Siege\\FireOnager" },
            { "trebuchet", "SPGeneral\\Siege\\Trebuchet" },
            { "fire_trebuchet", "SPGeneral\\Siege\\FireTrebuchet" },
            { "bricole", "SPGeneral\\Siege\\Bricole" },
            { "ram", "SPGeneral\\Siege\\Ram" },
            { "improved_ram", "SPGeneral\\Siege\\ImprovedRam" },
            { "siege_tower_level1", "SPGeneral\\Siege\\SiegeTowerLevel1" },
            { "siege_tower_level2", "SPGeneral\\Siege\\SiegeTowerLevel2" },
            { "ladder", "SPGeneral\\Siege\\Ladder" }
        };

        /// <summary>The sprite name for an engine id, or empty (no picture) for engines from other mods.</summary>
        public static string For(string engineId)
        {
            return engineId != null && ByEngineId.TryGetValue(engineId, out string sprite) ? sprite : "";
        }
    }
}
