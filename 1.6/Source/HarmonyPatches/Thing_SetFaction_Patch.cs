using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(Thing), nameof(Thing.SetFaction))]
public static class Thing_SetFaction_Patch
{
    public static void Postfix(Thing __instance, Faction newFaction)
    {
        var comp = __instance.TryGetComp<CompLearningBoard>();
        if (comp == null)
        {
            return;
        }
        if (newFaction == Faction.OfPlayer)
        {
            if (comp.classroom == null)
            {
                comp.InitializeClassroom();
            }
        }
        else if (newFaction != Faction.OfPlayer)
        {
            if (comp.classroom != null)
            {
                EducationLog.Message($"Learning board '{__instance.Label}' un-claimed. Removing associated classroom '{comp.classroom.name}'.");
                EducationManager.Instance.RemoveClassroom(comp.classroom);
                comp.classroom = null;
            }
        }
    }
}
