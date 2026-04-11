using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ProgressionEducation;

public class Pawn_TimetableTracker_Fixed : IExposable
{
    public Pawn pawn;

    private List<TimeAssignmentDef> times = DefaultTimes;

    public Pawn_TimetableTracker_Fixed(Pawn pawn) : this()
    {
        this.pawn = pawn;
    }

    public Pawn_TimetableTracker_Fixed()
    {
    }

    public static List<TimeAssignmentDef> DefaultTimes
    {
        get
        {
            var times = new List<TimeAssignmentDef>(24);
            for (var i = 0; i < 24; ++i)
            {
                times.Add(i is <= 5 or > 21
                    ? TimeAssignmentDefOf.Sleep
                    : TimeAssignmentDefOf.Anything);
            }

            return times;
        }
    }

    public List<TimeAssignmentDef> Times => times;

    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, nameof(pawn));
        Scribe_Collections.Look(ref times, nameof(times), LookMode.Def);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            times ??= DefaultTimes;
        }
    }

    public TimeAssignmentDef GetAssignment(int hour)
    {
        return hour is < 0 or > 24
            ? TimeAssignmentDefOf.Anything
            : times[hour];
    }

    public void SetAssignment(int hour, TimeAssignmentDef assignment)
    {
        if (hour is >= 0 and < 24)
        {
            times[hour] = assignment;
        }
    }
}