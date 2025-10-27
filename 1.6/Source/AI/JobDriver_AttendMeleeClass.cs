using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using System.Linq;

namespace ProgressionEducation
{
    [HotSwappable]
    public class JobDriver_AttendMeleeClass : JobDriver_AttendClass
    {
        private int ticksUntilNextAction;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilNextAction, "ticksUntilNextAction", 0);
        }

        protected override Toil MakeLearningToil()
        {
            Toil learningToil = new Toil();

            learningToil.initAction = delegate
            {
                ticksUntilNextAction = Rand.Range(60, 120);
            };

            learningToil.tickAction = delegate
            {
                LordJob_AttendClass lordJob = pawn.GetLord()?.LordJob as LordJob_AttendClass;
                if (lordJob == null || !lordJob.studyGroup.ClassIsActive())
                {
                    return;
                }

                lordJob.studyGroup.subjectLogic.ApplyLearningTick(pawn);

                if (pawn.pather.Moving)
                {
                    return;
                }

                pawn.rotationTracker.FaceCell(TargetA.Cell);

                ticksUntilNextAction--;

                if (ticksUntilNextAction <= 0)
                {
                    if (Rand.Chance(0.8f))
                    {
                        pawn.drawer.Notify_MeleeAttackOn(TargetA.Thing);
                    }
                    else
                    {
                        var validCells = GenAdj.CellsAdjacent8Way(TargetA.Thing).Where(c =>
                            c != pawn.Position &&
                            c.GetEdifice(pawn.Map) == null &&
                            pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly));

                        if (validCells.TryRandomElement(out IntVec3 newCell))
                        {
                            pawn.pather.StartPath(newCell, PathEndMode.OnCell);
                        }
                    }

                    ticksUntilNextAction = Rand.Range(60, 120);
                }
            };

            learningToil.defaultCompleteMode = ToilCompleteMode.Never;
            learningToil.socialMode = RandomSocialMode.Off;
            return learningToil;
        }
    }
}
