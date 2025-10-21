using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(EquipmentUtility), "CanEquip", [typeof(Thing), typeof(Pawn), typeof(string), typeof(bool)],
    [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal])]
    public static class EquipmentUtility_CanEquip_Patch
    {

        public static void Postfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason)
        {
            if (!__result)
            {
                return;
            }

            if (!pawn.CanEquipItem(thing))
            {
                __result = false;
                var proficiencyLevel = ProficiencyUtility.GetProficiencyLevelString(thing.def);
                cantReason = "PE_CannotEquipItem".Translate(proficiencyLevel);
                EducationLog.Message($"EquipmentUtility_CanEquip blocked {pawn.LabelShort} from equipping {thing.LabelCap} due to missing proficiency.");
            }
        }
    }
}
