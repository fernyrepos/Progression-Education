using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

[HotSwappable]
public class JobDriver_AttendMeleeClass : JobDriver_AttendClass
{
    private int ticksUntilNextAction;

    private void DoLearningInterval(int delta)
    {
        if (pawn.GetLord()?.LordJob is not LordJob_AttendClass lordJob
            || !lordJob.studyGroup.ClassIsActive())
        {
            return;
        }

        if (pawn.pather.Moving)
        {
            return;
        }

        lordJob.studyGroup.subjectLogic.ApplyLearningTick(pawn, delta);
        pawn.rotationTracker.FaceCell(TargetA.Cell);
        ticksUntilNextAction -= delta;
        if (ticksUntilNextAction > 0)
        {
            return;
        }

        if (Rand.Chance(0.8f))
        {
            pawn.Drawer.Notify_MeleeAttackOn(TargetA.Thing);
        }
        else
        {
            var validCells = GenAdj.CellsAdjacent8Way(TargetA.Thing)
                .Where(c => c != pawn.Position
                            && c.GetEdifice(pawn.Map) == null
                            && pawn.CanReach(c, PathEndMode.OnCell,
                                Danger.Deadly)
                );

            if (validCells.TryRandomElement(out var newCell))
            {
                pawn.pather.StartPath(newCell, PathEndMode.OnCell);
            }
        }

        ticksUntilNextAction = Rand.Range(60, 120);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksUntilNextAction, "ticksUntilNextAction",
            Rand.Range(60, 120));
    }

    public override void InitializeWeapon()
    {
        if (pawn.EquippedWornOrInventoryThings
                .Where(thing =>
                    thing.def.IsMeleeWeapon && EquipmentUtility.CanEquip(thing, pawn))
                .MaxBy(thing =>
                    thing.GetStatValueForPawn(StatDefOf.MeleeWeapon_AverageDPS, pawn))
            is Thing carriedWeapon
           )
        {
            weapon = carriedWeapon;
        }
        else if (TargetA.Thing?.TryGetComp<CompWeaponTrainingBench>()?.WeaponDef is ThingDef
                 weaponDef)
        {
            weapon = ThingMaker.MakeThing(weaponDef,
                GenStuff.DefaultStuffFor(weaponDef));
        }
    }

    protected override Toil MakeLearningToil()
    {
        this.FailOn(() => !GatheringsUtility.PawnCanStartOrContinueGathering(pawn));
        return new Toil
        {
            initAction = () =>
                ticksUntilNextAction = Rand.Range(60, 120),
            tickIntervalAction = DoLearningInterval,
            defaultCompleteMode = ToilCompleteMode.Never,
            socialMode = RandomSocialMode.Off,
        };
    }
}