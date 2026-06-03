using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(StatWorker), "IsDisabledFor")]
public static class StatWorker_IsDisabledFor_Patch
{
    public static void Postfix(StatWorker __instance, Thing thing, ref bool __result)
    {
        if (!__result && ModsConfig.OdysseyActive && EducationMod.settings.enableProficiencySystem && ProficiencyUtility.IsTrackEnabled(DefsOf.PE_VehicleTrack) && __instance.stat == StatDefOf.PilotingAbility && thing is Pawn p && !ProficiencyUtility.MeetsOrExceedsTier(p, DefsOf.PE_VehicleTrack, DefsOf.PE_OrbitalPilotingTier))
        {
            __result = true;
        }
    }
}
