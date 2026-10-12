using System;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace SiegeCommandMenu
{
    /// <summary>Extra output for the debug options. Nothing here runs when the debug options are off.</summary>
    internal static partial class SiegeActions
    {
        /// <summary>The engine's redeployment progress (after it was moved), or -1 if the game has no such value.</summary>
        private static string Redeployment(SiegeEvent.SiegeEngineConstructionProgress engine)
        {
            try
            {
                var property = engine.GetType().GetProperty("RedeploymentProgress", AllMembers);
                return property == null ? "?" : Convert.ToSingle(property.GetValue(engine, null)).ToString("0.00");
            }
            catch (Exception)
            {
                return "?";
            }
        }

        /// <summary>Detailed state of the reserve and slots, one engine per line, for the debug log.</summary>
        public static string DescribeDetailed(SiegeEvent siege)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                foreach (bool ranged in new[] { true, false })
                {
                    var slots = DeployedSlots(siege, ranged);
                    for (int i = 0; i < slots.Count; i++)
                        sb.AppendLine("    " + (ranged ? "ranged " : "melee  ") + i + ": " + DescribeEngine(slots[i]));
                }

                var reserve = Reserve(siege);
                sb.AppendLine("    reserve count: " + reserve.Count);
                for (int i = 0; i < reserve.Count; i++)
                    sb.AppendLine("    reserve " + i + ": " + DescribeEngine(reserve[i]));
                return sb.ToString().TrimEnd();
            }
            catch (Exception e)
            {
                return "describe failed: " + e.Message;
            }
        }

        private static string DescribeEngine(SiegeEvent.SiegeEngineConstructionProgress engine)
        {
            if (engine == null)
                return "empty";

            return engine.SiegeEngine.StringId + " progress=" + engine.Progress.ToString("0.00")
                + " redeploy=" + Redeployment(engine) + " " + HealthText(engine);
        }
    }
}
