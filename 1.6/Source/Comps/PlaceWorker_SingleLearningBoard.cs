using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProgressionEducation
{
    public class PlaceWorker_SingleLearningBoard : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            Room room = loc.GetRoom(map);
            List<Thing> thingsInRoom = room.ContainedAndAdjacentThings;
            foreach (Thing thingInRoom in thingsInRoom)
            {
                if (thingInRoom != thingToIgnore)
                {
                    if (thingInRoom.TryGetComp<CompLearningBoard>() != null)
                    {
                        return new AcceptanceReport("PE_AlreadyHasLearningBoard".Translate());
                    }
                }
            }

            return AcceptanceReport.WasAccepted;
        }
    }
}
