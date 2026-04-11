using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.Sound;

namespace ProgressionEducation;

public class CompProperties_Bell : CompProperties
{
    public const int TicksToRing = GenDate.TicksPerHour / 4;
    public SoundDef soundDef;
    public int ticksToRing = TicksToRing;

    public CompProperties_Bell()
    {
        compClass = typeof(CompBell);
    }
}

public class CompBell : ThingComp
{
    public static List<CompBell> AllBells = [];

    public bool IsPowered
    {
        get
        {
            var powerComp = parent.GetComp<CompPowerTrader>();
            return powerComp != null && powerComp.PowerOn;
        }
    }

    public CompProperties_Bell Props => (CompProperties_Bell)props;

    public bool ShouldRingAutomatically => Props.ticksToRing == 0 && IsPowered;

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        AllBells.Remove(this);
        base.PostDeSpawn(map, mode);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!AllBells.Contains(this))
        {
            AllBells.Add(this);
        }
    }

    public void RingBell()
    {
        Props.soundDef.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
    }
}