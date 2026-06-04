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
        if (!EducationMod.settings.enableProficiencySystem)
        {
            return;
        }

        var extension = proj.GetModExtension<ResearchGrantsTrait>();
        if (extension != null)
        {
            if (EducationMod.settings.bestowProficiencyToAll || Find.TickManager.TicksGame < 5000)
            {
                foreach (var pawn in PawnsFinder
                             .AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
                {
                    ProficiencyUtility.GrantProficiencyTrait(pawn, extension.trait);
                }
                if (Find.TickManager.TicksGame >= 5000)
                {
                    Find.LetterStack.ReceiveLetter(extension.title, extension.desc, LetterDefOf.PositiveEvent);
                }
            }
            else
            {
                if (PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction
                    .Any(p => p.IsFreeColonist && !p.WorkTypeIsDisabled(WorkTypeDefOf.Research)))
                {
                    Find.WindowStack.Add(new Dialog_BestowProficiency(extension));
                }
                else
                {
                    foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)
                    {
                        ProficiencyUtility.GrantProficiencyTrait(pawn, extension.trait);
                    }
                }
            }
        }
    }
}
