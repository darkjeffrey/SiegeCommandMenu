using TaleWorlds.CampaignSystem.Siege;

namespace SiegeCommandMenu
{
    internal enum SlotKind
    {
        Ranged,
        Melee,
        Reserve
    }

    /// <summary>One selectable line in the planning menu: a deployed slot (maybe empty) or a reserve engine.</summary>
    internal sealed class PlanRow
    {
        public SlotKind Kind;
        public int Index;
        public SiegeEvent.SiegeEngineConstructionProgress Engine;
        public string Text;
    }
}
