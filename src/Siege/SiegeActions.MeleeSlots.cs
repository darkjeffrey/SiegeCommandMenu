using System;
using System.Linq;
using SandBox.View.Map.Managers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SiegeCommandMenu
{
    internal static partial class SiegeActions
    {
        /// <summary>
        /// The game's melee slot layout: the first slots are for battering rams (one per ram spot in
        /// the settlement's map scene, normally just slot 0) and the rest are for siege towers. The
        /// campaign map draws a ram in slot i at ram spot i, so a ram in any later slot crashes the
        /// game (index out of range) as soon as it is finished.
        /// </summary>
        private const int DefaultRamSlots = 1;

        private static Settlement _ramSlotsFor;
        private static int _ramSlots = DefaultRamSlots;

        // Wrong-slot engines the repair could not remove, so it does not retry them every frame.
        private static readonly System.Collections.Generic.HashSet<SiegeEvent.SiegeEngineConstructionProgress> _repairFailed =
            new System.Collections.Generic.HashSet<SiegeEvent.SiegeEngineConstructionProgress>();

        /// <summary>How many of the melee slots (counted from 0) take rams. The rest take towers.</summary>
        public static int RamSlotCount(SiegeEvent siege)
        {
            int slotCount = DeployedSlots(siege, false).Count;
            var settlement = siege.BesiegedSettlement;

            if (settlement != _ramSlotsFor)
            {
                int rams = -1;
                try
                {
                    var visual = SettlementVisualManager.Current?.GetSettlementVisual(settlement);
                    var ramFrames = visual?.GetAttackerBatteringRamSiegeEngineFrames();
                    var towerFrames = visual?.GetAttackerTowerSiegeEngineFrames();
                    if (ramFrames != null && towerFrames != null && ramFrames.Length + towerFrames.Length > 0)
                        rams = ramFrames.Length;
                }
                catch (Exception)
                {
                    rams = -1;
                }

                // The map visual is not ready yet: use the usual layout without caching it.
                if (rams < 0)
                    return Math.Min(DefaultRamSlots, slotCount);

                _ramSlotsFor = settlement;
                _ramSlots = rams;
                ModLog.Debug(() => "Melee layout for " + settlement.StringId + ": " + rams + " ram slot(s), "
                    + Math.Max(0, slotCount - rams) + " tower slot(s)");
            }

            return Math.Min(_ramSlots, slotCount);
        }

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

        /// <summary>Does this kind of engine belong in this melee slot (ignoring whether it is free)?</summary>
        private static bool SlotTakes(SiegeEvent siege, int slotIndex, SiegeEngineType type)
        {
            int rams = RamSlotCount(siege);
            return IsTower(type) ? slotIndex >= rams : slotIndex < rams;
        }

        /// <summary>
        /// Can this engine go into this slot? The slot must be free and of the right kind (ram slots
        /// first, tower slots after them). slotIndex -1 means "any free slot". Ranged engines are always allowed.
        /// </summary>
        public static bool CanPlaceMelee(SiegeEvent siege, int slotIndex, SiegeEngineType type)
        {
            if (type == null || type.IsRanged)
                return true;

            var slots = DeployedSlots(siege, false);
            bool Fits(int i) => slots[i] == null && SlotTakes(siege, i, type);

            if (slotIndex >= 0)
                return slotIndex < slots.Count && Fits(slotIndex);

            return Enumerable.Range(0, slots.Count).Any(Fits);
        }

        /// <summary>
        /// Takes engines out of melee slots they do not belong in (left by version 1.0.0 of this mod,
        /// which put the ram in the middle slot). Finished engines go to the reserve, unfinished ones
        /// are cancelled, both through the game's own siege actions. Called every frame, so it runs
        /// before such an engine can finish and crash the map.
        /// </summary>
        public static void RepairMeleeSlots(SiegeEvent siege)
        {
            if (siege == null)
                return;

            try
            {
                var slots = DeployedSlots(siege, false);
                for (int i = 0; i < slots.Count; i++)
                {
                    var engine = slots[i];
                    if (engine == null || _repairFailed.Contains(engine) || SlotTakes(siege, i, engine.SiegeEngine))
                        continue;

                    int index = i;
                    bool finished = engine.Progress >= 1f;
                    string name = engine.SiegeEngine.Name.ToString();
                    ModLog.Debug(() => "Repair: " + engine.SiegeEngine.StringId + " is in the wrong melee slot " + index
                        + " (progress " + engine.Progress.ToString("0.00") + ") | " + Describe(siege));

                    if (finished)
                        MoveToReserve(siege, false, index);
                    else
                        Cancel(siege, false, index);

                    if (DeployedSlots(siege, false)[index] == engine)
                    {
                        _repairFailed.Add(engine);
                        ModLog.Debug(() => "Repair FAILED for melee slot " + index);
                        continue;
                    }

                    InformationManager.DisplayMessage(new InformationMessage(finished
                        ? Text.Get("{=scm_repair_moved}{ENGINE} was in the wrong slot and was moved to the reserve. Deploy it again from Siege Planning.", "ENGINE", name)
                        : Text.Get("{=scm_repair_cancelled}{ENGINE} was being built in the wrong slot and was cancelled. Build it again from Siege Planning.", "ENGINE", name)));
                }
            }
            catch (Exception e)
            {
                ModLog.Error("RepairMeleeSlots", e);
            }
        }
    }
}
