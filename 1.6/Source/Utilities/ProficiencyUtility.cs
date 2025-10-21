using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProgressionEducation
{
    public enum ProficiencyLevel
    {
        LowTech,
        Firearm,
        HighTech
    }

    [HotSwappable]
    public static class ProficiencyUtility
    {
        public static string ToStringHuman(this ProficiencyLevel proficiency)
        {
            return proficiency switch
            {
                ProficiencyLevel.LowTech => (string)"PE_ProficiencyLowTech".Translate(),
                ProficiencyLevel.Firearm => (string)"PE_ProficiencyFirearm".Translate(),
                ProficiencyLevel.HighTech => (string)"PE_ProficiencyHighTech".Translate(),
                _ => "Unknown",
            };
        }

        public static ProficiencyLevel TraitDefToProficiencyLevel(TraitDef traitDef)
        {
            if (traitDef == DefsOf.PE_LowTechProficiency)
            {
                return ProficiencyLevel.LowTech;
            }

            if (traitDef == DefsOf.PE_FirearmProficiency)
            {
                return ProficiencyLevel.Firearm;
            }

            return traitDef == DefsOf.PE_HighTechProficiency ? ProficiencyLevel.HighTech : ProficiencyLevel.LowTech;
        }

        public static TraitDef GetProficiencyForTechLevel(TechLevel techLevel)
        {
            return techLevel switch
            {
                TechLevel.Undefined or TechLevel.Animal or TechLevel.Neolithic or TechLevel.Medieval => DefsOf.PE_LowTechProficiency,
                TechLevel.Industrial => DefsOf.PE_FirearmProficiency,
                TechLevel.Spacer or TechLevel.Ultra or TechLevel.Archotech => DefsOf.PE_HighTechProficiency,
                _ => DefsOf.PE_LowTechProficiency,
            };
        }

        public static void ApplyProficiencyTraitToPawn(Pawn pawn)
        {
            if (pawn?.story?.traits == null || pawn.DevelopmentalStage == DevelopmentalStage.Newborn || pawn.Faction != Faction.OfPlayerSilentFail)
            {
                return;
            }

            if (!EducationSettings.Instance.enableProficiencySystem)
            {
                return;
            }

            bool hasProficiencyTrait = false;
            foreach (var trait in pawn.story.traits.allTraits)
            {
                if (trait.def == DefsOf.PE_LowTechProficiency ||
                    trait.def == DefsOf.PE_FirearmProficiency ||
                    trait.def == DefsOf.PE_HighTechProficiency)
                {
                    hasProficiencyTrait = true;
                    break;
                }
            }
            if (!hasProficiencyTrait)
            {
                var techLevel = pawn.Faction != null ? pawn.Faction.def.techLevel : TechLevel.Undefined;
                var traitToAdd = GetProficiencyForTechLevel(techLevel);
                GrantProficiencyTrait(pawn, traitToAdd);
                EducationLog.Message($"Added trait '{traitToAdd.label}' to pawn {pawn.LabelShort}.");
            }
        }

        public static void GrantProficiencyTrait(Pawn pawn, TraitDef traitToAdd)
        {
            var traits = pawn.story.traits.allTraits.Where(t => t.def == DefsOf.PE_LowTechProficiency || t.def == DefsOf.PE_FirearmProficiency || t.def == DefsOf.PE_HighTechProficiency).ToList();
            foreach (var t in traits)
            {
                pawn.story.traits.allTraits.Remove(t);
            }
            var trait = new Trait(traitToAdd);
            pawn.story.traits.GainTrait(trait);
            pawn.story.traits.allTraits.Remove(trait);
            pawn.story.traits.allTraits.Insert(0, trait);
       }

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

       public static bool CanEquipItem(this Pawn pawn, Thing equipment)
       {
           if (!EducationSettings.Instance.enableProficiencySystem || pawn.Faction != Faction.OfPlayerSilentFail)
           {
               return true;
           }

           if (pawn.story?.traits == null)
           {
               return true;
           }

           var proficiencyRequirement = equipment.def.GetModExtension<ItemProficiencyRequirement>();
           TraitDef requiredProficiency = null;
           if (proficiencyRequirement == null || proficiencyRequirement.requiredProficiency == null)
           {
               var techLevel = GetTechLevelFor(equipment.def);
               requiredProficiency = GetProficiencyForTechLevel(techLevel);
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

               return canEquip;
           }

           return true;
       }

       public static string GetProficiencyLevelString(ThingDef thingDef)
       {
           var techLevel = GetTechLevelFor(thingDef);
           var proficiency = GetProficiencyForTechLevel(techLevel);
           return TraitDefToProficiencyLevel(proficiency).ToStringHuman().ToLower();
       }
   }
}
