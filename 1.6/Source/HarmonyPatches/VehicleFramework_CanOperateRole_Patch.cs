using System.Reflection;
using HarmonyLib;
using Verse;

namespace ProgressionEducation;
[HotSwappable]
[HarmonyPatch]
public static class VehicleFramework_CanOperateRole_Patch
{
    public static MethodBase targetMethod;
    public static bool Prepare()
    {
        if (ModsConfig.IsActive("SmashPhil.VehicleFramework"))
        {
            var type = AccessTools.TypeByName("Vehicles.VehicleRoleHandler");
            if (type != null)
            {
                targetMethod = AccessTools.Method(type, "CanOperateRole", new[] { typeof(Pawn) });
                return targetMethod != null;
            }
            else
            {
                Log.Error("Vehicles.VehicleRoleHandler or Vehicles.HandlingType not found, disabling proficiency patch");
            }
        }
        return false;
    }

    public static MethodBase TargetMethod() => targetMethod;

    public static void Postfix(object __instance, Pawn pawn, ref bool __result)
    {
        if (!__result || !EducationMod.settings.enableProficiencySystem || !EducationMod.settings.enableVehicleProficiency) return;

        var (canOperate, _) = ProficiencyUtility.CheckVehicleOperation(pawn, __instance);
        __result = canOperate;
    }
}