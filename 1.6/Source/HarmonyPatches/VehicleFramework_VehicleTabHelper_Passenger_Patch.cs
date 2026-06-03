using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch]
public static class VehicleFramework_VehicleTabHelper_Passenger_Patch
{
    public static bool Prepare() => ModsConfig.IsActive("SmashPhil.VehicleFramework");
    public static MethodBase TargetMethod()
    {
        var helperType = AccessTools.TypeByName("Vehicles.VehicleTabHelper_Passenger");
        return AccessTools.Method(helperType, "HandleDragEvent");
    }
    public static bool Prefix()
    {
        var helperType = AccessTools.TypeByName("Vehicles.VehicleTabHelper_Passenger");
        var draggedPawnField = AccessTools.Field(helperType, "draggedPawn");
        var transferToHolderField = AccessTools.Field(helperType, "transferToHolder");
        var draggedPawn = draggedPawnField.GetValue(null) as Pawn;
        var transferToHolder = transferToHolderField.GetValue(null);

        if (draggedPawn == null || transferToHolder == null) return true;

        var handlerType = AccessTools.TypeByName("Vehicles.VehicleRoleHandler");
        var handler = transferToHolder as object;
        if (handler == null) return true;

        var (canOperate, failReason) = ProficiencyUtility.CheckVehicleOperation(draggedPawn, handler);
        if (!canOperate && !string.IsNullOrEmpty(failReason))
        {
            Messages.Message(failReason, MessageTypeDefOf.RejectInput);
            draggedPawnField.SetValue(null, null);
            transferToHolderField.SetValue(null, null);
            return false;
        }
        return true;
    }
}