using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation
{
    public class LordToil_RingBell : LordToil
    {
        private readonly StudyGroup studyGroup;
        private bool bellRung = false;

        public LordToil_RingBell(StudyGroup studyGroup)
        {
            this.studyGroup = studyGroup;
        }

        public override void UpdateAllDuties()
        {
            EducationLog.Message($"LordToil_RingBell.UpdateAllDuties called for class '{studyGroup.className}'");
            TryRingAutomaticBells();
            if (!bellRung)
            {
                studyGroup.teacher.mindState.duty = new PawnDuty(DefsOf.PE_RingBellDuty, studyGroup.teacher.Position);
                EducationLog.Message($"-> Set teacher {studyGroup.teacher.LabelShort} duty to PE_RingBellDuty at position {studyGroup.teacher.Position}");
            }
            else
            {
                EducationLog.Message($"-> Bell already rung, not setting teacher duty");
            }
        }

        public override void LordToilTick()
        {
            base.LordToilTick();
            TryRingAutomaticBells();
        }

        private void TryRingAutomaticBells()
        {
            if (!bellRung && lord.ticksInToil % 60 == 0)
            {
                foreach (var bellComp in CompBell.AllBells)
                {
                    if (bellComp.parent.Map == lord.Map && bellComp.ShouldRingAutomatically)
                    {
                        bellComp.RingBell();
                        bellRung = true;
                        EducationLog.Message($"Automatic bell '{bellComp.parent.Label}' rang for class '{studyGroup.className}'. Sending BellRung memo.");
                        lord.ReceiveMemo("BellRung");
                        return;
                    }
                }
            }
        }
    }
}
