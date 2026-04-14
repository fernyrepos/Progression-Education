using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

public class JobGiver_Teach : ThinkNode_JobGiver
{
    public override Job TryGiveJob(Pawn pawn)
    {
        EducationLog.Message($"JobGiver_Teach.TryGiveJob called for pawn: {pawn.LabelShort}");
        if (!GatheringsUtility.PawnCanStartOrContinueGathering(pawn))
        {
            EducationLog.Message(
                $"-> Pawn {pawn.LabelShort} cannot gather at this time. Returning null.");
            return null;
        }

        if (pawn.GetLord()?.LordJob is not LordJob_AttendClass lordJob)
        {
            EducationLog.Message(
                $"-> Pawn {pawn.LabelShort} is not in a LordJob_AttendClass. Returning null.");
            return null;
        }

        if (lordJob.studyGroup is not StudyGroup studyGroup)
        {
            EducationLog.Message("-> Study group is null. Returning null.");
            return null;
        }

        if (studyGroup.suspended)
        {
            EducationLog.Message(
                $"-> Study group {studyGroup.className} is suspended. Returning null.");
            return null;
        }

        if (studyGroup.classroom?.LearningBoard?.parent is not ThingWithComps learningBoard)
        {
            EducationLog.Message("-> No learning board found in room. Returning null.");
            return null;
        }

        EducationLog.Message(
            $"-> giving job to {pawn.LabelShort} to teach at {learningBoard.LabelCap}.");
        return JobMaker.MakeJob(DefsOf.PE_Teach, learningBoard);
    }
}