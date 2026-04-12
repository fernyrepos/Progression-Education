using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

public class JobGiver_RingBell : ThinkNode_JobGiver
{
    public override Job TryGiveJob(Pawn pawn)
    {
        EducationLog.Message($"JobGiver_RingBell.TryGiveJob called for pawn: {pawn.LabelShort}");
        if (!GatheringsUtility.PawnCanStartOrContinueGathering(pawn))
        {
            EducationLog.Message(
                $"-> Pawn {
                    pawn.LabelShort
                } is cannot gather at this time. Returning null.");
            return null;
        }

        if (pawn.GetLord()?.LordJob is not LordJob_AttendClass attendClass)
        {
            EducationLog.Message(
                $"-> Pawn {pawn.LabelShort} is not in a LordJob_AttendClass. Returning null.");
            return null;
        }

        var bell = CompBell.AllBells
            .Where(bc => bc.parent.Map == pawn.Map
                         && !bc.ShouldRingAutomatically
                         && pawn.CanReserveAndReach(bc.parent, PathEndMode.Touch,
                             Danger.Some))
            .Select(bc => bc.parent)
            .OrderBy(b => pawn.Position.DistanceTo(b.Position))
            .FirstOrDefault();
        if (bell != null)
        {
            EducationLog.Message($"-> Found bell to ring: {bell.Label}. Creating job.");
            return JobMaker.MakeJob(DefsOf.PE_RingBell, bell);
        }

        var studyGroup = attendClass.studyGroup;
        var learningBoard = studyGroup.classroom.LearningBoard.parent;
        var waypoints = learningBoard.GetWaypointsInFrontOfBoard(pawn);
        if (waypoints.Any())
        {
            EducationLog.Message(
                "-> No bell found. Found classroom board. Creating job to go to it.");
            var waypoint = waypoints.RandomElement();
            return JobMaker.MakeJob(JobDefOf.GotoWander, waypoint);
        }

        return null;
    }
}