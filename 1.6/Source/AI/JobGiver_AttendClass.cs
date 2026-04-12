using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

public class JobGiver_AttendClass : ThinkNode_JobGiver
{
    private Thing FindUnoccupiedThing(List<Thing> things, Pawn pawn,
        Predicate<Thing> thingValidator)
    {
        EducationLog.Message($"JobGiver_AttendClass.FindUnoccupiedThing called for pawn {
            pawn.LabelShort
        }");
        foreach (var thing in things
                     .Where(thing => thingValidator(thing)))
        {
            EducationLog.Message($"-> Found thing matching criteria: {thing.Label}");
            TimeAssignmentUtility.allowUsing = true;
            if (pawn.CanReserve(thing)
                && pawn.CanReserveSittableOrSpot(
                    JobDriver_AttendClass.DeskSpotForStudent(thing),
                    thing))
            {
                EducationLog.Message(
                    $"-> Pawn can reserve {thing.Label} and its spot. Returning it.");
                TimeAssignmentUtility.allowUsing = false;
                return thing;
            }

            EducationLog.Message($"-> Pawn cannot reserve {thing.Label} or its spot.");
            TimeAssignmentUtility.allowUsing = false;
        }

        EducationLog.Message("-> No unoccupied thing found.");
        return null;
    }

    public override Job TryGiveJob(Pawn pawn)
    {
        EducationLog.Message($"JobGiver_AttendClass.TryGiveJob called for pawn: {pawn.LabelShort}");
        if (!GatheringsUtility.PawnCanStartOrContinueGathering(pawn))
        {
            EducationLog.Message(
                $"-> Pawn {
                    pawn.LabelShort
                } cannot gather at this time. Returning null.");
            return null;
        }

        var lord = pawn.GetLord();
        if (lord?.LordJob is not LordJob_AttendClass lordJob)
        {
            EducationLog.Message(
                $"-> Pawn {pawn.LabelShort} is not in a LordJob_AttendClass. Returning null.");
            return null;
        }

        var studyGroup = lordJob.studyGroup;
        if (studyGroup == null)
        {
            EducationLog.Warning("-> No studyGroup found in LordJob_AttendClass. Returning null.");
            return null;
        }

        if (studyGroup.suspended)
        {
            EducationLog.Message(
                $"-> Study group {studyGroup.className} is suspended. Returning null.");
            return null;
        }

        if (!studyGroup.students.NotNullAndContains(pawn)
            && studyGroup.teacher != pawn)
        {
            EducationLog.Message(
                $"-> {pawn.LabelShort} is neither a student nor teacher. Returning null.");
            return null;
        }

        EducationLog.Message($"-> Got study group: {studyGroup.className}");
        var learningBoard = studyGroup.classroom?.LearningBoard?.parent;
        if (learningBoard?.TryGetComp<CompFacility>() is not CompFacility facility)
        {
            EducationLog.Message("-> No learning board found. Returning null.");
            return null;
        }

        EducationLog.Message($"-> Searching for a desk for student {pawn.LabelShort}.");
        var validBenches = studyGroup.subjectLogic.GetValidLearningBenches();
        if (FindUnoccupiedThing(
                facility.LinkedBuildings,
                pawn,
                thing => validBenches.Contains(thing.def))
            is Thing desk)
        {
            EducationLog.Message($"-> Found valid desk: {desk.LabelCap}. Giving job.");
            return JobMaker.MakeJob(studyGroup.subjectLogic.LearningJob, desk,
                learningBoard);
        }

        EducationLog.Message("-> No desk found. Returning null.");
        return null;
    }
}