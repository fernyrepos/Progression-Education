using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(ResearchManager),
    nameof(ResearchManager.FinishProject))]
public static class ResearchManager_FinishProject_Patch
{
    public static void Postfix(ResearchProjectDef proj)
    {
        if (!EducationSettings.Instance.enableProficiencySystem)
        {
            return;
        }

        var extension = proj.GetModExtension<ResearchGrantsTrait>();
        if (extension != null)
        {
            foreach (var pawn in PawnsFinder
                         .AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
            {
                ProficiencyUtility.GrantProficiencyTrait(pawn, extension.trait,
                    true);
            }

            if (Find.TickManager.TicksGame >= 5000)
            {
                Find.LetterStack.ReceiveLetter(extension.title, extension.desc,
                    LetterDefOf.PositiveEvent);
            }
        }
    }
}