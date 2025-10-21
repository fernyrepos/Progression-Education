using HarmonyLib;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", [typeof(PawnGenerationRequest)])]
    public static class PawnGenerator_GeneratePawn_Patch
    {
        public static void Postfix(Pawn __result)
        {
            ProficiencyUtility.ApplyProficiencyTraitToPawn(__result);
        }
    }
}
