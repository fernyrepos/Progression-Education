using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProgressionEducation
{
    public class ClassCandidatePool : ILordJobCandidatePool
    {
        private readonly List<Pawn> allPawns = [];
        private readonly List<Pawn> nonAssignablePawns = [];

        public ClassCandidatePool(Map map)
        {
            allPawns.AddRange(map.mapPawns.FreeColonistsAndPrisonersSpawned);
        }

        public List<Pawn> AllCandidatePawns => allPawns;

        public List<Pawn> NonAssignablePawns => nonAssignablePawns;

        public void AddPawn(Pawn pawn)
        {
            if (!allPawns.Contains(pawn))
            {
                allPawns.Add(pawn);
            }
        }

        public void RemovePawn(Pawn pawn)
        {
            allPawns.Remove(pawn);
            nonAssignablePawns.Remove(pawn);
        }
    }
}
