using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace SiegeCommandMenu
{
    /// <summary>
    /// The options screen, shown under Mod Options (Mod Configuration Menu v5). Read through
    /// <see cref="Options"/>, which falls back to defaults if MCM is not installed.
    /// </summary>
    public sealed class ModSettings : AttributeGlobalSettings<ModSettings>
    {
        public override string Id => "SiegeCommandMenu_v1";
        public override string DisplayName => "Siege Command Menu";
        public override string FolderName => "SiegeCommandMenu";
        public override string FormatType => "json";

        // ---- Engine panel

        [SettingPropertyBool("Show engine panel", Order = 0, RequireRestart = false,
            HintText = "Show the panel with engine pictures and stats beside the left menu while planning a siege.")]
        [SettingPropertyGroup("Engine panel", GroupOrder = 0)]
        public bool ShowEnginePanel { get; set; } = true;

        [SettingPropertyBool("Click engines in the panel", Order = 1, RequireRestart = false,
            HintText = "Clicking an engine in the panel does the same as the matching left menu option. Off makes the panel display-only.")]
        [SettingPropertyGroup("Engine panel", GroupOrder = 0)]
        public bool PanelClickable { get; set; } = true;

        // ---- Building

        [SettingPropertyBool("Send new engines to reserve when finished", Order = 2, RequireRestart = false,
            HintText = "Engines you queue in Siege Planning move to the reserve as soon as they are built. You can still change this per engine in the menu.")]
        [SettingPropertyGroup("Building", GroupOrder = 1)]
        public bool AutoReserve { get; set; } = true;

        [SettingPropertyBool("Hide non-fire engines when fire versions exist", Order = 3, RequireRestart = false,
            HintText = "When the fire version of an engine can be built, hide the plain version from the build list.")]
        [SettingPropertyGroup("Building", GroupOrder = 1)]
        public bool HideNonFire { get; set; } = false;

        // ---- Debug

        [SettingPropertyBool("Write debug log", Order = 4, RequireRestart = false,
            HintText = "Write every build, deploy and reserve action to debug_log.txt in the mod's folder.")]
        [SettingPropertyGroup("Debug", GroupOrder = 50)]
        public bool DebugLog { get; set; } = false;

        // ---- Cheats (always last)

        [SettingPropertyBool("Instant siege camp", Order = 10, RequireRestart = false,
            HintText = "Cheat: the besieger's siege camp is finished instantly, so engines can be built right away.")]
        [SettingPropertyGroup("Cheats", GroupOrder = 100)]
        public bool CheatInstantCamp { get; set; } = false;

        [SettingPropertyBool("Instant siege engines", Order = 11, RequireRestart = false,
            HintText = "Cheat: engines you queue are finished instantly.")]
        [SettingPropertyGroup("Cheats", GroupOrder = 100)]
        public bool CheatInstantEngines { get; set; } = false;
    }
}
