using System;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace SiegeCommandMenu
{
    /// <summary>The two "Cheats" options: instant siege camp and instant siege engines.</summary>
    internal static partial class SiegeActions
    {
        /// <summary>Cheat option: the siege camp is finished instantly.</summary>
        public static void FinishCampIfCheat(SiegeEvent siege)
        {
            if (siege == null || !Options.CheatInstantCamp)
                return;

            try
            {
                var preparations = Engines(siege).SiegePreparations;
                if (preparations == null || preparations.Progress >= 1f)
                    return;

                ModLog.Debug(() => "Cheat: camp progress forced to 1 (was " + preparations.Progress.ToString("0.00") + ")");
                SetProgressToOne(preparations);
            }
            catch (Exception e)
            {
                ModLog.Error("FinishCampIfCheat", e);
            }
        }

        /// <summary>
        /// Cheat option: engines under construction are finished instantly. Setting their progress
        /// directly crashed the game (an engine's working object is only created by the game's own
        /// construction step), so the game's construction step is run repeatedly instead.
        /// </summary>
        public static void FinishEnginesIfCheat(SiegeEvent siege)
        {
            if (siege == null || !Options.CheatInstantEngines)
                return;

            try
            {
                FinishCampIfCheat(siege);

                var method = typeof(SiegeEvent).GetMethod("ConstructionTick", AllMembers);
                if (method == null || method.GetParameters().Length != 1)
                {
                    ModLog.Debug(() => "Cheat: no ConstructionTick in this game version, engines not finished");
                    return;
                }

                var side = siege.GetSiegeEventSide(BattleSideEnum.Attacker);
                int ticks = 0;
                while (ticks < 1500 && HasUnfinishedEngine(siege))
                {
                    method.Invoke(siege, new object[] { side });
                    ticks++;
                }

                ModLog.Debug(() => "Cheat: ran " + ticks + " construction steps | " + Describe(siege));
            }
            catch (Exception e)
            {
                ModLog.Error("FinishEnginesIfCheat", e);
            }
        }

        private static bool HasUnfinishedEngine(SiegeEvent siege)
        {
            foreach (bool ranged in new[] { true, false })
            {
                foreach (var engine in DeployedSlots(siege, ranged))
                {
                    if (engine != null && engine.Progress < 1f)
                        return true;
                }
            }

            return false;
        }

        /// <summary>Sets a construction progress to done. The setter is not public, so it is found by reflection.</summary>
        private static void SetProgressToOne(object progress)
        {
            var type = progress.GetType();

            var method = type.GetMethod("SetProgress", AllMembers);
            if (method != null && method.GetParameters().Length == 1)
            {
                method.Invoke(progress, new object[] { 1f });
                return;
            }

            var property = type.GetProperty("Progress", AllMembers);
            if (property != null && property.GetSetMethod(true) != null)
            {
                property.SetValue(progress, 1f, null);
                return;
            }

            var field = type.GetField("<Progress>k__BackingField", AllMembers) ?? type.GetField("_progress", AllMembers);
            if (field != null)
                field.SetValue(progress, 1f);
        }
    }
}
