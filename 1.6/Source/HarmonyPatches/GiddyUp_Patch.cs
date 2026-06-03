using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch]
public static class GiddyUp_Patch
{
    public static MethodBase targetMethod;
    public static bool Prepare()
    {
        if (ModsConfig.IsActive("MemeGoddess.GiddyUp"))
        {
            targetMethod = AccessTools.Method("GiddyUp.Harmony.FloatMenuUtility:AddMountingOptions");
            return targetMethod != null;
        }
        return false;
    }

    public static MethodBase TargetMethod() => targetMethod;

    public static bool Prefix(Pawn animal, Pawn pawn, List<FloatMenuOption> opts)
    {
        if (EducationMod.settings.enableProficiencySystem && ProficiencyUtility.IsTrackEnabled(DefsOf.PE_VehicleTrack))
        {
            if (!ProficiencyUtility.MeetsOrExceedsTier(pawn, DefsOf.PE_VehicleTrack, DefsOf.PE_AnimalRidingTier))
            {
                opts.Add(new FloatMenuOption("PE_GiddyUpNeedRiding".Translate(), null));
                return false;
            }
        }
        return true;
    }
}
