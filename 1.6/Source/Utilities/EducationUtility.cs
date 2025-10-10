using System.Linq;
using RimWorld;
using Verse;

namespace ProgressionEducation
{
    public static class EducationUtility
    {
        public static bool HasBellOnMap(Map map, bool checkForPower)
        {
            if (map == null)
            {
                return false;
            }
            return CompBell.AllBells.Any(b => b.parent.Map == map && (!checkForPower || !b.Props.isAutomatic || b.IsPowered));
        }

        public static bool IsSchoolDesk(this ThingDef def)
        {
            return def.HasComp(typeof(CompSchoolDesk));
        }
    }
}
