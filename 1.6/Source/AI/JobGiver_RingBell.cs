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
                var classroomRoom = studyGroup.GetRoom();

                if (classroomRoom != null)
                {
                    Thing bell = null;
                    float closestDist = float.MaxValue;
                    foreach (var thing in classroomRoom.ContainedAndAdjacentThings)
                    {
                        var bellComp = thing.TryGetComp<CompBell>();
                        if (bellComp != null && !bellComp.ShouldRingAutomatically)
                        {
                            if (pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some))
                            {
                                float dist = pawn.Position.DistanceTo(thing.Position);
                                if (dist < closestDist)
                                {
                                    closestDist = dist;
                                    bell = thing;
                                }
                            }
                        }
                    }

                    if (bell != null)
                    {
                        EducationLog.Message($"-> Found bell to ring in own classroom: {bell.Label}. Creating job.");
                        return JobMaker.MakeJob(DefsOf.PE_RingBell, bell);
                    }
                    else
                    {
                        var waypoints = ProficiencyUtility.GetWaypointsInFrontOfBoard(studyGroup.classroom.LearningBoard.parent, pawn);
                        if (waypoints.Any())
                        {
                            EducationLog.Message($"-> Found classroom board. Creating job to go to it.");
                            return JobMaker.MakeJob(JobDefOf.GotoWander, waypoints.RandomElement());
                        }
                    }
                }
            }

            return null;
        }
    }
}
