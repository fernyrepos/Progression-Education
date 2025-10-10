using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(JoyGiver), "GetSearchSet", typeof(Pawn), typeof(List<Thing>))]
    public static class JoyGiver_GetSearchSet_Patch
    {
        public static void Postfix(Pawn pawn, List<Thing> outCandidates)
        {
            outCandidates.RemoveAll(thing =>
            {
                if (thing is Building building)
                {
                    return pawn.CanUseDuringActiveClassTime(building) is false;
                }
                return false;
            });
        }
    }
}
