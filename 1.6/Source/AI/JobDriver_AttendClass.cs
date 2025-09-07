using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation
{
    public class JobDriver_AttendClass : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1, -1, null, errorOnFailed) && pawn.ReserveSittableOrSpot(DeskSpotStudent(TargetA.Thing), job, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => pawn.mindState.duty.def != DefsOf.PE_AttendClassDuty);
            this.FailOn(() =>
            {
                if (pawn.GetLord()?.LordJob is LordJob_AttendClass lordJob)
                {
                    return !lordJob.studyGroup.students.Contains(pawn);
                }
                return true;
            });
            yield return Toils_Goto.GotoCell(DeskSpotStudent(TargetA.Thing), PathEndMode.OnCell);
            Toil waitToil = new()
            {
                tickAction = delegate
                {
                    pawn.rotationTracker.FaceTarget(job.GetTarget(TargetIndex.A));
                    PawnUtility.GainComfortFromCellIfPossible(pawn, 1);
                    var lordJob = (LordJob_AttendClass)pawn.GetLord().LordJob;
                    var studyGroup = lordJob.studyGroup;
                    if (studyGroup.AllStudentsAreGathered())
                    {
                        studyGroup.subjectLogic.ApplyLearningTick(pawn);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never,
                socialMode = RandomSocialMode.Off,
                handlingFacing = true
            };
            yield return waitToil;
        }
        public static IntVec3 DeskSpotStudent(Thing desk)
        {
            if (desk.InteractionCells.Any())
            {
                return desk.InteractionCells[0];
            }
            if (desk.def.hasInteractionCell)
            {
                return desk.InteractionCell;
            }
            return desk.Position;
        }

    }
}
