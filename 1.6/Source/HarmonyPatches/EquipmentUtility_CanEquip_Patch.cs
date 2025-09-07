using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation
{
    [HarmonyPatch(typeof(EquipmentUtility), "CanEquip", [typeof(Thing), typeof(Pawn), typeof(string), typeof(bool)],
    [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal])]
    public static class EquipmentUtility_CanEquip_Patch
    {
        private static void Postfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason, bool checkBonded = true)
        {
            if (!__result)
            {
                return;
            }
            if (!EducationSettings.Instance.enableProficiencySystem)
            {
                return;
            }

            if (pawn.story?.traits == null)
            {
                return;
            }

            var proficiencyRequirement = thing.def.GetModExtension<ItemProficiencyRequirement>();
            TraitDef requiredProficiency = null;
            if (proficiencyRequirement == null || proficiencyRequirement.requiredProficiency == null)
            {
                if (thing.def.researchPrerequisites != null && thing.def.researchPrerequisites.Count > 0)
                {
                    var highestTechLevel = TechLevel.Undefined;
                    foreach (var researchProject in thing.def.researchPrerequisites)
                    {
                        if (researchProject.techLevel > highestTechLevel)
                        {
                            highestTechLevel = researchProject.techLevel;
                        }
                    }
                    requiredProficiency = ProficiencyUtility.GetProficiencyForTechLevel(highestTechLevel);
                }
            }
            else
            {
                requiredProficiency = proficiencyRequirement.requiredProficiency;
            }

            if (requiredProficiency != null)
            {
                bool canEquip = false;
                if (requiredProficiency == DefsOf.PE_HighTechProficiency)
                {
                    canEquip = pawn.story.traits.HasTrait(DefsOf.PE_HighTechProficiency);
                }
                else if (requiredProficiency == DefsOf.PE_FirearmProficiency)
                {
                    canEquip = pawn.story.traits.HasTrait(DefsOf.PE_FirearmProficiency)
                        || pawn.story.traits.HasTrait(DefsOf.PE_HighTechProficiency);
                }
                else if (requiredProficiency == DefsOf.PE_LowTechProficiency)
                {
                    canEquip = pawn.story.traits.HasTrait(DefsOf.PE_LowTechProficiency)
                        || pawn.story.traits.HasTrait(DefsOf.PE_FirearmProficiency)
                        || pawn.story.traits.HasTrait(DefsOf.PE_HighTechProficiency);
                }
                if (canEquip is false)
                {
                    __result = false;
                    var proficiencyLevel = ProficiencyUtility.TraitDefToProficiencyLevel(requiredProficiency);
                    cantReason = "PE_CannotEquipItem".Translate(proficiencyLevel.ToStringHuman());
                    EducationLog.Message($"EquipmentUtility_CanEquip blocked {pawn.LabelShort} from equipping {thing.LabelCap} due to missing proficiency: {requiredProficiency.label}.");
                }
            }
        }
    }
}
