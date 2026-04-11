using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public class Classroom : IExposable, ILoadReferenceable, IRenameable
{
    public Color color;
    public int id;
    public bool interruptJobs = true;
    public bool addKids = true;
    private Thing learningBoardThing;
    public string name;
    public bool restrictReservationsDuringClass;

    public Classroom()
    {
    }

    public Classroom(Thing board)
    {
        learningBoardThing = board;
        var educationManager = EducationManager.Instance;
        id = educationManager.GetNextClassroomId();
        name = "PE_Classroom".Translate() + " " + (educationManager.Classrooms.Count + 1);
        color = new Color(Rand.Value, Rand.Value, Rand.Value);
    }

    public float ClassSpeed => learningBoardThing.GetStatValue(DefsOf.PE_ClassSpeed);

    public CompLearningBoard LearningBoard => learningBoardThing.TryGetComp<CompLearningBoard>();

    public void ExposeData()
    {
        Scribe_Values.Look(ref id, "id");
        Scribe_Values.Look(ref name, "name");
        Scribe_Values.Look(ref color, "color");
        Scribe_References.Look(ref learningBoardThing, "learningBoard");
        Scribe_Values.Look(ref restrictReservationsDuringClass,
            "restrictReservationsDuringClass",
            true);
        Scribe_Values.Look(ref interruptJobs, "interruptJobs", true);
        Scribe_Values.Look(ref addKids, "addKids", true);
    }

    public string GetUniqueLoadID()
    {
        return "Classroom_" + id;
    }

    public string RenamableLabel
    {
        get => name;
        set => name = value;
    }

    public string BaseLabel => name;

    public string InspectLabel => name;
}