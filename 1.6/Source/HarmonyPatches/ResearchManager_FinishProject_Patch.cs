using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class ResearchManager_FinishProject_Patch
    {
        public static void Postfix(ResearchProjectDef proj)
        {
            var extension = proj.GetModExtension<ResearchGrantsTrait>();
            if (extension != null)
            {
                foreach (Pawn pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
                {
                    ProficiencyUtility.GrantProficiencyTrait(pawn, extension.trait, true);
                }
                Find.LetterStack.ReceiveLetter(extension.title, extension.desc, LetterDefOf.PositiveEvent);
            }
        }
    }
}
