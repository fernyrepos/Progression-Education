using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

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
            return CompBell.AllBells.Any(b => b.parent.Map == map && (!checkForPower || !b.ShouldRingAutomatically || b.IsPowered));
        }

        public static bool IsSchoolDesk(this ThingDef def)
        {
            return def.HasComp(typeof(CompSchoolDesk));
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
    }
}
