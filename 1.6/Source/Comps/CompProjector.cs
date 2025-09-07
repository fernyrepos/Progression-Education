using Verse;

namespace ProgressionEducation
{
    public class CompProperties_Projector : CompProperties
    {
        public float learningBonus = 0.1f;

        public CompProperties_Projector()
        {
            compClass = typeof(CompProjector);
        }
    }
    public class CompProjector : ThingComp
    {
        public CompProperties_Projector Props => (CompProperties_Projector)props;
    }
}
