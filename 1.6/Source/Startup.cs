using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[StaticConstructorOnStartup]
public static class Startup
{
    public static Dictionary<ThingDef, List<ThingDef>> hiddenFacilityPairs = new();

    static Startup()
    {
        var learningBoardDefs = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(d => d.HasComp(typeof(CompLearningBoard)))
            .ToList();
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

        foreach (var deskDef in deskDefs)
        {
            AddAffectedByFacilityComp(deskDef, learningBoardDefs);
        }

        foreach (var boardDef in learningBoardDefs)
        {
            AddFacilityComp(boardDef, deskDefs.ToList());
            AddAffectedByFacilityComp(boardDef, projectorDefs);
        }
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
            var newFacilities = facilities
                .Where(x => !existingComp.linkableFacilities.Contains(x))
                .ToList();
            existingComp.linkableFacilities.AddRange(newFacilities);
            hiddenFacilityPairs[def] = newFacilities;
        }
        else
        {
            var comp = new CompProperties_AffectedByFacilities
            {
                linkableFacilities = facilities,
            };
            hiddenFacilityPairs[def] = facilities;
            def.comps.Add(comp);
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
        }
    }
}
