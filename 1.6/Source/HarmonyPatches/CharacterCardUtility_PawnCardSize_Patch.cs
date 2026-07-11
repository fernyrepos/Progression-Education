using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(CharacterCardUtility), "PawnCardSize")]
public static class CharacterCardUtility_PawnCardSize_Patch
{
    public static void Postfix(Pawn pawn, ref Vector2 __result)
    {
        if (!EducationMod.settings.enableKnowledgePanel) return;
        if (pawn.CanHaveProficiencies() is false) return;
        
        int activeRows = DefDatabase<ProficiencyDef>.AllDefsListForReading.Count(ProficiencyUtility.IsTrackEnabled);
        int heightRows = activeRows;
        if (ModsConfig.IsActive("ferny.traumaandintegrity"))
        {
            if (activeRows == 0)
            {
                heightRows = 4;
            }
            else
            {
                heightRows = Mathf.Max(activeRows, 3);
            }
        }
        if (heightRows > 0)
        {
            var hasAbilities = pawn.abilities != null && pawn.abilities.AllAbilitiesForReading.Any(a => a.def.showOnCharacterCard);
            float extraHeight = (activeRows > 0 ? 24f : 0f) + (hasAbilities ? 6f : 0f) + heightRows * 24f;
            __result.y += extraHeight;
        }
    }
}
