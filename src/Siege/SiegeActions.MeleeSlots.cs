using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace SiegeCommandMenu
{
    internal static partial class SiegeActions
    {
        /// <summary>Base game limits: 1 battering ram and 2 siege towers deployed at a time.</summary>
        public const int MaxRams = 1;

        public const int MaxTowers = 2;

        /// <summary>The melee slot that holds the battering ram (the middle one of three).</summary>
        public const int RamSlot = 1;

        /// <summary>True for siege towers, false for battering rams (and any other melee engine).</summary>
        public static bool IsTower(SiegeEngineType type)
        {
            if (type == null)
                return false;

            string id = (type.StringId ?? "").ToLowerInvariant();
            if (id.Contains("tower"))
                return true;
            if (id.Contains("ram"))
                return false;

            try
            {
                return Campaign.Current.Models.SiegeEventModel
                    .GetAvailableAttackerTowerSiegeEngines(TaleWorlds.CampaignSystem.Party.PartyBase.MainParty).Contains(type);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Can this engine go into this slot? With the usual three melee slots, the middle one is for
        /// the ram and the outer two for towers; otherwise the 1 ram / 2 tower limits are counted.
        /// slotIndex -1 means "any free slot". Ranged engines are always allowed.
        /// </summary>
        public static bool CanPlaceMelee(SiegeEvent siege, int slotIndex, SiegeEngineType type)
        {
            if (type == null || type.IsRanged)
                return true;

            var slots = DeployedSlots(siege, false);
            if (slots.Count != 3)
                return CanAddMelee(siege, type);

            bool tower = IsTower(type);
            bool Fits(int i) => slots[i] == null && (tower ? i != RamSlot : i == RamSlot);

            if (slotIndex >= 0)
                return slotIndex < slots.Count && Fits(slotIndex);

            return Enumerable.Range(0, slots.Count).Any(Fits);
        }

        private static bool CanAddMelee(SiegeEvent siege, SiegeEngineType type)
        {
            bool tower = IsTower(type);
            int used = DeployedSlots(siege, false).Count(e => e != null && IsTower(e.SiegeEngine) == tower);
            return used < (tower ? MaxTowers : MaxRams);
        }
    }
}
