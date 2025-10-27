using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(LifeStageWorker), nameof(LifeStageWorker.Notify_LifeStageStarted))]
    public static class LifeStageWorker_Notify_LifeStageStarted_Patch
    {
        public static void Postfix(Pawn pawn, LifeStageDef previousLifeStage)
        {
            if (previousLifeStage != null && previousLifeStage.developmentalStage == DevelopmentalStage.Newborn && pawn.DevelopmentalStage == DevelopmentalStage.Child)
            {
                ProficiencyUtility.ApplyProficiencyTraitToPawn(pawn);
            }

            if (previousLifeStage != null && previousLifeStage.developmentalStage == DevelopmentalStage.Child && pawn.DevelopmentalStage != DevelopmentalStage.Child)
            {
                foreach (var studyGroup in EducationManager.Instance.studyGroups.ToList())
                {
                    if (studyGroup.subjectLogic is DaycareClassLogic && studyGroup.students.Contains(pawn))
                    {
                        studyGroup.RemoveStudent(pawn);
                        Messages.Message("PE_RemovedFromDaycareOnAgeUp".Translate(pawn.LabelShort, studyGroup.className), pawn, MessageTypeDefOf.PositiveEvent);
                    }
                }
            }
        }
    }
}
