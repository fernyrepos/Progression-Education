using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(PawnRenderUtility))]
    [HarmonyPatch(nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras))]
    public static class PawnRenderUtility_DrawEquipmentAndApparelExtras_Patch
    {
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
        {
            if (pawn.jobs?.curDriver is JobDriver_LessonBase lessonDriver)
            {
                lessonDriver.DrawEquipment(drawPos, facing, flags);
                return false;
            }
            return true;
        }
    }
}