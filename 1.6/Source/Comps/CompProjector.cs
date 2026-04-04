using RimWorld;
using Verse;

namespace ProgressionEducation
{
    public class CompProperties_Projector : CompProperties
    {
        public float learningBonus;

        public CompProperties_Projector()
        {
            compClass = typeof(CompProjector);
        }
    }
    public class CompProjector : ThingComp
    {
        public CompProperties_Projector Props => (CompProperties_Projector)props;

        public bool Active => parent.Spawned && (compPower == null || compPower.PowerOn);

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compPower = parent.GetComp<CompPowerTrader>();
        }
        private CompPowerTrader compPower;
    }
}
