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
        // ---------------------------------------------------------------- planning menu

        private static void PlanningInit(MenuCallbackArgs args)
        {
            args.MenuTitle = new TextObject("Siege Planning");
            SiegeInfoOverlay.Hide();
            _buildToReserve = Options.AutoReserve;
            SiegeActions.FinishEnginesIfCheat(Siege);

            var siege = Siege;
            if (siege == null)
            {
                MBTextManager.SetTextVariable("SCM_PLANNING_TEXT", "You are not besieging anything.");
                return;
            }

            string text = SiegeActions.CampProgress(siege) >= 1f
                ? "Siege camp: complete"
                : "Siege camp: ready in " + SiegeActions.DaysText(SiegeActions.CampDaysLeft(siege));
            text += "\n\n";
            text += string.IsNullOrEmpty(_note) ? "Pick a slot or an engine." : _note;
            _note = "";

            MBTextManager.SetTextVariable("SCM_PLANNING_TEXT", text);
        }

        private static List<PlanRow> Rows(SiegeEvent siege)
        {
            var rows = new List<PlanRow>();
            AddDeployed(rows, siege, SlotKind.Ranged, "Ranged slot ");
            AddDeployed(rows, siege, SlotKind.Melee, "Tower or ram slot ");

            var reserve = SiegeActions.Reserve(siege);
            for (int i = 0; i < reserve.Count; i++)
            {
                if (reserve[i] == null)
                    continue;

                rows.Add(new PlanRow
                {
                    Kind = SlotKind.Reserve,
                    Index = i,
                    Engine = reserve[i],
                    Text = "Reserve: " + reserve[i].SiegeEngine.Name + " (" + Status(reserve[i]) + ")"
                });
            }

            return rows;
        }

        private static void AddDeployed(List<PlanRow> rows, SiegeEvent siege, SlotKind kind, string prefix)
        {
            var slots = SiegeActions.DeployedSlots(siege, kind == SlotKind.Ranged);
            for (int i = 0; i < slots.Count; i++)
            {
                var engine = slots[i];
                string text = prefix + (i + 1) + ": ";
                text += engine == null ? "empty" : engine.SiegeEngine.Name + " (" + Status(engine) + ")";
                rows.Add(new PlanRow { Kind = kind, Index = i, Engine = engine, Text = text });
            }
        }

        private static string Status(SiegeEvent.SiegeEngineConstructionProgress engine)
        {
            string health = SiegeActions.HealthText(engine);
            string status = BuildStatus(engine);
            return health.Length > 0 ? status + ", " + health : status;
        }

        private static string BuildStatus(SiegeEvent.SiegeEngineConstructionProgress engine)
        {
            if (engine.Progress >= 1f)
                return "ready";

            var siege = Siege;
            if (siege == null)
                return "building";

            return "ready in " + SiegeActions.DaysText(SiegeActions.DaysToBuild(siege, engine.SiegeEngine, engine.Progress));
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

        private static void CancelSelected()
        {
            if (SelectedUnfinished() && Siege != null)
            {
                var engine = SelectedEngine(out bool _);
                SiegeActions.Cancel(Siege, _selectedKind == SlotKind.Ranged, _selectedIndex);
                _flags.Remove(Key(_selectedKind, _selectedIndex));
                _note = "Cancelled construction of " + engine.SiegeEngine.Name + ".";
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

        private static bool RowCondition(MenuCallbackArgs args, int row)
        {
            var siege = Siege;
            if (siege == null)
                return false;

            var rows = Rows(siege);
            if (row >= rows.Count)
                return false;

            MBTextManager.SetTextVariable("SCM_ROW_" + row, rows[row].Text);
            args.optionLeaveType = rows[row].Engine == null
                ? GameMenuOption.LeaveType.Craft
                : GameMenuOption.LeaveType.Submenu;
            return true;
        }

        private static void RowSelected(int row)
        {
            var siege = Siege;
            if (siege == null)
                return;

            var rows = Rows(siege);
            if (row >= rows.Count)
                return;

            _selectedKind = rows[row].Kind;
            _selectedIndex = rows[row].Index;
            GameMenu.SwitchToMenu(SlotMenuId);
        }
    }
}
