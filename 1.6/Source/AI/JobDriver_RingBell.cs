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
        private Thing bell;

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
            bell = job.targetA.Thing;
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil ringBell = new()
            {
                initAction = delegate
                {
                    SoundDefOf.TinyBell.PlayOneShot(new TargetInfo(bell.Position, bell.Map));
                },
                defaultDuration = 60,
                handlingFacing = true
            };
            ringBell.AddFinishAction(delegate
            {
                EducationLog.Message($"Pawn {pawn.LabelShort} finished ringing bell. Sending 'BellRung' memo to lord.");
                pawn.GetLord().ReceiveMemo(LordJob_AttendClass.MemoBellRung);
            });
            yield return ringBell;
        }
    }
}
