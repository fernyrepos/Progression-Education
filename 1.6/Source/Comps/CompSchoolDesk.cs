using Verse;

namespace ProgressionEducation
{
    public class CompProperties_SchoolDesk : CompProperties
    {
        public float speedModifier = 1.0f;

        public CompProperties_SchoolDesk()
        {
            compClass = typeof(CompSchoolDesk);
        }
    }

    public class CompSchoolDesk : ThingComp
    {
        public CompProperties_SchoolDesk Props => (CompProperties_SchoolDesk)props;
    }
}