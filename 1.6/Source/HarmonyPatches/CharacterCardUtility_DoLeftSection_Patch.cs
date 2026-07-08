using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
[HarmonyPatch(typeof(CharacterCardUtility), "DoLeftSection")]
public static class CharacterCardUtility_DoLeftSection_Patch
{
    public static Type sectionType = AccessTools.TypeByName("RimWorld.CharacterCardUtility+LeftRectSection");
    private static readonly FieldInfo numSectionsField = AccessTools.InnerTypes(typeof(CharacterCardUtility))
        .SelectMany(t => AccessTools.GetDeclaredFields(t))
        .FirstOrDefault(f => f.Name == "numSections");

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getCountMethod = AccessTools.Method(typeof(List<>).MakeGenericType(sectionType), "get_Count");
        bool insertedSection = false;

        foreach (var inst in instructions)
        {
            if (inst.StoresField(numSectionsField))
            {
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CharacterCardUtility_DoLeftSection_Patch), nameof(AdjustNumSections)));
            }

            if (!insertedSection && inst.Calls(getCountMethod))
            {
                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CharacterCardUtility_DoLeftSection_Patch), nameof(AddProficienciesSection)));
                insertedSection = true;
            }
            yield return inst;
        }
    }

    public static int AdjustNumSections(int original, Pawn pawn)
    {
        if (EducationMod.settings.enableKnowledgePanel && pawn.CanHaveProficiencies())
        {
            int activeRows = 0;
            if (EducationMod.settings.enableWeaponProficiency) activeRows++;
            if (EducationMod.settings.enableVehicleProficiency && ProficiencyUtility.AreVehicleModsActive) activeRows++;
            if (EducationMod.settings.enableSpeechProficiency) activeRows++;
            if (activeRows > 0)
            {
                return original + 1;
            }
        }
        return original;
    }

    public static void AddProficienciesSection(object listObj, Pawn pawn, Rect leftRect)
    {
        if (!EducationMod.settings.enableKnowledgePanel) return;
        if (pawn.CanHaveProficiencies() is false) return;
        var list = (IList)listObj;
        var hasAbilities = pawn.abilities != null && pawn.abilities.AllAbilitiesForReading.Any(a => a.def.showOnCharacterCard);
        var topGap = hasAbilities ? 6f : 0f;
        float height = 24f + topGap;
        int activeRows = 0;
        if (EducationMod.settings.enableWeaponProficiency) activeRows++;
        if (EducationMod.settings.enableVehicleProficiency && ProficiencyUtility.AreVehicleModsActive) activeRows++;
        if (EducationMod.settings.enableSpeechProficiency) activeRows++;
        if (activeRows == 0) return;
        height += activeRows * 24f;
        var section = Activator.CreateInstance(sectionType);
        AccessTools.Field(sectionType, "rect").SetValue(section, new Rect(0f, 0f, leftRect.width, height));
        AccessTools.Field(sectionType, "drawer").SetValue(section, (Action<Rect>)((Rect r) =>
        {
            var drawingRect = new Rect(r.x, r.y + topGap, r.width, r.height - topGap);
            ProficiencyUtility.DrawKnowledgePanel(drawingRect, pawn);
        }));

        list.Add(section);
    }
}
