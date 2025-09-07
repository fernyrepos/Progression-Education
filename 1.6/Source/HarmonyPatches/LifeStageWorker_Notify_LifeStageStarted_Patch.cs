using HarmonyLib;
using RimWorld;
using Verse;

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
        }
    }
}
