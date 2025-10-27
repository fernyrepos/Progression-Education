using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class PlaceWorker_SingleLearningBoard : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            Room room = loc.GetRoom(map);
            if (room is null) return AcceptanceReport.WasAccepted;
            if (room.PsychologicallyOutdoors)
            {
                foreach (var nearby in GenRadial.RadialDistinctThingsAround(loc, map, 10, true))
                {
                    if (thingToIgnore != nearby && nearby.GetRoom() == room)
                    {
                        if (IsLearningBoard(nearby))
                        {
                            return new AcceptanceReport("PE_LearningBoardNearby".Translate());
                        }
                    }
                }
            }
            else
            {
                List<Thing> thingsInRoom = room.ContainedAndAdjacentThings;
                foreach (Thing thingInRoom in thingsInRoom)
                {
                    if (thingInRoom != thingToIgnore)
                    {
                        if (IsLearningBoard(thingInRoom))
                        {
                            return new AcceptanceReport("PE_AlreadyHasLearningBoard".Translate());
                        }
                    }
                }
            }
            return AcceptanceReport.WasAccepted;
        }

        private static bool IsLearningBoard(Thing nearby)
        {
            return nearby.def.HasComp<CompLearningBoard>() || nearby.def.entityDefToBuild is ThingDef thingDef && thingDef.HasComp<CompLearningBoard>();
        }
    }
}
