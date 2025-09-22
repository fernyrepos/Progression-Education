using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace ProgressionEducation
{
    public class CompProperties_Bell : CompProperties
    {
        public bool isAutomatic = false;

        public CompProperties_Bell()
        {
            compClass = typeof(CompBell);
        }
    }
    public class CompBell : ThingComp
    {
        public static List<CompBell> AllBells = [];
        public CompProperties_Bell Props => (CompProperties_Bell)props;

        public bool IsPowered
        {
            get
            {
                var powerComp = parent.GetComp<CompPowerTrader>();
                return powerComp != null && powerComp.PowerOn;
            }
        }

        public bool ShouldRingAutomatically => Props.isAutomatic && IsPowered;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!AllBells.Contains(this))
            {
                AllBells.Add(this);
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            AllBells.Remove(this);
            base.PostDeSpawn(map, mode);
        }

        public void RingBell()
        {
            DefsOf.PE_SchoolBellSound.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
        }
    }
}
