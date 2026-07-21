using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

public class JobDriver_AttendClass : JobDriver_LessonBase
{
    public static IntVec3 DeskSpotForStudent(Thing desk)
    {
        if (desk.def.multipleInteractionCellOffsets.NullOrEmpty() is false)
        {
            return desk.InteractionCells[0];
        }
        if (desk.def.hasInteractionCell)
        {
            return desk.InteractionCell;
        }
        return desk.Position;
    }

    protected virtual void DoLearningInterval(int delta)
    {
        pawn?.GainComfortFromCellIfPossible(delta, true);
        pawn?.rotationTracker?.FaceTarget(job.GetTarget(TargetIndex.A));
        if (pawn?.GetLord()?.LordJob is not LordJob_AttendClass lordJob)
        {
            EducationLog.Message("-> lord is not LordJob_AttendClass, returning.");
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

    private bool IsAtAssignedLearningPosition()
    {
        if (TargetA.Thing == null)
        {
            return false;
        }

        if (this is JobDriver_AttendMeleeClass)
        {
            return GenAdj.CellsAdjacent8Way(TargetA.Thing)
                .Contains(pawn.Position);
        }

        return pawn.Position == DeskSpotForStudent(TargetA.Thing);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => !GatheringsUtility.PawnCanStartOrContinueGathering(pawn));
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => pawn.mindState.duty.def != DefsOf.PE_AttendClassDuty);
        this.FailOn(IsFailOnLordChanged);
        var deskSpot = DeskSpotForStudent(TargetA.Thing);
        yield return Toils_Goto.GotoCell(deskSpot, PathEndMode.OnCell);
        var learningToil = MakeLearningToil();
        // This toil never completes on its own. If a student is displaced,
        // end it so the lord can issue a fresh job and path them back.
        learningToil.FailOn(() => !pawn.pather.Moving
                                   && !IsAtAssignedLearningPosition());
        yield return learningToil;
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.CanAttendClass())
        {
            return false;
        }

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
