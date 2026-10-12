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
    internal partial class SiegeMenuBehavior
    {
        // ---------------------------------------------------------------- slot menu

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

        /// <summary>True when the selection is a reserve engine and a free slot it fits exists.</summary>
        private static bool SelectedReserveHasFreeSlot()
        {
            var engine = SelectedEngine(out bool deployed);
            return engine != null && !deployed && Siege != null && SiegeActions.FreeSlotFor(Siege, engine) >= 0;
        }

        /// <summary>True when the selection is an empty deployed slot.</summary>
        private static bool SelectedIsEmptySlot()
        {
            if (_selectedKind == SlotKind.Reserve || Siege == null)
                return false;

            var slots = SiegeActions.DeployedSlots(Siege, _selectedKind == SlotKind.Ranged);
            return _selectedIndex >= 0 && _selectedIndex < slots.Count && slots[_selectedIndex] == null;
        }

        private static void SlotInit(MenuCallbackArgs args)
        {
            args.MenuTitle = new TextObject("Siege Planning");

            if (Siege == null)
            {
                MBTextManager.SetTextVariable("SCM_SLOT_TEXT", "You are not besieging anything.");
                return;
            }

            string slotName = _selectedKind == SlotKind.Reserve
                ? "Reserve"
                : (_selectedKind == SlotKind.Ranged ? "Ranged slot " : "Tower or ram slot ") + (_selectedIndex + 1);

            var engine = SelectedEngine(out bool _);
            string text;

            var siege = Siege;
            var cards = new List<SiegeEngineCardVM>();

            // Clicking a card runs the same action as the matching left-menu option.
            bool clickable = Options.PanelClickable;

            if (engine == null)
            {
                text = slotName + " is empty.\nWhen finished: " + (_buildToReserve ? "send to reserve" : "stay deployed") + "\nAvailable to build:";

                var buildable = SiegeActions.BuildableTypes(_selectedKind == SlotKind.Ranged, Siege, _selectedIndex);
                ModLog.Debug(() => "Build list (hide non-fire " + (Options.HideNonFire ? "on" : "off") + "): "
                    + string.Join(", ", buildable.Select(t => t.StringId)));
                for (int i = 0; i < buildable.Count; i++)
                {
                    int b = i;
                    var type = buildable[i];
                    text += "\n" + type.Name + ": " + SiegeInfoOverlay.OneLine(siege, type);
                    cards.Add(SiegeInfoOverlay.Card(siege, type,
                        clickable ? (_buildToReserve ? "Click to build (then reserve)" : "Click to build") : null,
                        clickable ? (Action)(() => BuildSelected(b)) : null));
                }

                var reserve = MatchingReserve();
                for (int i = 0; i < reserve.Count; i++)
                {
                    int r = i;
                    cards.Add(SiegeInfoOverlay.Card(siege, reserve[i].SiegeEngine,
                        clickable ? "In reserve: click to deploy here" : null,
                        clickable ? (Action)(() => FromReserveSelected(r)) : null,
                        reserve[i]));
                }

                if (_selectedKind == SlotKind.Melee && buildable.Count == 0 && reserve.Count == 0)
                    text += "\nNothing more can be added: the middle slot takes the battering ram, the outer two take siege towers.";

                SiegeInfoOverlay.Show(reserve.Count > 0 ? "Build or deploy" : "Available to build", cards);
            }
            else
            {
                var type = engine.SiegeEngine;
                bool deployed = _selectedKind != SlotKind.Reserve;
                text = slotName + ": " + type.Name + "\n" + Status(engine)
                    + (deployed && engine.Progress < 1f && _flags.Contains(Key(_selectedKind, _selectedIndex)) ? " - will move to reserve when finished" : "") + "\n" + SiegeInfoOverlay.OneLine(siege, type);

                bool unfinished = deployed && engine.Progress < 1f;
                bool noSlot = !deployed && SiegeActions.FreeSlotFor(siege, engine) < 0;
                if (noSlot)
                    text += "\nThere is no free slot available for this engine.";

                string actionText = unfinished ? "Click to cancel construction"
                    : deployed ? "Click to move to reserve"
                    : noSlot ? "No free slot available" : "Click to deploy to a free slot";
                Action action = unfinished ? (Action)CancelSelected
                    : deployed ? (Action)MoveSelectedToReserve
                    : noSlot ? null : (Action)DeploySelected;
                cards.Add(SiegeInfoOverlay.Card(siege, type,
                    clickable || noSlot ? actionText : null,
                    clickable ? action : null,
                    engine));
                SiegeInfoOverlay.Show(type.Name.ToString(), cards);
            }

            MBTextManager.SetTextVariable("SCM_SLOT_TEXT", text);
        }

        /// <summary>Deploys the selected reserve engine into a free slot, then returns to the planning screen.</summary>
        private static void DeploySelected()
        {
            var engine = SelectedEngine(out bool _);
            if (engine != null && Siege != null)
            {
                ModLog.Debug(() => "Menu: deploy reserve engine " + engine.SiegeEngine.StringId + " to a free slot");
                bool ok = SiegeActions.DeployFromReserve(Siege, engine);
                _note = ok ? "Deployed " + engine.SiegeEngine.Name + "." : "No free slot for " + engine.SiegeEngine.Name + ".";
            }

            GameMenu.SwitchToMenu(PlanningMenuId);
        }

        /// <summary>Moves the selected deployed engine to the reserve, then returns to the planning screen.</summary>
        private static void MoveSelectedToReserve()
        {
            var engine = SelectedEngine(out bool _);
            if (engine != null && Siege != null)
            {
                bool moved = SiegeActions.MoveToReserve(Siege, _selectedKind == SlotKind.Ranged, _selectedIndex);
                _note = moved
                    ? engine.SiegeEngine.Name + " moved to reserve."
                    : "Could not move " + engine.SiegeEngine.Name + " to the reserve.";
            }

            GameMenu.SwitchToMenu(PlanningMenuId);
        }

        private static bool BuildCondition(MenuCallbackArgs args, int b)
        {
            if (!SelectedIsEmptySlot())
                return false;

            var types = SiegeActions.BuildableTypes(_selectedKind == SlotKind.Ranged, Siege, _selectedIndex);
            if (b >= types.Count)
                return false;

            MBTextManager.SetTextVariable("SCM_BUILD_" + b,
                (_buildToReserve ? "Build, then reserve: " : "Build ") + types[b].Name + " (" + SiegeActions.DaysText(SiegeActions.DaysToBuild(Siege, types[b], 0f)) + ")");
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

            SiegeActions.Build(Siege, ranged, _selectedIndex, types[b]);
            string key = Key(_selectedKind, _selectedIndex);
            _flags.Remove(key);
            if (_buildToReserve)
            {
                // It stays in the slot while under construction and moves to the reserve when finished.
                _flags.Add(key);
                _note = "Started building " + types[b].Name + "; it will move to the reserve when finished.";
            }
            else
            {
                _note = "Started building " + types[b].Name + ".";
            }
            GameMenu.SwitchToMenu(PlanningMenuId);
        }

        /// <summary>Reserve engines of the right kind that could go into the selected empty slot.</summary>
        private static List<SiegeEvent.SiegeEngineConstructionProgress> MatchingReserve()
        {
            if (!SelectedIsEmptySlot())
                return new List<SiegeEvent.SiegeEngineConstructionProgress>();

            bool ranged = _selectedKind == SlotKind.Ranged;
            return SiegeActions.Reserve(Siege)
                .Where(e => e != null && e.SiegeEngine.IsRanged == ranged
                    && (ranged || SiegeActions.CanPlaceMelee(Siege, _selectedIndex, e.SiegeEngine)))
                .ToList();
        }

        private static bool FromReserveCondition(MenuCallbackArgs args, int r)
        {
            var matching = MatchingReserve();
            if (r >= matching.Count)
                return false;

            MBTextManager.SetTextVariable("SCM_FROMRESERVE_" + r,
                "Deploy " + matching[r].SiegeEngine.Name + " from reserve");
            args.optionLeaveType = GameMenuOption.LeaveType.Continue;
            return true;
        }

        private static void FromReserveSelected(int r)
        {
            var matching = MatchingReserve();
            if (r >= matching.Count)
                return;

            var engine = matching[r];
            ModLog.Debug(() => "Menu: deploy reserve engine " + engine.SiegeEngine.StringId + " to " + _selectedKind + " slot " + _selectedIndex);
            bool ok = SiegeActions.DeployFromReserve(Siege, engine, _selectedIndex);
            _note = ok ? "Deployed " + engine.SiegeEngine.Name + "." : "No free slot for " + engine.SiegeEngine.Name + ".";
            GameMenu.SwitchToMenu(PlanningMenuId);
        }
    }
}
