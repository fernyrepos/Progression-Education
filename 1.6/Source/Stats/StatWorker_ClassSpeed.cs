using RimWorld;
using Verse;

namespace ProgressionEducation;

public class StatWorker_ClassSpeed : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        if (!base.ShouldShowFor(req))
        {
            return false;
        }

        if (req.Def is not ThingDef def)
        {
            return false;
        }

        return def.GetCompProperties<CompProperties_LearningBoard>() != null;
    }
}