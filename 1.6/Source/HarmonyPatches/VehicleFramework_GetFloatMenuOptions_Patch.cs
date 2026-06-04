using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch]
[HotSwappable]
public static class VehicleFramework_GetFloatMenuOptions_Patch
{
    public static bool Prepare() => ModsConfig.IsActive("SmashPhil.VehicleFramework");
    public static MethodBase TargetMethod()
    {
        var vehiclePawnType = AccessTools.TypeByName("Vehicles.VehiclePawn");
        return AccessTools.Method(vehiclePawnType, "GetFloatMenuOptions", new[] { typeof(Pawn) });
    }
    public static void Postfix(object __instance, Pawn selPawn, ref IEnumerable<FloatMenuOption> __result)
    {
        if (!EducationMod.settings.enableProficiencySystem || !EducationMod.settings.enableVehicleProficiency)
        {
            return;
        }

        var list = new List<FloatMenuOption>(__result);
        var handlersField = AccessTools.Field(__instance.GetType(), "handlers");
        var handlers = handlersField.GetValue(__instance) as IEnumerable<object>;
        if (handlers == null) return;

        int idx = 0;
        foreach (var handler in handlers)
        {
            if (idx >= list.Count) break;
            var (canOperate, failReason) = ProficiencyUtility.CheckVehicleOperation(selPawn, handler);
            if (!canOperate && !string.IsNullOrEmpty(failReason))
            {
                var opt = list[idx];
                string originalLabel = opt.Label;
                string newLabel = originalLabel.Replace("VF_BoardFailureNonCombatant".Translate(selPawn.LabelShort), failReason);
                opt.Label = newLabel;
            }
            idx++;
        }
        __result = list;
    }
}
