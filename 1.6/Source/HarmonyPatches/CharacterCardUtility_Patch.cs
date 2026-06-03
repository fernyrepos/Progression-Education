using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HarmonyPatch(typeof(CharacterCardUtility), "DoLeftSection")]
public static class CharacterCardUtility_DoLeftSection_Patch
{
    public static Type sectionType = AccessTools.TypeByName("RimWorld.CharacterCardUtility+LeftRectSection");
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getCountMethod = AccessTools.Method(typeof(List<>).MakeGenericType(sectionType), "get_Count");
        bool inserted = false;

        foreach (var inst in instructions)
        {
            if (!inserted && inst.opcode == OpCodes.Callvirt && inst.operand as MethodInfo == getCountMethod)
            {
                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Ldarg_2);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CharacterCardUtility_DoLeftSection_Patch), nameof(AddProficienciesSection)));
                inserted = true;
            }
            yield return inst;
        }
    }

    public static void AddProficienciesSection(object listObj, Pawn pawn, Rect leftRect)
    {
        if (pawn.CanHaveProficiencies() is false) return;
        var list = (IList)listObj;
        var hasAbilities = pawn.abilities.AllAbilitiesForReading.Any(a => a.def.showOnCharacterCard);
        var topGap = hasAbilities ? 15f : 0f;
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

[HarmonyPatch(typeof(CharacterCardUtility), nameof(CharacterCardUtility.DrawCharacterCard))]
public static class CharacterCardUtility_DrawCharacterCard_Patch
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var inst in instructions)
        {
            if (inst.opcode == OpCodes.Ldc_R4 && (float)inst.operand == 250f)
            {
                inst.operand = 310f;
            }
            yield return inst;
        }
    }
}
