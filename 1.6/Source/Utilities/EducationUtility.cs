using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

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

        public static List<IntVec3> GetWaypointsInFrontOfBoard(Thing board, Pawn pawn)
        {
            var map = pawn.Map;
            var waypoints = new HashSet<IntVec3>();
            foreach (var cell in board.OccupiedRect())
            {
                var forward = cell + board.Rotation.FacingCell;
                waypoints.AddRange(new[] {
                    forward,
                    forward + board.Rotation.RighthandCell,
                    forward - board.Rotation.RighthandCell
                });
            }

            return waypoints.Where(c => c.InBounds(map) && c.GetFirstBuilding(map) == null && c.Walkable(map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly)).ToList();
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
            if (pawn?.story?.traits == null || pawn.DevelopmentalStage == DevelopmentalStage.Newborn)
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
    }
}
