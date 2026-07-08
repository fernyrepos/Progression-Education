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

        int activeRows = 0;
        if (EducationMod.settings.enableWeaponProficiency) activeRows++;
        if (EducationMod.settings.enableVehicleProficiency && ProficiencyUtility.AreVehicleModsActive) activeRows++;
        if (EducationMod.settings.enableSpeechProficiency) activeRows++;
        if (activeRows > 0)
        {
            var hasAbilities = pawn.abilities != null && pawn.abilities.AllAbilitiesForReading.Any(a => a.def.showOnCharacterCard);
            float extraHeight = 24f + (hasAbilities ? 6f : 0f) + activeRows * 24f;
            __result.y += extraHeight;
        }
    }
}
