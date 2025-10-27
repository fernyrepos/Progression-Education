using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation
{
    public class JobGiver_Teach : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            EducationLog.Message($"JobGiver_Teach.TryGiveJob called for pawn: {pawn.LabelShort}");

            var lord = pawn.GetLord();
            if (lord == null || lord.LordJob is not LordJob_AttendClass lordJob)
            {
                EducationLog.Message($"-> Pawn {pawn.LabelShort} is not in a LordJob_AttendClass. Returning null.");
                return null;
            }

            var studyGroup = lordJob.studyGroup;
            if (studyGroup is null)
            {
                EducationLog.Message($"-> Study group {studyGroup?.className} is null. Returning null.");
                return null;
            }
            if (studyGroup?.suspended == true)
            {
                EducationLog.Message($"-> Study group {studyGroup?.className} is suspended. Returning null.");
                return null;
            }

            Thing learningBoard = studyGroup.classroom.LearningBoard.parent;

            if (learningBoard != null)
            {
                EducationLog.Message($"JobGiver_Teach giving job to {pawn.LabelShort} to teach at {learningBoard.Label}.");
                return JobMaker.MakeJob(DefsOf.PE_Teach, learningBoard);
            }
            else
            {
                EducationLog.Message($"-> No learning board found in room. Returning null.");
                return null;
            }
        }
    }
}
