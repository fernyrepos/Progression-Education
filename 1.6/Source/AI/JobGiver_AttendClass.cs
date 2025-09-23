using RimWorld;
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
            var room = studyGroup.GetRoom();
            EducationLog.Message($"-> Got room: {room?.ID}");
            Thing desk = null;
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

            EducationLog.Message($"JobGiver_AttendClass for student {pawn.LabelShort}: Searching for a desk.");
            if (studyGroup.subjectLogic is SkillClassLogic skillLogic)
            {
                EducationLog.Message($"-> This is a skill class.");
                var requirement = skillLogic.skillFocus.GetModExtension<SkillBuildingRequirement>();
                if (requirement != null && !requirement.requiredBuildings.NullOrEmpty())
                {
                    EducationLog.Message($"-> Found requirement with {requirement.requiredBuildings.Count} required buildings.");
                    desk = FindUnoccupiedThing(room, pawn, thing => requirement.requiredBuildings.Contains(thing.def));
                }
                else
                {
                    EducationLog.Message($"-> No requirement found or requiredBuildings is empty.");
                }
            }
            else
            {
                EducationLog.Message($"-> This is not a skill class, using default desk finding logic.");
                desk = FindUnoccupiedThing(room, pawn, thing => thing.IsSchoolDesk());
            }

            if (desk != null)
            {
                EducationLog.Message($"-> Found valid desk: {desk.Label}. Giving job.");
                if (learningBoard != null)
                {
                    EducationLog.Message($"-> Found learning board: {learningBoard.Label}. Creating job.");
                    return JobMaker.MakeJob(DefsOf.PE_AttendClass, desk, learningBoard);
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

        private Thing FindUnoccupiedThing(Room room, Pawn pawn, System.Predicate<Thing> thingValidator)
        {
            EducationLog.Message($"-> FindUnoccupiedThing called for pawn {pawn.LabelShort}");
            foreach (var thing in room.ContainedAndAdjacentThings)
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
