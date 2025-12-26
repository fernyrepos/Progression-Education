using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(BuildFacilityCommandUtility), nameof(BuildFacilityCommandUtility.BuildFacilityCommands))]
    public static class BuildFacilityCommandUtility_BuildFacilityCommands_Patch
    {
        public static IEnumerable<Command> Postfix(IEnumerable<Command> __result, BuildableDef building)
        {
            var hiddenFacilities = new List<ThingDef>();
            if (building is ThingDef thingDef)
            {
                Startup.hiddenFacilityPairs.TryGetValue(thingDef, out hiddenFacilities);
            }
            foreach (var command in __result)
            {
                if (hiddenFacilities != null && command is Designator_Build designator && hiddenFacilities.Contains(designator.PlacingDef as ThingDef))
                {
                    continue;
                }
                else
                {
                    
                    yield return command;
                }
            }
        }
    }
}
