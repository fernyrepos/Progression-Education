using RimWorld;
using System.Linq;
using RimWorld.Planet;
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
                    var learningBoard = studyGroup.classroom.LearningBoard.parent;
                    var waypoints = EducationUtility.GetWaypointsInFrontOfBoard(learningBoard, pawn);
                    if (waypoints.Any())
                    {
                        EducationLog.Message($"-> No bell found. Found classroom board. Creating job to go to it.");
                        IntVec3 waypoint = waypoints.RandomElement();
                        Job job = JobMaker.MakeJob(JobDefOf.GotoWander, waypoint);
                        job.globalTarget = new GlobalTargetInfo(waypoint, learningBoard.Map);
                        return job;
                    }
                }
            }
            return null;
        }
    }
}
