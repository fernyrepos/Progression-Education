using System.Linq;
using HarmonyLib;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
public static class Pawn_DeSpawn_Patch
{
    public static void Postfix(Pawn __instance, DestroyMode mode)
    {
        if (mode != DestroyMode.Vanish)
        {
            return;
        }
        foreach (var studyGroup in EducationManager.Instance.StudyGroups
                     .ToList()
                     .Where(sg => sg.teacher == __instance
                     && sg.teacher.Dead))
        {
            EducationManager.Instance.RemoveStudyGroup(studyGroup);
        }
    }
}