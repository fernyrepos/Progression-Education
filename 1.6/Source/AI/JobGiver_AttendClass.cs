using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation
{
    public class JobGiver_AttendClass : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            EducationLog.Message($"JobGiver_AttendClass.TryGiveJob called for pawn: {pawn.LabelShort}");

            var lord = pawn.GetLord();
            if (lord == null || lord.LordJob is not LordJob_AttendClass lordJob)
            {
                EducationLog.Message($"-> Pawn {pawn.LabelShort} is not in a LordJob_AttendClass. Returning null.");
                return null;
            }

            var studyGroup = lordJob.studyGroup;
            EducationLog.Message($"-> Got study group: {studyGroup?.className}");
            var learningBoard = studyGroup.classroom.LearningBoard.parent;
            if (learningBoard.TryGetComp<CompFacility>() is not CompFacility facility)
            {
                return null;
            }

            Thing desk = null;

            EducationLog.Message($"JobGiver_AttendClass for student {pawn.LabelShort}: Searching for a desk.");
            if (studyGroup.subjectLogic is SkillClassLogic skillLogic)
            {
                EducationLog.Message($"-> This is a skill class.");
                var requirement = skillLogic.skillFocus.GetModExtension<SkillBuildingRequirement>();
                if (requirement != null && !requirement.requiredBuildings.NullOrEmpty())
                {
                    EducationLog.Message($"-> Found requirement with {requirement.requiredBuildings.Count} required buildings.");
                    desk = FindUnoccupiedThing(facility.LinkedBuildings, pawn, thing => requirement.requiredBuildings.Contains(thing.def));
                }
                else
                {
                    EducationLog.Message($"-> No requirement found or requiredBuildings is empty.");
                }
            }
            else
            {
                EducationLog.Message($"-> This is not a skill class, using default desk finding logic.");
                desk = FindUnoccupiedThing(facility.LinkedBuildings, pawn, thing => thing.IsSchoolDesk());
            }

            if (desk != null)
            {
                EducationLog.Message($"-> Found valid desk: {desk.Label}. Giving job.");
                if (learningBoard != null)
                {
                    EducationLog.Message($"-> Found learning board: {learningBoard.Label}. Creating job.");
                    return JobMaker.MakeJob(studyGroup.subjectLogic.LearningJob, desk, learningBoard);
                }
                else
                {
                    EducationLog.Message($"-> No learning board found. Returning null.");
                    return null;
                }
            }
            else
            {
                EducationLog.Message($"-> No valid, unoccupied desk found. Returning null.");
                return null;
            }
        }

        private Thing FindUnoccupiedThing(List<Thing> things, Pawn pawn, System.Predicate<Thing> thingValidator)
        {
            EducationLog.Message($"-> FindUnoccupiedThing called for pawn {pawn.LabelShort}");
            foreach (var thing in things)
            {
                if (thingValidator(thing))
                {
                    EducationLog.Message($"-> Found thing matching criteria: {thing.Label}");
                    if (pawn.CanReserve(thing) && pawn.CanReserveSittableOrSpot(JobDriver_AttendClass.DeskSpotStudent(thing)))
                    {
                        EducationLog.Message($"-> Pawn can reserve {thing.Label} and its spot. Returning it.");
                        return thing;
                    }
                    else
                    {
                        EducationLog.Message($"-> Pawn cannot reserve {thing.Label} or its spot.");
                    }
                }
            }
            EducationLog.Message($"-> No unoccupied thing found.");
            return null;
        }
    }
}
