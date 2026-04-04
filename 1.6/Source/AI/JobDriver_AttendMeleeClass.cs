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
            return new Toil
            {
                initAction = delegate
                {
                    ticksUntilNextAction = Rand.Range(60, 120);
                },

                tickIntervalAction = DoLearningInterval,
                defaultCompleteMode = ToilCompleteMode.Never,
                socialMode = RandomSocialMode.Off
            };
        }

        private void DoLearningInterval(int delta)
        {
            if (pawn.GetLord()?.LordJob is not LordJob_AttendClass lordJob
                || !lordJob.studyGroup.ClassIsActive())
            {
                return;
            }

            lordJob.studyGroup.subjectLogic.ApplyLearningTick(pawn, delta);

            if (pawn.pather.Moving)
            {
                return;
            }

            pawn.rotationTracker.FaceCell(TargetA.Cell);

            ticksUntilNextAction -= delta;

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
        }
    }
}
