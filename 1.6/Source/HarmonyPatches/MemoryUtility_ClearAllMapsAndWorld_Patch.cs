using HarmonyLib;
using Verse;
using Verse.Profile;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
    public static class MemoryUtility_ClearAllMapsAndWorld_Patch
    {
        public static void Prefix()
        {
            CompBell.AllBells.Clear();
        }
    }
}