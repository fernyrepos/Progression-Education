using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Pawn_Kill_Patch
    {
        public static void Postfix(Pawn __instance)
        {
            foreach (var studyGroup in EducationManager.Instance.studyGroups.ToList())
            {
                if (studyGroup.students.Contains(__instance))
                {
                    studyGroup.RemoveStudent(__instance);
                    EducationLog.Message($"Removed deceased pawn {__instance.LabelShort} from study group {studyGroup.className}.");
                }
                if (studyGroup.teacher == __instance)
                {
                    EducationLog.Message($"Teacher {__instance.LabelShort} of study group {studyGroup.className} has died.");
                    EducationManager.Instance.RemoveStudyGroup(studyGroup);
                }
            }
        }
    }
}
