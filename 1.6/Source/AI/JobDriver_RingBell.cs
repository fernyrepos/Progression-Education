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
                var compPower = job.targetA.Thing.TryGetComp<CompPowerTrader>();
                if (compPower != null && !compPower.PowerOn)
                {
                    EducationLog.Message($"Pawn {pawn.LabelShort} bell lost power. Sending 'MemoClassCancelled' memo to lord.");
                    pawn.GetLord()?.ReceiveMemo(LordJob_AttendClass.MemoClassCancelled);
                    return true;
                }
                return false;
            });
            this.FailOn(() =>
            {
                var lord = pawn.GetLord();
                if (lord is null || lord.LordJob is not LordJob_AttendClass lordJob)
                {
                    return true;
                }
                return lordJob.studyGroup.teacher != pawn;
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
            };
            ringBell.AddFinishAction(delegate
            {
                EducationLog.Message($"Pawn {pawn.LabelShort} finished ringing bell. Sending 'BellRung' memo to lord.");
                pawn.GetLord()?.ReceiveMemo(LordJob_AttendClass.MemoBellRung);
            });
            yield return ringBell;
            Toil waitAtBell = new()
            {
                initAction = delegate
                {
                    pawn.rotationTracker.FaceTarget(job.targetA.Thing);
                },
                defaultDuration = job.GetTarget(TargetIndex.A).Thing.TryGetComp<CompBell>().Props.ticksToRing,
                defaultCompleteMode = ToilCompleteMode.Delay,
                handlingFacing = true
            };
            yield return waitAtBell;
        }
    }
}
