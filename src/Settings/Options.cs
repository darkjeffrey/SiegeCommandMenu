using System;
using System.Runtime.CompilerServices;

namespace SiegeCommandMenu
{
    /// <summary>
    /// The mod's options, read from Mod Configuration Menu. If MCM is missing, or a setting cannot
    /// be read for any reason, each option falls back to its default, so the mod still works.
    /// </summary>
    internal static class Options
    {
        public static bool ShowEnginePanel => Get(nameof(ModSettings.ShowEnginePanel), true);

        public static bool PanelClickable => Get(nameof(ModSettings.PanelClickable), true);

        public static bool AutoReserve => Get(nameof(ModSettings.AutoReserve), true);

        public static bool HideNonFire => Get(nameof(ModSettings.HideNonFire), false);

        public static bool DebugLog => Get(nameof(ModSettings.DebugLog), false);

        public static bool DebugMessages => Get(nameof(ModSettings.DebugMessages), false);

        public static bool CheatInstantCamp => Get(nameof(ModSettings.CheatInstantCamp), false);

        public static bool CheatInstantEngines => Get(nameof(ModSettings.CheatInstantEngines), false);

        private static bool Get(string name, bool fallback)
        {
            try
            {
                return Read(name, fallback);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        // Kept in its own non-inlined method so the settings class is only loaded inside the try above.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool Read(string name, bool fallback)
        {
            var settings = ModSettings.Instance;
            if (settings == null)
                return fallback;

            return (bool)typeof(ModSettings).GetProperty(name).GetValue(settings, null);
        }
    }
}
