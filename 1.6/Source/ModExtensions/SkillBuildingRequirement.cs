using System.Collections.Generic;
using Verse;

namespace ProgressionEducation
{
    public class SkillBuildingRequirement : DefModExtension
    {
        public List<ThingDef> requiredBuildings;
        public string requirementLabel;
    }
}