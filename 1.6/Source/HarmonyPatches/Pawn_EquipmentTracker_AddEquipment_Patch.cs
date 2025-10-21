using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(Pawn_EquipmentTracker), "AddEquipment")]
    public static class Pawn_EquipmentTracker_AddEquipment_Patch
    {
        public static void Postfix(Pawn_EquipmentTracker __instance, Thing newEq)
        {
            if (!__instance.pawn.CanEquipItem(newEq))
            {
                __instance.pawn.equipment.Remove((ThingWithComps)newEq);
                if (__instance.pawn.Spawned)
                {
                    GenPlace.TryPlaceThing(newEq, __instance.pawn.Position, __instance.pawn.Map, ThingPlaceMode.Near);
                    if (PawnUtility.ShouldSendNotificationAbout(__instance.pawn))
                    {
                        Messages.Message("PE_TriedToEquipButCouldNotDueToLackOfProficiency".Translate(__instance.pawn.LabelShort, newEq.LabelCap), __instance.pawn, MessageTypeDefOf.RejectInput, false);
                    }
                }
            }

        }
    }
}
