using HarmonyLib;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(RoomRoleDef), "PostProcessedLabel")]
    public static class RoomRoleDef_PostProcessedLabel_Patch
    {
        public static void Postfix(ref string __result, Room room)
        {
            if (room != null && EducationManager.Instance?.Classrooms != null)
            {
                foreach (var classroom in EducationManager.Instance.Classrooms)
                {
                    if (classroom?.LearningBoard?.parent?.GetRoom() == room)
                    {
                        __result = classroom.name;
                        return;
                    }
                }
            }
        }
    }
}
