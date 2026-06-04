using HarmonyLib;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(PawnGenerator), "GeneratePawn",
    typeof(PawnGenerationRequest))]
public static class PawnGenerator_GeneratePawn_Patch
{
    public static void Postfix(Pawn __result, PawnGenerationRequest request)
    {
        if (__result != null)
        {
            var extension = __result.kindDef.GetModExtension<PawnKindProficiencyRequirement>();
            if (extension?.forcedProficiencies != null)
            {
                foreach (var tier in extension.forcedProficiencies)
                {
                    foreach (var track in DefDatabase<ProficiencyDef>.AllDefsListForReading)
                    {
                        if (track.tiers.Contains(tier))
                        {
                            ProficiencyUtility.GrantTier(__result, track, tier);
                            break;
                        }
                    }
                }
            }
            else
            {
                ProficiencyUtility.ApplyProficiencyTraitToPawn(__result);
            }
        }
    }
}
