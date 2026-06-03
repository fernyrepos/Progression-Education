using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(SkillRecord), "TotallyDisabled", MethodType.Getter)]
public static class SkillRecord_TotallyDisabled_Patch
{
    public static void Postfix(SkillRecord __instance, Pawn ___pawn, ref bool __result)
    {
        if (!__result && __instance.def == SkillDefOf.Social)
        {
            if (___pawn.CanHaveProficiencies() && ProficiencyUtility.IsTrackEnabled(DefsOf.PE_SpeechTrack) && ProficiencyUtility.GetCurrentTier(___pawn, DefsOf.PE_SpeechTrack) == DefsOf.PE_MuteTier)
            {
                __result = true;
            }
        }
    }
}
