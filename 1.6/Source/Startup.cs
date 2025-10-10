using RimWorld;
using System.Linq;
using System.Collections.Generic;
using Verse;

namespace ProgressionEducation
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            EducationLog.Message("Progression: Education - Initializing dynamic facility linking.");

            var learningBoardDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.HasComp(typeof(CompLearningBoard)))
                .ToList();
                
            foreach (var def in learningBoardDefs)
            {
                var count = def.comps.RemoveAll(x => x is CompProperties_Facility);
                EducationLog.Message("Removed " + count + " CompProperties_Facility comps from " + def);
            }
            EducationLog.Message($"Found {learningBoardDefs.Count} learning board defs: {string.Join(", ", learningBoardDefs.Select(d => d.defName))}");

            var deskDefs = new HashSet<ThingDef>();

            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.IsSchoolDesk())
                {
                    deskDefs.Add(def);
                }
            }

            foreach (var skillDef in DefDatabase<SkillDef>.AllDefs)
            {
                var requirement = skillDef.GetModExtension<SkillBuildingRequirement>();
                if (requirement?.requiredBuildings != null)
                {
                    foreach (var buildingDef in requirement.requiredBuildings)
                    {
                        deskDefs.Add(buildingDef);
                    }
                }
            }
            
            var projectorDefs = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(d => d.HasComp(typeof(CompProjector)))
                .ToList();
                
            EducationLog.Message($"Found {deskDefs.Count} desk/workbench defs: {deskDefs.ToStringSafeEnumerable()} and projector defs {projectorDefs.ToStringSafeEnumerable()}");
                
            foreach (var deskDef in deskDefs.Concat(projectorDefs))
            {
                AddAffectedByFacilityComp(deskDef, learningBoardDefs);
            }

            foreach (var boardDef in learningBoardDefs)
            {
                AddFacilityComp(boardDef, deskDefs.ToList());
            }
            
            EducationLog.Message("Progression: Education - Dynamic facility linking complete.");
        }

        private static void AddAffectedByFacilityComp(ThingDef def, List<ThingDef> facilities)
        {
            if (def.comps == null)
            {
                def.comps = new List<CompProperties>();
            }

            var existingComp = def.GetCompProperties<CompProperties_AffectedByFacilities>();
            if (existingComp != null)
            {
                existingComp.linkableFacilities.AddRange(facilities);
                existingComp.linkableFacilities = existingComp.linkableFacilities.Distinct().ToList();
                EducationLog.Message($"Added {facilities.ToStringSafeEnumerable()} learning boards to existing CompAffectedByFacilities on {def.defName}.");
            }
            else
            {
                var comp = new CompProperties_AffectedByFacilities
                {
                    linkableFacilities = facilities
                };
                def.comps.Add(comp);
                EducationLog.Message($"Added new CompAffectedByFacilities with {facilities.ToStringSafeEnumerable()} learning boards to {def.defName}.");
            }
        }

        private static void AddFacilityComp(ThingDef def, List<ThingDef> linkables)
        {
            if (def.comps == null)
            {
                def.comps = new List<CompProperties>();
            }

            var existingComp = def.GetCompProperties<CompProperties_Facility>();
            if (existingComp != null)
            {
                existingComp.linkableBuildings.AddRange(linkables);
                existingComp.linkableBuildings = existingComp.linkableBuildings.Distinct().ToList();
                existingComp.requiresLOS = true;

                EducationLog.Message($"Added {linkables.ToStringSafeEnumerable()} desks/workbenches to existing CompFacility on {def.defName}.");
            }
            else
            {
                var comp = new CompProperties_Facility
                {
                    linkableBuildings = linkables,
                    maxSimultaneous = 1,
                    maxDistance = 100,
                    requiresLOS = true,
                    showMaxSimultaneous = false,
                };
                def.comps.Add(comp);
                EducationLog.Message($"Added new CompFacility with {linkables.ToStringSafeEnumerable()} linkable buildings to {def.defName}.");
            }
        }
    }
}
