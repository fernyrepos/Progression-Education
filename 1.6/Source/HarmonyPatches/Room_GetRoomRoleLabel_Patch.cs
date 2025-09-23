using HarmonyLib;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(Room), "GetRoomRoleLabel")]
    public static class Room_GetRoomRoleLabel_Patch
    {
        public static void Postfix(ref string __result, Room __instance)
        {
            foreach (var classroom in EducationManager.Instance.Classrooms)
            {
                if (classroom.LearningBoard.parent.GetRoom() == __instance)
                {
                    __result = classroom.name;
                    return;
                }
            }
        }
    }
}
