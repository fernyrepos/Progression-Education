using HarmonyLib;
using RimWorld;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(ThinkNode_Priority_Learn), nameof(ThinkNode_Priority_Learn.GetPriority))]
    public static class ThinkNode_Priority_Learn_GetPriority_Patch
    {
        public static void Postfix(ref float __result)
        {
            __result = 0f;
        }
    }
}