using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ProgressionEducation;

public static class EducationUtility
{
    public static List<ClassSubjectLogic> GetAvailableSubjectLogics(StudyGroup studyGroup)
    {
        var logics = new List<ClassSubjectLogic>();
        foreach (var type in typeof(ClassSubjectLogic).AllSubclasses())
        {
            if (type.IsAbstract) continue;
            var logic = (ClassSubjectLogic)Activator.CreateInstance(type, [studyGroup]);
            if (logic.IsEnabled)
            {
                logics.Add(logic);
            }
        }
        return logics;
    }

    public static bool CanAttendClass(this Pawn pawn)
    {
        return pawn is
        {
            Spawned: true,
            DeadOrDowned: false,
            InMentalState: false,
            Drafted: false,
            Deathresting: false,
            DevelopmentalStage: >= DevelopmentalStage.Child,
        };
    }


    public static List<IntVec3> GetWaypointsInFrontOfBoard(this Thing board, Pawn pawn)
    {
        var map = board.Map;
        return board.OccupiedRect()
            .Select(cell => cell + board.Rotation.FacingCell)
            .SelectMany(forward => new[]
            {
                forward,
                forward + board.Rotation.RighthandCell,
                forward - board.Rotation.RighthandCell,
            })
            .Where(c => c.InBounds(map)
                        && c.GetFirstBuilding(map) == null
                        && c.Walkable(map)
                        && pawn.CanReach(c, PathEndMode.OnCell,
                            Danger.Deadly))
            .ToList();
    }

    public static bool HasBellOnMap(Map map, bool checkForPower)
    {
        return map != null
               && CompBell.AllBells
                   .Any(b => b.parent.Map == map
                             && (!checkForPower || !b.ShouldRingAutomatically || b.IsPowered));
    }

    public static bool IsSchoolDesk(this ThingDef def)
    {
        return def.HasComp(typeof(CompSchoolDesk));
    }
}