using System.Linq;
using HarmonyLib;
using Verse;

namespace ProgressionEducation;

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
                EducationLog.Message(
                    $"Removed deceased pawn {
                        __instance.LabelShort
                    } from study group {
                        studyGroup.className
                    }.");
                if (studyGroup.students.Count < studyGroup.GetStudentRole().MinCount)
                {
                    if (studyGroup.subjectLogic.IsInfinite)
                    {
                        studyGroup.Suspend(true);
                        EducationLog.Message(
                            $"Suspended study group {
                                studyGroup.className
                            } due to insufficient students after death of {
                                __instance.LabelShort
                            }.");
                    }
                    else
                    {
                        EducationManager.Instance.RemoveStudyGroup(studyGroup);
                        EducationLog.Message(
                            $"Removed study group {
                                studyGroup.className
                            } due to insufficient students after death of {
                                __instance.LabelShort
                            }.");
                    }
                }
            }

            if (studyGroup.teacher == __instance)
            {
                EducationLog.Message(
                    $"Teacher {
                        __instance.LabelShort
                    } of study group {
                        studyGroup.className
                    } has died.");
                EducationManager.Instance.RemoveStudyGroup(studyGroup);
            }
        }
    }
}