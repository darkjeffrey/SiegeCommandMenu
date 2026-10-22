using System.Collections.Generic;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Localization;

namespace SiegeCommandMenu
{
    /// <summary>The planning menu: camp progress plus one option per slot and reserve engine.</summary>
    internal partial class SiegeMenuBehavior
    {
        private static TextObject MenuTitle => new TextObject("{=scm_title}Siege Planning");

        private static string NotBesieging => Text.Get("{=scm_not_besieging}You are not besieging anything.");

        private static void PlanningInit(MenuCallbackArgs args)
        {
            args.MenuTitle = MenuTitle;
            SiegeInfoOverlay.Hide();
            _buildToReserve = Options.AutoReserve;
            SiegeActions.FinishEnginesIfCheat(Siege);

            var siege = Siege;
            if (siege == null)
            {
                MBTextManager.SetTextVariable("SCM_PLANNING_TEXT", NotBesieging);
                return;
            }

            string text = SiegeActions.CampProgress(siege) >= 1f
                ? Text.Get("{=scm_camp_done}Siege camp: complete")
                : Text.Get("{=scm_camp_ready_in}Siege camp: ready in {TIME}", "TIME", SiegeActions.DaysText(SiegeActions.CampDaysLeft(siege)));
            text += "\n\n" + (string.IsNullOrEmpty(_note) ? Text.Get("{=scm_pick}Pick a slot or an engine.") : _note);
            _note = "";

            MBTextManager.SetTextVariable("SCM_PLANNING_TEXT", text);
        }

        private static string SlotName(SlotKind kind, int index)
        {
            switch (kind)
            {
                case SlotKind.Ranged:
                    return Text.Get("{=scm_slot_ranged}Ranged slot {N}", "N", index + 1);
                case SlotKind.Melee:
                    return Text.Get("{=scm_slot_melee}Tower or ram slot {N}", "N", index + 1);
                default:
                    return Text.Get("{=scm_slot_reserve}Reserve");
            }
        }

        private static List<PlanRow> Rows(SiegeEvent siege)
        {
            var rows = new List<PlanRow>();
            AddDeployed(rows, siege, SlotKind.Ranged);
            AddDeployed(rows, siege, SlotKind.Melee);

            var reserve = SiegeActions.Reserve(siege);
            for (int i = 0; i < reserve.Count; i++)
            {
                if (reserve[i] != null)
                    rows.Add(new PlanRow { Kind = SlotKind.Reserve, Index = i, Engine = reserve[i], Text = RowText(SlotKind.Reserve, i, reserve[i]) });
            }

            return rows;
        }

        private static void AddDeployed(List<PlanRow> rows, SiegeEvent siege, SlotKind kind)
        {
            var slots = SiegeActions.DeployedSlots(siege, kind == SlotKind.Ranged);
            for (int i = 0; i < slots.Count; i++)
                rows.Add(new PlanRow { Kind = kind, Index = i, Engine = slots[i], Text = RowText(kind, i, slots[i]) });
        }

        private static string RowText(SlotKind kind, int index, SiegeEvent.SiegeEngineConstructionProgress engine)
        {
            return engine == null
                ? Text.Get("{=scm_row_empty}{SLOT}: empty", "SLOT", SlotName(kind, index))
                : Text.Get("{=scm_row_engine}{SLOT}: {ENGINE} ({STATUS})",
                    "SLOT", SlotName(kind, index), "ENGINE", engine.SiegeEngine.Name, "STATUS", Status(engine));
        }

        /// <summary>"ready in ~5 hours, 80% HP".</summary>
        private static string Status(SiegeEvent.SiegeEngineConstructionProgress engine)
        {
            string health = SiegeActions.HealthText(engine);
            string status = BuildStatus(engine);
            return health.Length > 0 ? status + ", " + health : status;
        }

        private static string BuildStatus(SiegeEvent.SiegeEngineConstructionProgress engine)
        {
            if (engine.Progress >= 1f)
                return Text.Get("{=scm_ready}ready");

            var siege = Siege;
            if (siege == null)
                return Text.Get("{=scm_building}building");

            return Text.Get("{=scm_ready_in}ready in {TIME}",
                "TIME", SiegeActions.DaysText(SiegeActions.DaysToBuild(siege, engine.SiegeEngine, engine.Progress)));
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
