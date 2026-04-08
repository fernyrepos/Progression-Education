using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

public class JobDriver_RingBell : JobDriver
{
    private bool IsFailOnLordChanged()
    {
        return pawn.GetLord()?.LordJob is not LordJob_AttendClass lordJob
               || lordJob.studyGroup.teacher != pawn;
    }

    private bool IsFailOnPowerLost()
    {
        if (job.targetA.Thing.TryGetComp<CompPowerTrader>()?.PowerOn != false)
        {
            return false;
        }

        EducationLog.Message(
            $"Pawn {pawn.LabelShort} bell lost power. Sending 'MemoClassCancelled' memo to lord.");
        pawn.GetLord()?.ReceiveMemo(LordJob_AttendClass.MemoClassCancelled);
        return true;
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => pawn.mindState.duty.def != DefsOf.PE_RingBellDuty);
        this.FailOn(IsFailOnPowerLost);
        this.FailOn(IsFailOnLordChanged);
        yield return Toils_Reserve.Reserve(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        var ringBell = new Toil
        {
            initAction = () => job.targetA.Thing.TryGetComp<CompBell>()?.RingBell(),
        };
        yield return ringBell;
        var waitAtBell = new Toil
        {
            initAction = () => pawn.rotationTracker.FaceTarget(job.targetA.Thing),
            defaultDuration = job.GetTarget(TargetIndex.A)
                                  .Thing?.TryGetComp<CompBell>()
                                  ?.Props
                                  ?.ticksToRing
                              ?? CompProperties_Bell.TicksToRing,
            defaultCompleteMode = ToilCompleteMode.Delay,
            handlingFacing = true,
        };
        waitAtBell.AddFinishAction(() =>
        {
            EducationLog.Message(
                $"Pawn {pawn.LabelShort} finished ringing bell. Sending 'BellRung' memo to lord.");
            pawn.GetLord()?.ReceiveMemo(LordJob_AttendClass.MemoBellRung);
        });
        yield return waitAtBell;
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }
}