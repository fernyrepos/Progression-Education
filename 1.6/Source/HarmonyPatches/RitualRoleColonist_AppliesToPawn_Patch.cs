using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(RitualRoleColonist), nameof(RitualRoleColonist.AppliesToPawn))]
public static class RitualRoleColonist_AppliesToPawn_Patch
{
    public static void Postfix(RitualRoleColonist __instance, Pawn p, ref bool __result, ref string reason, TargetInfo selectedTarget, LordJob_Ritual ritual, RitualRoleAssignments assignments, Precept_Ritual precept, bool skipReason)
    {
        if (__result && ModsConfig.OdysseyActive && __instance.usedStat == StatDefOf.PilotingAbility)
        {
            if (EducationMod.settings.enableProficiencySystem
                && ProficiencyUtility.IsTrackEnabled(DefsOf.PE_VehicleTrack)
                && !ProficiencyUtility.MeetsOrExceedsTier(p, DefsOf.PE_VehicleTrack, DefsOf.PE_OrbitalPilotingTier))
            {
                __result = false;
                if (!skipReason)
                {
                    reason = "PE_StudentNotQualifiedProficiency".Translate(p.LabelShort, DefsOf.PE_OrbitalPilotingTier.label);
                }
            }
        }
    }
}
