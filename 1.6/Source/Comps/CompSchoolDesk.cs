using Verse;

namespace ProgressionEducation
{
    public class CompProperties_SchoolDesk : CompProperties
    {
        public CompProperties_SchoolDesk()
        {
            compClass = typeof(CompSchoolDesk);
        }
    }

    public class CompSchoolDesk : ThingComp
    {
    }
}