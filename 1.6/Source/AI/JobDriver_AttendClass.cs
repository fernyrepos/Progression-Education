using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

public class JobDriver_AttendClass : JobDriver_LessonBase
{
    public static IntVec3 DeskSpotForStudent(Thing desk)
    {
        if (desk.InteractionCells.Any())
        {
            return desk.InteractionCells[0];
        }

        return desk.def.hasInteractionCell
            ? desk.InteractionCell
            : desk.Position;
    }

    private void DoLearningInterval(int delta)
    {
        pawn?.GainComfortFromCellIfPossible(delta, true);
        pawn?.rotationTracker?.FaceTarget(job.GetTarget(TargetIndex.A));
        if (pawn?.GetLord()?.LordJob is not LordJob_AttendClass lordJob)
        {
            EducationLog.Message(
                "-> lord is not LordJob_AttendClass, returning.");
            return;
        }

        if (lordJob.studyGroup.ClassIsActive())
        {
            lordJob.studyGroup.subjectLogic.ApplyLearningTick(pawn, delta);
        }
    }

    public override string GetReport()
    {
        if (pawn.GetLord()?.LordJob is LordJob_AttendClass lordJob
            && !lordJob.studyGroup.ClassIsActive())
        {
            return "PE_JobReport_WaitingForClass".Translate();
        }

        return base.GetReport();
    }

    private bool IsFailOnLordChanged()
    {
        if (pawn.GetLord()?.LordJob is not LordJob_AttendClass lordJob)
        {
            return true;
        }

        if (lordJob.studyGroup?.students?.Contains(pawn) != true)
        {
            lordJob.lord.RemovePawn(pawn);
            return true;
        }

        return false;
    }

    protected virtual Toil MakeLearningToil()
    {
        return new Toil
        {
            tickIntervalAction = DoLearningInterval,
            defaultCompleteMode = ToilCompleteMode.Never,
            socialMode = RandomSocialMode.Off,
            handlingFacing = true,
        };
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => pawn.mindState.duty.def != DefsOf.PE_AttendClassDuty);
        this.FailOn(IsFailOnLordChanged);
        var deskSpot = DeskSpotForStudent(TargetA.Thing);
        yield return Toils_Goto.GotoCell(deskSpot, PathEndMode.OnCell);
        yield return MakeLearningToil();
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        TimeAssignmentUtility.allowUsing = true;
        var reserveThing =
            pawn.Reserve(job.GetTarget(TargetIndex.A), job, 1,
                -1, null, errorOnFailed);
        var reserveSpot =
            pawn.ReserveSittableOrSpot(DeskSpotForStudent(TargetA.Thing), job,
                errorOnFailed);
        var result = reserveThing && reserveSpot;
        TimeAssignmentUtility.allowUsing = false;
        return result;
    }
}