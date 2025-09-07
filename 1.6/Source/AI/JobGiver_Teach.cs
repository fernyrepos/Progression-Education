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
            var room = studyGroup.GetRoom();
            EducationLog.Message($"-> Got room: {room?.ID}");
            Thing learningBoard = null;

            EducationLog.Message($"-> Searching for learning board in room.");
            foreach (var thing in room.ContainedAndAdjacentThings)
            {
                if (thing.TryGetComp<CompLearningBoard>() != null)
                {
                    learningBoard = thing;
                    EducationLog.Message($"-> Found learning board: {learningBoard.Label}");
                    break;
                }
            }

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
