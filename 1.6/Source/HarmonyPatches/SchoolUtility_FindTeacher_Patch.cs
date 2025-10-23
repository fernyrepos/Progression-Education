using HarmonyLib;
using RimWorld;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(SchoolUtility), nameof(SchoolUtility.FindTeacher))]
    public static class SchoolUtility_FindTeacher_Patch
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}
