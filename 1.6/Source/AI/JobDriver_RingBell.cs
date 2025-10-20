using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace ProgressionEducation
{
    public class JobDriver_RingBell : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.mindState.duty.def != DefsOf.PE_RingBellDuty);
            this.FailOn(() =>
            {
                if (pawn.GetLord()?.LordJob is LordJob_AttendClass lordJob)
                {
                    return lordJob.studyGroup.teacher != pawn;
                }
                return true;
            });
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil ringBell = new()
            {
                initAction = delegate
                {
                    var bell = job.targetA.Thing;
                    var bellComp = bell.TryGetComp<CompBell>();
                    bellComp.RingBell();
                },
                defaultDuration = job.GetTarget(TargetIndex.A).Thing.TryGetComp<CompBell>().Props.ticksToRing,
                defaultCompleteMode = ToilCompleteMode.Delay,
                handlingFacing = true
            };
            ringBell.AddFinishAction(delegate
            {
                EducationLog.Message($"Pawn {pawn.LabelShort} finished ringing bell. Sending 'BellRung' memo to lord.");
                pawn.GetLord().ReceiveMemo(LordJob_AttendClass.MemoBellRung);
            });
            yield return ringBell;

            Toil waitAtBell = new()
            {
                initAction = delegate
                {
                    pawn.rotationTracker.FaceTarget(job.targetA.Thing);
                },
                defaultDuration = 600,
                defaultCompleteMode = ToilCompleteMode.Delay,
                handlingFacing = true
            };
            yield return waitAtBell;
        }
    }
}
