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
            if (previousLifeStage != null && previousLifeStage.developmentalStage != DevelopmentalStage.Child && pawn.DevelopmentalStage == DevelopmentalStage.Child)
            {
                ProficiencyUtility.ApplyProficiencyTraitToPawn(pawn);
                AddToDaycare(pawn);
            }

            if (previousLifeStage != null && previousLifeStage.developmentalStage == DevelopmentalStage.Child && pawn.DevelopmentalStage != DevelopmentalStage.Child)
            {
                RemoveFromDaycare(pawn);
            }
        }

        public static void AddToDaycare(Pawn pawn)
        {
            if (!pawn.IsFreeNonSlaveColonist)
            {
                return;
            }
            var daycareGroups = EducationManager.Instance.studyGroups.Where(sg => sg.subjectLogic is DaycareClassLogic).ToList();
            if (daycareGroups.Count == 0)
            {
                return;
            }
            var daycareGroup = daycareGroups.OrderBy(sg => sg.students.Count).FirstOrDefault(sg => sg.CanAcceptMoreStudents());
            if (daycareGroup != null)
            {
                daycareGroup.AddStudent(pawn);
                Messages.Message("PE_AddedToDaycareOnAgeUp".Translate(pawn.LabelShort, daycareGroup.className), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }

        public static void RemoveFromDaycare(Pawn pawn)
        {
            var daycareGroups = EducationManager.Instance.studyGroups.Where(sg => sg.subjectLogic is DaycareClassLogic && sg.students.Contains(pawn)).ToList();
            if (daycareGroups.Count == 0)
            {
                return;
            }

            foreach (var daycareGroup in daycareGroups)
            {
                daycareGroup.RemoveStudent(pawn);
                Messages.Message("PE_RemovedFromDaycareOnAgeUp".Translate(pawn.LabelShort, daycareGroup.className), pawn, MessageTypeDefOf.PositiveEvent);
            }
            return;
        }
    }
}
