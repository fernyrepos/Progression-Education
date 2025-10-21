using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation
{
    public class JobGiver_RingBell : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            var lord = pawn.GetLord();
            if (lord?.LordJob is LordJob_AttendClass attendClass)
            {
                var studyGroup = attendClass.studyGroup;
                Thing bell = null;
                float closestDist = float.MaxValue;
                foreach (var bellComp in CompBell.AllBells)
                {
                    if (bellComp.parent.Map == pawn.Map && !bellComp.ShouldRingAutomatically)
                    {
                        var bellThing = bellComp.parent;
                        if (pawn.CanReserveAndReach(bellThing, PathEndMode.Touch, Danger.Some))
                        {
                            float dist = pawn.Position.DistanceTo(bellThing.Position);
                            if (dist < closestDist)
                            {
                                closestDist = dist;
                                bell = bellThing;
                            }
                        }
                    }
                }
                if (bell != null)
                {
                    EducationLog.Message($"-> Found bell to ring: {bell.Label}. Creating job.");
                    return JobMaker.MakeJob(DefsOf.PE_RingBell, bell);
                }
                else
                {
                    var waypoints = EducationUtility.GetWaypointsInFrontOfBoard(studyGroup.classroom.LearningBoard.parent, pawn);
                    if (waypoints.Any())
                    {
                        EducationLog.Message($"-> No bell found. Found classroom board. Creating job to go to it.");
                        return JobMaker.MakeJob(JobDefOf.GotoWander, waypoints.RandomElement());
                    }
                }
            }
            return null;
        }
    }
}
