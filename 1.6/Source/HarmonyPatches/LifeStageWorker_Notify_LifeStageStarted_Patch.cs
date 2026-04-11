using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(LifeStageWorker),
    nameof(LifeStageWorker.Notify_LifeStageStarted))]
public static class LifeStageWorker_Notify_LifeStageStarted_Patch
{
    public static void AddToDaycare(Pawn pawn)
    {
        if (!pawn.IsFreeNonSlaveColonist)
        {
            return;
        }

        var daycareGroups = EducationManager.Instance.studyGroups
            .Where(sg => sg.subjectLogic is DaycareClassLogic
            && (sg.classroom?.addKids ?? false))
            .ToList();
        if (daycareGroups.Count == 0)
        {
            return;
        }

        var daycareGroup = daycareGroups
            .OrderBy(sg => sg.students.Count)
            .FirstOrDefault(sg => sg.CanAcceptMoreStudents());
        if (daycareGroup == null)
        {
            return;
        }

        daycareGroup.AddStudent(pawn);
        Messages.Message(
            "PE_AddedToDaycareOnAgeUp".Translate(pawn.LabelShort,
                daycareGroup.className), pawn,
            MessageTypeDefOf.PositiveEvent);
    }

    public static void Postfix(Pawn pawn, LifeStageDef previousLifeStage)
    {
        if (previousLifeStage != null
            && previousLifeStage.developmentalStage != DevelopmentalStage.Child
            && pawn.DevelopmentalStage == DevelopmentalStage.Child)
        {
            ProficiencyUtility.ApplyProficiencyTraitToPawn(pawn);
            AddToDaycare(pawn);
        }

        if (previousLifeStage?.developmentalStage == DevelopmentalStage.Child
            && pawn.DevelopmentalStage != DevelopmentalStage.Child)
        {
            RemoveFromDaycare(pawn);
        }
    }

    public static void RemoveFromDaycare(Pawn pawn)
    {
        foreach (var daycareGroup in EducationManager.Instance.studyGroups
                     .Where(sg => sg.subjectLogic is DaycareClassLogic
                                  && sg.students.Contains(pawn)))
        {
            daycareGroup.RemoveStudent(pawn);
            Messages.Message(
                "PE_RemovedFromDaycareOnAgeUp".Translate(pawn.LabelShort,
                    daycareGroup.className),
                pawn,
                MessageTypeDefOf.PositiveEvent);
        }
    }
}