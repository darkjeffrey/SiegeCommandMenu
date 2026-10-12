using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace SiegeCommandMenu
{
    /// <summary>
    /// Every call into the game's siege API lives in the SiegeActions partial classes, so a game
    /// version difference only needs fixing in this folder. This part only reads state.
    /// </summary>
    internal static partial class SiegeActions
    {
        private const System.Reflection.BindingFlags AllMembers = System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

        private static SiegeEvent.SiegeEnginesContainer Engines(SiegeEvent siege)
        {
            return siege.GetSiegeEventSide(BattleSideEnum.Attacker).SiegeEngines;
        }

        public static List<SiegeEvent.SiegeEngineConstructionProgress> DeployedSlots(SiegeEvent siege, bool ranged)
        {
            var engines = Engines(siege);
            IEnumerable<SiegeEvent.SiegeEngineConstructionProgress> slots = ranged
                ? (IEnumerable<SiegeEvent.SiegeEngineConstructionProgress>)engines.DeployedRangedSiegeEngines
                : engines.DeployedMeleeSiegeEngines;
            return slots.ToList();
        }

        public static List<SiegeEvent.SiegeEngineConstructionProgress> Reserve(SiegeEvent siege)
        {
            return Engines(siege).ReservedSiegeEngines.ToList();
        }

        /// <summary>
        /// Engine types the game says the player can build right now (this is what applies the siege
        /// tier). Melee is the ram list plus the siege tower list, minus engines that do not fit the slot.
        /// </summary>
        public static List<SiegeEngineType> BuildableTypes(bool ranged, SiegeEvent siege = null, int slotIndex = -1)
        {
            var model = Campaign.Current.Models.SiegeEventModel;
            var party = PartyBase.MainParty;

            if (ranged)
                return HideNonFire(model.GetAvailableAttackerRangedSiegeEngines(party).ToList());

            var all = model.GetAvailableAttackerRamSiegeEngines(party)
                .Concat(model.GetAvailableAttackerTowerSiegeEngines(party))
                .ToList();

            return siege == null ? all : all.Where(t => CanPlaceMelee(siege, slotIndex, t)).ToList();
        }

        /// <summary>With the option on, drops each plain engine whose fire version ("fire_" + id) is also buildable.</summary>
        private static List<SiegeEngineType> HideNonFire(List<SiegeEngineType> types)
        {
            if (!Options.HideNonFire)
                return types;

            var ids = new HashSet<string>(types.Select(t => t.StringId));
            return types.Where(t => t.StringId.StartsWith("fire_", StringComparison.Ordinal)
                || !ids.Contains("fire_" + t.StringId)).ToList();
        }

        // ---- Build times

        /// <summary>Siege camp (siege preparations) build progress, 0 to 1.</summary>
        public static float CampProgress(SiegeEvent siege)
        {
            var preparations = Engines(siege).SiegePreparations;
            return preparations == null ? 1f : Math.Max(0f, Math.Min(1f, preparations.Progress));
        }

        /// <summary>
        /// In-game days to finish building an engine from the given progress (0 to 1), using the
        /// game's own construction rate. Returns -1 when nothing is built per hour.
        /// </summary>
        public static float DaysToBuild(SiegeEvent siege, SiegeEngineType type, float progress)
        {
            var side = siege.GetSiegeEventSide(BattleSideEnum.Attacker);
            float perHour = Campaign.Current.Models.SiegeEventModel.GetConstructionProgressPerHour(type, siege, side);
            return perHour <= 0f ? -1f : Math.Max(0f, 1f - progress) / (perHour * 24f);
        }

        /// <summary>Days left to finish the siege camp.</summary>
        public static float CampDaysLeft(SiegeEvent siege)
        {
            var preparations = Engines(siege).SiegePreparations;
            if (preparations == null || preparations.Progress >= 1f)
                return 0f;

            return DaysToBuild(siege, preparations.SiegeEngine, preparations.Progress);
        }

        /// <summary>"~5 hours" under a day, otherwise "~2.5 days".</summary>
        public static string DaysText(float days)
        {
            if (days < 0f)
                return "no progress yet";

            if (days < 1f)
            {
                int hours = Math.Max(1, (int)Math.Round(days * 24f));
                return "~" + hours + (hours == 1 ? " hour" : " hours");
            }

            float rounded = (float)Math.Round(days, 1);
            return "~" + rounded.ToString("0.#") + (rounded == 1f ? " day" : " days");
        }

        // ---- Health

        /// <summary>"80% HP", or empty if the engine's health cannot be read.</summary>
        public static string HealthText(SiegeEvent.SiegeEngineConstructionProgress engine)
        {
            if (engine == null)
                return "";

            try
            {
                // Read by name, so it also works if the getters are not public.
                var type = engine.GetType();
                var hpProperty = type.GetProperty("Hitpoints", AllMembers);
                var maxProperty = type.GetProperty("MaxHitPoints", AllMembers);
                if (hpProperty == null || maxProperty == null)
                    return "";

                float hp = Convert.ToSingle(hpProperty.GetValue(engine, null));
                float max = Convert.ToSingle(maxProperty.GetValue(engine, null));
                if (max <= 0f)
                    max = engine.SiegeEngine.BaseHitPoints;
                if (max <= 0f)
                    return "";

                return (hp / max * 100f).ToString("0") + "% HP";
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>One line describing every deployed and reserve engine, for the debug log.</summary>
        public static string Describe(SiegeEvent siege)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                foreach (bool ranged in new[] { true, false })
                {
                    var slots = DeployedSlots(siege, ranged);
                    for (int i = 0; i < slots.Count; i++)
                    {
                        var e = slots[i];
                        sb.Append(ranged ? "R" : "M").Append(i).Append('=')
                          .Append(e == null ? "-" : e.SiegeEngine.StringId + "@" + e.Progress.ToString("0.00"))
                          .Append(' ');
                    }
                }

                sb.Append("reserve=");
                foreach (var e in Reserve(siege))
                    sb.Append(e == null ? "null" : e.SiegeEngine.StringId + "@" + e.Progress.ToString("0.00")).Append(',');
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "describe failed: " + ex.Message;
            }
        }
    }
}
