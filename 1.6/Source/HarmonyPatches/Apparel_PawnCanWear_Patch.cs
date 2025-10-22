using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(Apparel), "PawnCanWear")]
    public static class Apparel_PawnCanWear_Patch
    {

        public static void Postfix(ref bool __result, Apparel __instance, Pawn pawn)
        {
            if (!__result)
            {
                return;
            }

            if (!pawn.CanEquipItem(__instance))
            {
                __result = false;
                EducationLog.Message($"EquipmentUtility_CanEquip blocked {pawn.LabelShort} from equipping {__instance.LabelCap} due to missing proficiency.");
            }
        }
    }
}
