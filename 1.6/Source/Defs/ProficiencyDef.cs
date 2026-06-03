using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProgressionEducation
{
    public class ProficiencyDef : Def
    {
        public List<ProficiencyTierDef> tiers = new();
    }
}