using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Localization;

namespace SiegeCommandMenu
{
    /// <summary>The slot menu: build, deploy, move to reserve or cancel for the selected slot or reserve engine.</summary>
    internal partial class SiegeMenuBehavior
    {
        // ---------------------------------------------------------------- selection

        /// <summary>The engine in the selected slot / reserve entry (null if empty), and whether it is deployed.</summary>
        private static SiegeEvent.SiegeEngineConstructionProgress SelectedEngine(out bool deployed)
        {
            deployed = _selectedKind != SlotKind.Reserve;

            var siege = Siege;
            if (siege == null)
                return null;

            var list = _selectedKind == SlotKind.Reserve
                ? SiegeActions.Reserve(siege)
                : SiegeActions.DeployedSlots(siege, _selectedKind == SlotKind.Ranged);

            return _selectedIndex >= 0 && _selectedIndex < list.Count ? list[_selectedIndex] : null;
        }

        private static bool SelectedIsEmptySlot()
        {
            if (_selectedKind == SlotKind.Reserve || Siege == null)
                return false;

            var slots = SiegeActions.DeployedSlots(Siege, _selectedKind == SlotKind.Ranged);
            return _selectedIndex >= 0 && _selectedIndex < slots.Count && slots[_selectedIndex] == null;
        }

        private static bool SelectedUnfinished()
        {
            var engine = SelectedEngine(out bool deployed);
            return engine != null && deployed && engine.Progress < 1f;
        }

        private static bool SelectedFinishedDeployed()
        {
            var engine = SelectedEngine(out bool deployed);
            return engine != null && deployed && engine.Progress >= 1f;
        }

        private static bool SelectedReserveHasFreeSlot()
        {
            var engine = SelectedEngine(out bool deployed);
            return engine != null && !deployed && Siege != null && SiegeActions.FreeSlotFor(Siege, engine) >= 0;
        }

        private static bool SelectedFlagged => _flags.Contains(Key(_selectedKind, _selectedIndex));

        /// <summary>Reserve engines of the right kind that could go into the selected empty slot.</summary>
        private static List<SiegeEvent.SiegeEngineConstructionProgress> MatchingReserve()
        {
            if (!SelectedIsEmptySlot())
                return new List<SiegeEvent.SiegeEngineConstructionProgress>();

            bool ranged = _selectedKind == SlotKind.Ranged;
            return SiegeActions.Reserve(Siege)
                .Where(engine => engine != null && engine.SiegeEngine.IsRanged == ranged
                    && (ranged || SiegeActions.CanPlaceMelee(Siege, _selectedIndex, engine.SiegeEngine)))
                .ToList();
        }

        /// <summary>"When finished: send to reserve (click to change)".</summary>
        private static string WhenFinishedOption(bool toReserve)
        {
            return Text.Get("{=scm_when_finished_option}When finished: {CHOICE} (click to change)", "CHOICE", WhenFinishedChoice(toReserve));
        }

        private static string WhenFinishedChoice(bool toReserve)
        {
            return toReserve ? Text.Get("{=scm_send_to_reserve}send to reserve") : Text.Get("{=scm_stay_deployed}stay deployed");
        }

        // ---------------------------------------------------------------- menu text and panel

        private static void SlotInit(MenuCallbackArgs args)
        {
            args.MenuTitle = MenuTitle;

            var siege = Siege;
            if (siege == null)
            {
                MBTextManager.SetTextVariable("SCM_SLOT_TEXT", NotBesieging);
                return;
            }

            var engine = SelectedEngine(out bool deployed);
            string slotName = SlotName(_selectedKind, _selectedIndex);
            string text = engine == null ? EmptySlotText(siege, slotName) : EngineText(siege, slotName, engine, deployed);
            MBTextManager.SetTextVariable("SCM_SLOT_TEXT", text);
        }

        /// <summary>Menu text and panel for an empty slot: what can be built or deployed into it.</summary>
        private static string EmptySlotText(SiegeEvent siege, string slotName)
        {
            bool clickable = Options.PanelClickable;
            var cards = new List<SiegeEngineCardVM>();

            string text = Text.Get("{=scm_slot_empty}{SLOT} is empty.", "SLOT", slotName)
                + "\n" + Text.Get("{=scm_when_finished}When finished: {CHOICE}", "CHOICE", WhenFinishedChoice(_buildToReserve))
                + "\n" + Text.Get("{=scm_available}Available to build:");

            var buildable = SiegeActions.BuildableTypes(_selectedKind == SlotKind.Ranged, siege, _selectedIndex);
            ModLog.Debug(() => "Build list (hide non-fire " + (Options.HideNonFire ? "on" : "off") + "): "
                + string.Join(", ", buildable.Select(type => type.StringId)));

            string buildHint = _buildToReserve
                ? Text.Get("{=scm_click_build_reserve}Click to build (then reserve)")
                : Text.Get("{=scm_click_build}Click to build");
            for (int i = 0; i < buildable.Count; i++)
            {
                int b = i;
                var type = buildable[i];
                text += "\n" + type.Name + ": " + SiegeInfoOverlay.OneLine(siege, type);
                cards.Add(SiegeInfoOverlay.Card(siege, type, clickable ? buildHint : null, clickable ? (Action)(() => BuildSelected(b)) : null));
            }

            var reserve = MatchingReserve();
            for (int i = 0; i < reserve.Count; i++)
            {
                int r = i;
                cards.Add(SiegeInfoOverlay.Card(siege, reserve[i].SiegeEngine,
                    clickable ? Text.Get("{=scm_click_deploy_here}In reserve: click to deploy here") : null,
                    clickable ? (Action)(() => FromReserveSelected(r)) : null,
                    reserve[i]));
            }

            if (_selectedKind == SlotKind.Melee && buildable.Count == 0 && reserve.Count == 0)
                text += "\n" + Text.Get("{=scm_melee_full}Nothing more can be added: the first slot takes the battering ram, the other two take siege towers.");

            SiegeInfoOverlay.Show(reserve.Count > 0
                ? Text.Get("{=scm_panel_build_or_deploy}Build or deploy")
                : Text.Get("{=scm_panel_available}Available to build"), cards);
            return text;
        }

        /// <summary>Menu text and panel for a deployed or reserve engine.</summary>
        private static string EngineText(SiegeEvent siege, string slotName, SiegeEvent.SiegeEngineConstructionProgress engine, bool deployed)
        {
            var type = engine.SiegeEngine;
            bool unfinished = deployed && engine.Progress < 1f;
            bool noSlot = !deployed && SiegeActions.FreeSlotFor(siege, engine) < 0;

            string text = slotName + ": " + type.Name + "\n" + Status(engine);
            if (unfinished && SelectedFlagged)
                text += " - " + Text.Get("{=scm_will_move}will move to reserve when finished");
            text += "\n" + SiegeInfoOverlay.OneLine(siege, type);
            if (noSlot)
                text += "\n" + Text.Get("{=scm_no_free_slot_long}There is no free slot available for this engine.");

            string actionText;
            Action action;
            if (unfinished)
            {
                actionText = Text.Get("{=scm_click_cancel}Click to cancel construction");
                action = CancelSelected;
            }
            else if (deployed)
            {
                actionText = Text.Get("{=scm_click_reserve}Click to move to reserve");
                action = MoveSelectedToReserve;
            }
            else if (noSlot)
            {
                actionText = Text.Get("{=scm_no_free_slot}No free slot available");
                action = null;
            }
            else
            {
                actionText = Text.Get("{=scm_click_deploy}Click to deploy to a free slot");
                action = DeploySelected;
            }

            bool clickable = Options.PanelClickable;
            var card = SiegeInfoOverlay.Card(siege, type, clickable || noSlot ? actionText : null, clickable ? action : null, engine);
            SiegeInfoOverlay.Show(type.Name.ToString(), new List<SiegeEngineCardVM> { card });
            return text;
        }

        // ---------------------------------------------------------------- options and actions

        private static bool BuildCondition(MenuCallbackArgs args, int b)
        {
            if (!SelectedIsEmptySlot())
                return false;

            var types = SiegeActions.BuildableTypes(_selectedKind == SlotKind.Ranged, Siege, _selectedIndex);
            if (b >= types.Count)
                return false;

            string time = SiegeActions.DaysText(SiegeActions.DaysToBuild(Siege, types[b], 0f));
            MBTextManager.SetTextVariable("SCM_BUILD_" + b, _buildToReserve
                ? Text.Get("{=scm_build_then_reserve}Build, then reserve: {ENGINE} ({TIME})", "ENGINE", types[b].Name, "TIME", time)
                : Text.Get("{=scm_build}Build {ENGINE} ({TIME})", "ENGINE", types[b].Name, "TIME", time));
            args.optionLeaveType = GameMenuOption.LeaveType.Craft;
            return true;
        }

        private static void BuildSelected(int b)
        {
            if (!SelectedIsEmptySlot())
                return;

            bool ranged = _selectedKind == SlotKind.Ranged;
            var types = SiegeActions.BuildableTypes(ranged, Siege, _selectedIndex);
            if (b >= types.Count)
                return;

            var type = types[b];
            SiegeActions.Build(Siege, ranged, _selectedIndex, type);

            // With "send to reserve" the engine stays in the slot while it is built and moves when finished.
            string key = Key(_selectedKind, _selectedIndex);
            _flags.Remove(key);
            if (_buildToReserve)
                _flags.Add(key);

            _note = _buildToReserve
                ? Text.Get("{=scm_started_then_reserve}Started building {ENGINE}; it will move to the reserve when finished.", "ENGINE", type.Name)
                : Text.Get("{=scm_started}Started building {ENGINE}.", "ENGINE", type.Name);
            GameMenu.SwitchToMenu(PlanningMenuId);
        }

        private static bool FromReserveCondition(MenuCallbackArgs args, int r)
        {
            var matching = MatchingReserve();
            if (r >= matching.Count)
                return false;

            MBTextManager.SetTextVariable("SCM_FROMRESERVE_" + r,
                Text.Get("{=scm_deploy_from_reserve}Deploy {ENGINE} from reserve", "ENGINE", matching[r].SiegeEngine.Name));
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return true;
        }

        /// <summary>Deploys a matching reserve engine into the selected empty slot.</summary>
        private static void FromReserveSelected(int r)
        {
            var matching = MatchingReserve();
            if (r >= matching.Count)
                return;

            var engine = matching[r];
            ModLog.Debug(() => "Menu: deploy reserve engine " + engine.SiegeEngine.StringId + " to " + _selectedKind + " slot " + _selectedIndex);
            SetDeployNote(engine, SiegeActions.DeployFromReserve(Siege, engine, _selectedIndex));
            GameMenu.SwitchToMenu(PlanningMenuId);
        }

        /// <summary>Deploys the selected reserve engine into the first free slot it fits.</summary>
        private static void DeploySelected()
        {
            var engine = SelectedEngine(out bool _);
            if (engine != null && Siege != null)
            {
                ModLog.Debug(() => "Menu: deploy reserve engine " + engine.SiegeEngine.StringId + " to a free slot");
                SetDeployNote(engine, SiegeActions.DeployFromReserve(Siege, engine));
            }

            GameMenu.SwitchToMenu(PlanningMenuId);
        }

        private static void SetDeployNote(SiegeEvent.SiegeEngineConstructionProgress engine, bool deployed)
        {
            _note = deployed
                ? Text.Get("{=scm_deployed}Deployed {ENGINE}.", "ENGINE", engine.SiegeEngine.Name)
                : Text.Get("{=scm_no_slot_for}No free slot for {ENGINE}.", "ENGINE", engine.SiegeEngine.Name);
        }

        private static void MoveSelectedToReserve()
        {
            var engine = SelectedEngine(out bool _);
            if (engine != null && Siege != null)
            {
                bool moved = SiegeActions.MoveToReserve(Siege, _selectedKind == SlotKind.Ranged, _selectedIndex);
                _note = moved
                    ? Text.Get("{=scm_moved}{ENGINE} moved to reserve.", "ENGINE", engine.SiegeEngine.Name)
                    : Text.Get("{=scm_move_failed}Could not move {ENGINE} to the reserve.", "ENGINE", engine.SiegeEngine.Name);
            }

            GameMenu.SwitchToMenu(PlanningMenuId);
        }

        private static void CancelSelected()
        {
            if (SelectedUnfinished() && Siege != null)
            {
                var engine = SelectedEngine(out bool _);
                SiegeActions.Cancel(Siege, _selectedKind == SlotKind.Ranged, _selectedIndex);
                _flags.Remove(Key(_selectedKind, _selectedIndex));
                _note = Text.Get("{=scm_cancelled}Cancelled construction of {ENGINE}.", "ENGINE", engine.SiegeEngine.Name);
            }

            GameMenu.SwitchToMenu(PlanningMenuId);
        }

        private static void ToggleSelectedFlag()
        {
            if (!SelectedUnfinished())
                return;

            string key = Key(_selectedKind, _selectedIndex);
            if (!_flags.Remove(key))
                _flags.Add(key);

            GameMenu.SwitchToMenu(SlotMenuId);
        }
    }
}
