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
        private static Dictionary<ThingDef, TechLevel> cachedTechLevelValues = new Dictionary<ThingDef, TechLevel>();
        public static TechLevel GetTechLevelFor(ThingDef thingDef)
        {
            if (!cachedTechLevelValues.TryGetValue(thingDef, out TechLevel techLevel))
            {
                cachedTechLevelValues[thingDef] = techLevel = GetTechLevelForInt(thingDef);
            }
            return techLevel;
        }

        private static TechLevel GetTechLevelForInt(ThingDef thingDef)
        {
            List<TechLevel> techLevelSources = new List<TechLevel>();
            if (thingDef.GetCompProperties<CompProperties_Techprint>() != null)
            {
                EducationLog.Message("1 Result: " + thingDef.GetCompProperties<CompProperties_Techprint>().project.techLevel + " - " + thingDef);
                techLevelSources.Add(thingDef.GetCompProperties<CompProperties_Techprint>().project.techLevel);
            }

            if (thingDef.recipeMaker != null)
            {
                if (thingDef.recipeMaker.researchPrerequisite != null)
                {
                    var techLevel = thingDef.recipeMaker.researchPrerequisite.techLevel;
                    if (techLevel != TechLevel.Undefined)
                    {
                        EducationLog.Message("2 Result: " + techLevel + " - " + thingDef);
                        techLevelSources.Add(techLevel);
                    }
                }
                if (thingDef.recipeMaker.researchPrerequisites?.Any() ?? false)
                {
                    var num = thingDef.recipeMaker.researchPrerequisites.MaxBy(x => (int)x.techLevel).techLevel;
                    var techLevel = (TechLevel)num;
                    if (techLevel != TechLevel.Undefined)
                    {
                        EducationLog.Message("3 Result: " + techLevel + " - " + thingDef);
                        techLevelSources.Add(techLevel);
                    }
                }
            }
            if (thingDef.researchPrerequisites?.Any() ?? false)
            {
                var num = thingDef.researchPrerequisites.MaxBy(x => (int)x.techLevel).techLevel;
                var techLevel = (TechLevel)num;
                if (techLevel != TechLevel.Undefined)
                {
                    EducationLog.Message("4 Result: " + techLevel + " - " + thingDef);
                    techLevelSources.Add(techLevel);
                }
            }
            EducationLog.Message("5 Result: " + thingDef.techLevel + " - " + thingDef);
            techLevelSources.Add(thingDef.techLevel);
            EducationLog.Message(thingDef + " - FINAL: " + techLevelSources.MaxBy(x => (int)x));
            return techLevelSources.MaxBy(x => (int)x);
        }

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
                var techLevel = GetTechLevelFor(thing.def);
                requiredProficiency = ProficiencyUtility.GetProficiencyForTechLevel(techLevel);
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
                    cantReason = "PE_CannotEquipItem".Translate(proficiencyLevel.ToStringHuman().ToLower());
                    EducationLog.Message($"EquipmentUtility_CanEquip blocked {pawn.LabelShort} from equipping {thing.LabelCap} due to missing proficiency: {requiredProficiency.label}.");
                }
            }
        }
    }
}
