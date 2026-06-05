using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch]
public static class VehicleFramework_SpecialDisplayStats_Patch
{
    public static bool Prepare() => ModsConfig.IsActive("SmashPhil.VehicleFramework");

    public static MethodBase TargetMethod()
    {
        var vehicleDefType = AccessTools.TypeByName("Vehicles.VehicleDef");
        var vehiclePawnType = AccessTools.TypeByName("Vehicles.VehiclePawn");
        return AccessTools.Method(vehicleDefType, "SpecialDisplayStats", new[] { vehiclePawnType });
    }

    public static object Postfix(object __result, object __instance)
    {
        if (!EducationMod.settings.enableProficiencySystem || !EducationMod.settings.enableVehicleProficiency)
        {
            return __result;
        }

        var statDrawEntryType = AccessTools.TypeByName("Vehicles.VehicleStatDrawEntry");
        var listType = typeof(List<>).MakeGenericType(statDrawEntryType);
        var list = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add");
        if (__result is IEnumerable originalEnumerable)
        {
            foreach (var item in originalEnumerable)
            {
                addMethod.Invoke(list, new[] { item });
            }
        }
        var reqTier = ProficiencyUtility.GetRequiredVehicleTierFromDef(__instance);
        if (reqTier != null)
        {
            var ctor = AccessTools.Constructor(statDrawEntryType, new[] {
                        typeof(StatCategoryDef),
                        typeof(string),
                        typeof(string),
                        typeof(string),
                        typeof(int),
                        typeof(string),
                        typeof(IEnumerable<Dialog_InfoCard.Hyperlink>),
                        typeof(bool)
                    });

            object newEntry = ctor.Invoke(new object[] {
                        StatCategoryDefOf.Basics,
                        "PE_RequiredProficiencyStat".Translate().ToString(),
                        reqTier.label.CapitalizeFirst(),
                        "PE_RequiredProficiencyStatVehicleDesc".Translate().ToString(),
                        5000,
                        null,
                        null,
                        false
                    });

            addMethod.Invoke(list, new[] { newEntry });
        }
        return list;
    }
}
