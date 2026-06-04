using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(TraitSet), "TraitsSorted", MethodType.Getter)]
public static class TraitSet_TraitsSorted_Patch
{
    public static void Postfix(ref List<Trait> __result)
    {
        if (__result == null) return;

        __result = __result.Where(t =>
        {
            if (!EducationMod.settings.enableProficiencySystem && ProficiencyUtility.IsProficiencyTrait(t.def))
            {
                return false;
            }

            if (EducationMod.settings.enableKnowledgePanel && ProficiencyUtility.IsProficiencyTrait(t.def))
            {
                return false;
            }

            foreach (var track in DefDatabase<ProficiencyDef>.AllDefsListForReading)
            {
                if (track.tiers.Any(tier => tier.traitDef == t.def))
                {
                    return ProficiencyUtility.IsTrackEnabled(track);
                }
            }

            return true;
        }).ToList();
    }
}