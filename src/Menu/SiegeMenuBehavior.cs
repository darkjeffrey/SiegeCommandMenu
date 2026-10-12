using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SiegeCommandMenu
{
    /// <summary>
    /// Everything lives in the left-hand game menu. The besieger's siege menu gets a
    /// "Siege Planning" option; that opens a planning menu (camp progress plus one option per
    /// slot / reserve engine), and picking one opens a slot menu with the build / deploy /
    /// move-to-reserve options. Game menu options are static, so each screen has a pool of
    /// options whose text and visibility are decided in their condition.
    /// </summary>
    internal partial class SiegeMenuBehavior : CampaignBehaviorBase
    {
        // The besieger's own game menu, which gets the "Siege Planning" option.
        private const string MainMenuId = "menu_siege_strategies";
        private const string PlanningMenuId = "scm_planning";
        private const string SlotMenuId = "scm_slot";

        private const int RowPool = 24;
        private const int BuildPool = 8;
        private const int ReservePool = 10;

        private static SlotKind _selectedKind;
        private static int _selectedIndex;
        private static string _note = "";
        // When true, newly queued engines are sent to the reserve instead of staying in the slot.
        private static bool _buildToReserve;
        // Slots ("R0", "M1", ...) whose unfinished engine should move to the reserve when done.
        private static List<string> _flags = new List<string>();

        private static string Key(SlotKind kind, int index) => (kind == SlotKind.Ranged ? "R" : "M") + index;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourly);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("scm_reserve_flags", ref _flags);
            if (_flags == null)
                _flags = new List<string>();
        }

        // Moves are queued here by the hourly tick and carried out on a later frame, outside the
        // game's own hourly processing.
        private static readonly List<string> _pendingMoves = new List<string>();

        /// <summary>Finds flagged engines that are finished and queues them for the reserve.</summary>
        private void OnHourly()
        {
            try
            {
                var siege = Siege;
                SiegeActions.FinishEnginesIfCheat(siege);
                ModLog.Debug(() => siege == null ? "Hourly | no siege"
                    : "Hourly | flags=[" + string.Join(",", _flags) + "] " + SiegeActions.Describe(siege));

                if (siege == null || _flags.Count == 0)
                    return;

                foreach (var key in _flags.ToList())
                {
                    bool ranged = key[0] == 'R';
                    int index;
                    if (!int.TryParse(key.Substring(1), out index))
                    {
                        _flags.Remove(key);
                        continue;
                    }

                    var slots = SiegeActions.DeployedSlots(siege, ranged);
                    var engine = index < slots.Count ? slots[index] : null;
                    if (engine == null)
                    {
                        _flags.Remove(key);
                        continue;
                    }

                    if (engine.Progress >= 1f && !_pendingMoves.Contains(key))
                        _pendingMoves.Add(key);
                }
            }
            catch (Exception e)
            {
                ModLog.Error("OnHourly", e);
            }
        }

        /// <summary>Called every frame: carries out queued moves to the reserve.</summary>
        public static void ProcessPending()
        {
            if (_pendingMoves.Count == 0)
                return;

            var keys = _pendingMoves.ToList();
            _pendingMoves.Clear();

            try
            {
                var siege = Siege;
                if (siege == null)
                    return;

                foreach (var key in keys)
                {
                    bool ranged = key[0] == 'R';
                    int index = int.Parse(key.Substring(1));
                    _flags.Remove(key);

                    var slots = SiegeActions.DeployedSlots(siege, ranged);
                    var engine = index < slots.Count ? slots[index] : null;
                    if (engine == null || engine.Progress < 1f)
                        continue;

                    string name = engine.SiegeEngine.Name.ToString();
                    bool moved = SiegeActions.MoveToReserve(siege, ranged, index);
                    InformationManager.DisplayMessage(new InformationMessage(moved
                        ? name + " finished and moved to the reserve."
                        : name + " finished, but could not be moved to the reserve."));
                }
            }
            catch (Exception e)
            {
                ModLog.Error("ProcessPending", e);
            }
        }

        private static SiegeEvent Siege =>
            PlayerSiege.PlayerSide == BattleSideEnum.Attacker ? PlayerSiege.PlayerSiegeEvent : null;

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Entry in the besieger's menu.
            starter.AddGameMenuOption(MainMenuId, "scm_open", "Siege Planning",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    return Siege != null;
                },
                args => GameMenu.SwitchToMenu(PlanningMenuId),
                false, 0);

            // Planning menu: camp progress in the text, one option per slot / reserve engine.
            starter.AddGameMenu(PlanningMenuId, "{SCM_PLANNING_TEXT}", PlanningInit);

            for (int i = 0; i < RowPool; i++)
            {
                int row = i;
                starter.AddGameMenuOption(PlanningMenuId, "scm_row_" + row, "{SCM_ROW_" + row + "}",
                    args => RowCondition(args, row),
                    args => RowSelected(row));
            }

            starter.AddGameMenuOption(PlanningMenuId, "scm_planning_back", "Back to the siege menu",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => GameMenu.SwitchToMenu(MainMenuId),
                true);

            // Slot menu: actions for the selected slot / reserve engine.
            starter.AddGameMenu(SlotMenuId, "{SCM_SLOT_TEXT}", SlotInit);

            starter.AddGameMenuOption(SlotMenuId, "scm_dest", "{SCM_DEST}",
                args =>
                {
                    if (!SelectedIsEmptySlot())
                        return false;

                    MBTextManager.SetTextVariable("SCM_DEST",
                        "When finished: " + (_buildToReserve ? "send to reserve" : "stay deployed") + " (click to change)");
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    return true;
                },
                args =>
                {
                    _buildToReserve = !_buildToReserve;
                    GameMenu.SwitchToMenu(SlotMenuId);
                });

            for (int i = 0; i < BuildPool; i++)
            {
                int b = i;
                starter.AddGameMenuOption(SlotMenuId, "scm_build_" + b, "{SCM_BUILD_" + b + "}",
                    args => BuildCondition(args, b),
                    args => BuildSelected(b));
            }

            for (int i = 0; i < ReservePool; i++)
            {
                int r = i;
                starter.AddGameMenuOption(SlotMenuId, "scm_fromreserve_" + r, "{SCM_FROMRESERVE_" + r + "}",
                    args => FromReserveCondition(args, r),
                    args => FromReserveSelected(r));
            }

            starter.AddGameMenuOption(SlotMenuId, "scm_deploy_selected", "Deploy to a free slot",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                    return SelectedEngine(out bool deployed) != null && !deployed;
                },
                args => DeploySelected());

            starter.AddGameMenuOption(SlotMenuId, "scm_to_reserve", "Move to reserve",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    return SelectedFinishedDeployed();
                },
                args => MoveSelectedToReserve());

            starter.AddGameMenuOption(SlotMenuId, "scm_flag", "{SCM_FLAG}",
                args =>
                {
                    if (!SelectedUnfinished())
                        return false;

                    MBTextManager.SetTextVariable("SCM_FLAG",
                        "When finished: " + (_flags.Contains(Key(_selectedKind, _selectedIndex)) ? "send to reserve" : "stay deployed") + " (click to change)");
                    args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    return true;
                },
                args => ToggleSelectedFlag());

            starter.AddGameMenuOption(SlotMenuId, "scm_cancel", "Cancel construction",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return SelectedUnfinished();
                },
                args => CancelSelected());

            starter.AddGameMenuOption(SlotMenuId, "scm_slot_back", "Back to planning",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args => GameMenu.SwitchToMenu(PlanningMenuId),
                true);
        }
    }
}
