using System;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace SiegeCommandMenu
{
    internal static partial class SiegeActions
    {
        /// <summary>
        /// Runs one of the game's own siege actions (SiegeEvent.DoSiegeAction), the same entry point its
        /// siege screen uses, so the game does its own bookkeeping. Calling the engine container
        /// directly crashed the game, so every change goes through here. The action is found by a part
        /// of its enum name. Returns false if this game version has no such action.
        /// </summary>
        private static bool DoGameAction(SiegeEvent siege, string namePart, SiegeEngineType type, int deploymentIndex, int reserveIndex)
        {
            try
            {
                var method = typeof(SiegeEvent).GetMethod("DoSiegeAction", AllMembers);
                if (method == null || method.GetParameters().Length != 5)
                    return false;

                var actionType = method.GetParameters()[1].ParameterType;
                if (!actionType.IsEnum)
                    return false;

                object action = null;
                foreach (var name in Enum.GetNames(actionType))
                {
                    if (name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    action = Enum.Parse(actionType, name);
                    ModLog.Debug(() => "DoSiegeAction " + name + " type=" + (type == null ? "-" : type.StringId)
                        + " deployment=" + deploymentIndex + " reserve=" + reserveIndex);
                    break;
                }

                if (action == null)
                    return false;

                method.Invoke(siege, new object[] { siege.GetSiegeEventSide(BattleSideEnum.Attacker), action, type, deploymentIndex, reserveIndex });
                return true;
            }
            catch (Exception e)
            {
                ModLog.Error("DoGameAction " + namePart, e);
                return false;
            }
        }

        /// <summary>Starts building a new engine of the given type in an empty slot.</summary>
        public static void Build(SiegeEvent siege, bool ranged, int index, SiegeEngineType type)
        {
            if (!ranged && !CanPlaceMelee(siege, index, type))
                return;

            ModLog.Debug(() => "Build " + type.StringId + " ranged=" + ranged + " slot=" + index + " | before: " + Describe(siege));
            if (!DoGameAction(siege, "Construct", type, index, -1))
            {
                var fallback = new SiegeEvent.SiegeEngineConstructionProgress(type, 0f, type.BaseHitPoints);
                Engines(siege).DeploySiegeEngineAtIndex(fallback, index);
            }

            FinishEnginesIfCheat(siege);
            ModLog.Debug(() => "Build done | after: " + Describe(siege));
        }

        /// <summary>Moves a reserve engine into the first free slot it fits. False if none is free.</summary>
        public static bool DeployFromReserve(SiegeEvent siege, SiegeEvent.SiegeEngineConstructionProgress engine)
        {
            bool ranged = engine.SiegeEngine.IsRanged;
            var slots = DeployedSlots(siege, ranged);

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null && (ranged || CanPlaceMelee(siege, i, engine.SiegeEngine)))
                    return DeployFromReserve(siege, engine, i);
            }

            return false;
        }

        /// <summary>Moves a reserve engine into one specific free slot. False if the slot is taken or does not fit.</summary>
        public static bool DeployFromReserve(SiegeEvent siege, SiegeEvent.SiegeEngineConstructionProgress engine, int slotIndex)
        {
            bool ranged = engine.SiegeEngine.IsRanged;
            var slots = DeployedSlots(siege, ranged);
            if (slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex] != null)
                return false;
            if (!ranged && !CanPlaceMelee(siege, slotIndex, engine.SiegeEngine))
                return false;

            var engines = Engines(siege);
            ModLog.Debug(() => "DeployFromReserve " + engine.SiegeEngine.StringId + " slot=" + slotIndex + " | before: " + Describe(siege));

            int reserveIndex = Reserve(siege).IndexOf(engine);
            if (!(reserveIndex >= 0 && DoGameAction(siege, "FromReserve", engine.SiegeEngine, slotIndex, reserveIndex)))
            {
                engines.DeploySiegeEngineAtIndex(engine, slotIndex);
                if (engines.ReservedSiegeEngines.Contains(engine))
                    engines.ReservedSiegeEngines.Remove(engine);
            }

            ModLog.Debug(() => "DeployFromReserve done | after: " + Describe(siege));
            return true;
        }

        /// <summary>Moves a finished deployed engine to the reserve. Unfinished engines are refused, as in the base game.</summary>
        public static bool MoveToReserve(SiegeEvent siege, bool ranged, int index)
        {
            var engine = DeployedSlots(siege, ranged)[index];
            if (engine == null || engine.Progress < 1f)
                return false;

            ModLog.Debug(() => "MoveToReserve ranged=" + ranged + " slot=" + index + " | before: " + Describe(siege));
            if (!DoGameAction(siege, "ToReserve", engine.SiegeEngine, index, -1))
                return false;

            ModLog.Debug(() => "MoveToReserve done | after: " + Describe(siege));
            return true;
        }

        /// <summary>Cancels construction of an unfinished deployed engine (it is discarded).</summary>
        public static void Cancel(SiegeEvent siege, bool ranged, int index)
        {
            var engine = DeployedSlots(siege, ranged)[index];
            if (engine == null || engine.Progress >= 1f)
                return;

            ModLog.Debug(() => "Cancel ranged=" + ranged + " slot=" + index + " | before: " + Describe(siege));
            if (!DoGameAction(siege, "RemoveDeployed", engine.SiegeEngine, index, -1))
                Engines(siege).RemoveDeployedSiegeEngine(index, ranged, false);
            ModLog.Debug(() => "Cancel done | after: " + Describe(siege));
        }
    }
}
