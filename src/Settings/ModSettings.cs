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
        private const string PanelGroup = "{=scm_mcm_group_panel}Engine panel";
        private const string BuildingGroup = "{=scm_mcm_group_building}Building";
        private const string DebugGroup = "{=scm_mcm_group_debug}Debug";
        private const string CheatsGroup = "{=scm_mcm_group_cheats}Cheats";

        public override string Id => "SiegeCommandMenu_v1";

        // MCM shows DisplayName as given, so it is resolved here. If the text system is not ready yet,
        // the raw "{=id}text" comes back; the id is dropped then so the English name still shows.
        public override string DisplayName
        {
            get
            {
                string name = Text.Get("{=scm_mcm_title}Siege Command Menu");
                int close = name.StartsWith("{=", System.StringComparison.Ordinal) ? name.IndexOf('}') : -1;
                return close >= 0 ? name.Substring(close + 1) : name;
            }
        }

        public override string FolderName => "SiegeCommandMenu";
        public override string FormatType => "json";

        [SettingPropertyBool("{=scm_mcm_panel}Show engine panel", Order = 0, RequireRestart = false,
            HintText = "{=scm_mcm_panel_hint}Show the panel with engine pictures and stats beside the left menu while planning a siege.")]
        [SettingPropertyGroup(PanelGroup, GroupOrder = 0)]
        public bool ShowEnginePanel { get; set; } = true;

        [SettingPropertyBool("{=scm_mcm_clickable}Click engines in the panel", Order = 1, RequireRestart = false,
            HintText = "{=scm_mcm_clickable_hint}Clicking an engine in the panel does the same as the matching left menu option. Off makes the panel display-only.")]
        [SettingPropertyGroup(PanelGroup, GroupOrder = 0)]
        public bool PanelClickable { get; set; } = true;

        [SettingPropertyBool("{=scm_mcm_auto_reserve}Send new engines to reserve when finished", Order = 2, RequireRestart = false,
            HintText = "{=scm_mcm_auto_reserve_hint}Engines you queue in Siege Planning move to the reserve as soon as they are built. You can still change this per engine in the menu.")]
        [SettingPropertyGroup(BuildingGroup, GroupOrder = 1)]
        public bool AutoReserve { get; set; } = true;

        [SettingPropertyBool("{=scm_mcm_hide_non_fire}Hide non-fire engines when fire versions exist", Order = 3, RequireRestart = false,
            HintText = "{=scm_mcm_hide_non_fire_hint}When the fire version of an engine can be built, hide the plain version from the build list.")]
        [SettingPropertyGroup(BuildingGroup, GroupOrder = 1)]
        public bool HideNonFire { get; set; } = false;

        [SettingPropertyBool("{=scm_mcm_debug_log}Write debug log", Order = 4, RequireRestart = false,
            HintText = "{=scm_mcm_debug_log_hint}Write every build, deploy and reserve action, the siege state before and after, and a state line on every hourly and daily tick to debug_log.txt in the mod's folder. The file is written line by line, so it survives a crash.")]
        [SettingPropertyGroup(DebugGroup, GroupOrder = 50)]
        public bool DebugLog { get; set; } = false;

        [SettingPropertyBool("{=scm_mcm_debug_messages}Print debug info in game", Order = 5, RequireRestart = false,
            HintText = "{=scm_mcm_debug_messages_hint}Also show each siege action and the engine state as messages in the game's message feed (needs the debug log option as well to be saved to the file).")]
        [SettingPropertyGroup(DebugGroup, GroupOrder = 50)]
        public bool DebugMessages { get; set; } = false;

        [SettingPropertyBool("{=scm_mcm_instant_camp}Instant siege camp", Order = 10, RequireRestart = false,
            HintText = "{=scm_mcm_instant_camp_hint}Cheat: the besieger's siege camp is finished instantly, so engines can be built right away.")]
        [SettingPropertyGroup(CheatsGroup, GroupOrder = 100)]
        public bool CheatInstantCamp { get; set; } = false;

        [SettingPropertyBool("{=scm_mcm_instant_engines}Instant siege engines", Order = 11, RequireRestart = false,
            HintText = "{=scm_mcm_instant_engines_hint}Cheat: engines you queue are finished instantly.")]
        [SettingPropertyGroup(CheatsGroup, GroupOrder = 100)]
        public bool CheatInstantEngines { get; set; } = false;
    }
}
