using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProgressionEducation;

public class ClassCandidatePool : ILordJobCandidatePool
{
    public ClassCandidatePool(Map map)
    {
        AllCandidatePawns.AddRange(map.mapPawns.FreeColonistsAndPrisonersSpawned);
    }

    public List<Pawn> AllCandidatePawns { get; } = [];

    public List<Pawn> NonAssignablePawns { get; } = [];

    public void AddPawn(Pawn pawn)
    {
        if (!AllCandidatePawns.Contains(pawn))
        {
            AllCandidatePawns.Add(pawn);
        }
    }

    public void RemovePawn(Pawn pawn)
    {
        AllCandidatePawns.Remove(pawn);
        NonAssignablePawns.Remove(pawn);
    }
}