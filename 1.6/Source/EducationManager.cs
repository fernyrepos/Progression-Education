using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation;

[HotSwappable]
public class EducationManager(World world) : WorldComponent(world)
{
    private static EducationManager _instance;

    private List<Classroom> classrooms = [];
    private int nextClassroomId;
    private int nextStudyGroupId;
    public List<StudyGroup> studyGroups = [];

    public List<Classroom> Classrooms
    {
        get
        {
            classrooms ??= [];
            classrooms.RemoveAll(x => x == null);
            return classrooms;
        }
    }

    public static EducationManager Instance
    {
        get
        {
            if (_instance == null
                || _instance.world != Find.World)
            {
                _instance = Find.World.GetComponent<EducationManager>();
            }

            return _instance;
        }
    }

    public List<StudyGroup> StudyGroups
    {
        get
        {
            studyGroups ??= [];
            studyGroups.RemoveAll(x => x?.classroom == null);
            return studyGroups;
        }
    }

    public void AddClassroom(Classroom classroom)
    {
        if (classroom == null)
        {
            return;
        }

        classrooms ??= [];
        if (!classrooms.Contains(classroom))
        {
            classrooms.Add(classroom);
        }

        EducationLog.Message($"Classroom added: {classroom.GetUniqueLoadID()}");
    }

    public void AddStudyGroup(StudyGroup studyGroup)
    {
        studyGroups.Add(studyGroup);
        EducationLog.Message($"EducationManager.AddStudyGroup Scheduled class added: {studyGroup.className} ({studyGroup.GetUniqueLoadID()})");
    }

    public static void ApplyScheduleToPawns(StudyGroup studyGroup)
    {
        TimeAssignmentUtility.ApplyScheduleToPawns(studyGroup,
            studyGroup.AllParticipants);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref studyGroups, nameof(studyGroups),
            LookMode.Deep);
        Scribe_Values.Look(ref nextClassroomId, nameof(nextClassroomId));
        Scribe_Values.Look(ref nextStudyGroupId, nameof(nextStudyGroupId));
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            studyGroups ??= [];
            classrooms ??= [];
        }
    }

    public override void FinalizeInit(bool fromLoad)
    {
        base.FinalizeInit(fromLoad);
        TimeAssignmentUtility.RemoveAllDynamicTimeAssignmentDefs();
        foreach (var studyGroup in studyGroups)
        {
            EducationLog.Message($"Generating TimeAssignmentDef for study group '{studyGroup.className}'");
            TimeAssignmentUtility.GenerateTimeAssignmentDef(studyGroup);
        }
        foreach (var pawn in PawnsFinder.AllMapsAndWorld_Alive)
        {
            ProficiencyUtility.ApplyProficiencyTraitToPawn(pawn);
        }
    }

    public int GetNextClassroomId()
    {
        return nextClassroomId++;
    }

    public int GetNextStudyGroupId()
    {
        return nextStudyGroupId++;
    }

    private void InterruptPawnsUsingLearningBenches(StudyGroup studyGroup)
    {
        var validBenches = studyGroup.subjectLogic.GetValidLearningBenches();
        if (validBenches == null
            || !validBenches.Any())
        {
            return;
        }

        var classroomRoom = studyGroup.classroom.LearningBoard.parent.GetRoom();
        if (classroomRoom == null)
        {
            return;
        }

        foreach (var pawn in classroomRoom.ContainedThings<Pawn>())
        {
            var curJob = pawn.CurJob;
            if (curJob == null)
            {
                continue;
            }

            var targets = new[] { curJob.targetA, curJob.targetB };
            var shouldInterrupt = targets
                .Where(target => target.HasThing)
                .Any(target => validBenches.Contains(target.Thing.def));

            if (shouldInterrupt)
            {
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
                EducationLog.Message($"-> Interrupted pawn {pawn.LabelShort} who was using a learning bench during class initiation.");
            }
        }
    }

    public void Notify_ClassInvalidated(StudyGroup studyGroup)
    {
        studyGroup.cancelledUntilTick = -1;
    }

    public void RemoveClassroom(Classroom classroom)
    {
        var studyGroupsToRemove = StudyGroups.Where(sg => sg.classroom == classroom).ToList();
        foreach (var studyGroup in studyGroupsToRemove)
        {
            var allParticipants = studyGroup.AllParticipants;
            EducationLog.Message($"-> Removing study group '{studyGroup.className}' due to classroom removal. Cleaning up timetables for participants: {allParticipants.ToStringSafeEnumerable()}");
            TimeAssignmentUtility.ClearScheduleFromPawns(studyGroup,
                allParticipants);
            TimeAssignmentUtility.RemoveTimeAssignmentDef(studyGroup);
        }

        studyGroups.RemoveAll(sg => sg.classroom == classroom);
        classrooms.Remove(classroom);
        EducationLog.Message($"Classroom removed: {classroom.GetUniqueLoadID()}");
    }

    public void RemoveStudyGroup(StudyGroup studyGroup)
    {
        studyGroups.Remove(studyGroup);
        var allParticipants = studyGroup.AllParticipants;
        EducationLog.Message($"EducationManager.RemoveStudyGroup Removing study group '{studyGroup.className}'. Cleaning up timetables for participants: {allParticipants.ToStringSafeEnumerable()}");
        TimeAssignmentUtility.ClearScheduleFromPawns(studyGroup,
            allParticipants);
        TimeAssignmentUtility.RemoveTimeAssignmentDef(studyGroup);
    }

    public void TryInitiateClassForStudyGroup(StudyGroup studyGroup)
    {
        if (studyGroup.cancelledUntilTick > Find.TickManager.TicksGame)
        {
            return;
        }

        var currentAssignment = studyGroup.teacher.timetable.CurrentAssignment;
        if (!currentAssignment.IsStudyGroupAssignment()
            || currentAssignment.defName != studyGroup.timeAssignmentDefName)
        {
            return;
        }

        if (studyGroup.suspended)
        {
            return;
        }

        var validationReport = studyGroup.ValidateClassStatus();
        if (!validationReport.Accepted)
        {
            EducationLog.Message($"-> Class '{studyGroup.className}' cancelled due to validation failure: {validationReport.Reason}");
            if (studyGroup.cancelledUntilTick < Find.TickManager.TicksGame)
            {
                Messages.Message(
                    $"{"PE_ClassCancelledToday".Translate(studyGroup.className)} {validationReport.Reason}",
                    MessageTypeDefOf.NegativeEvent);
                TimeAssignmentUtility.ClearScheduleFromPawns(studyGroup, studyGroup.AllParticipants);
                studyGroup.cancelledUntilTick = Find.TickManager.TicksGame + (studyGroup.Duration * GenDate.TicksPerHour);
            }

            return;
        }

        var classroomMap = studyGroup.classroom.LearningBoard.parent.Map;

        if (!EducationUtility.HasBellOnMap(classroomMap, true))
        {
            EducationLog.Message($"-> No bell found on map for class '{studyGroup.className}'. Cannot initiate class.");
            return;
        }

        if (studyGroup.teacher.GetLord() is Lord lord
            && lord.LordJob is LordJob_AttendClass)
        {
            EducationLog.Message($"-> Teacher {studyGroup.teacher.LabelShort} is already in a LordJob_AttendClass. Not initiating another class.");
            return;
        }

        if (classroomMap.lordManager.lords
            .Any(l => l.LordJob is LordJob_AttendClass attendClassLordJob
                      && attendClassLordJob.studyGroup == studyGroup))
        {
            EducationLog.Message($"-> An existing LordJob_AttendClass was found for class '{studyGroup.className}'. Not initiating another class.");
            return;
        }

        if (!GatheringsUtility.PawnCanStartOrContinueGathering(studyGroup.teacher))
        {
            EducationLog.Message($"-> but {studyGroup.teacher.LabelShort} is unavailable. Suspending class.");
            studyGroup.Notify_TeacherUnavailable();
            return;
        }

        LordJob_AttendClass lordJob = new(studyGroup);
        List<Pawn> initialPawns = [studyGroup.teacher];
        initialPawns.RemoveAll(p => p.GetLord() != null);

        if (initialPawns.Count < 1)
        {
            EducationLog.Message($"-> All participants for class '{studyGroup.className}' are already in other lords. Cannot initiate class.");
            return;
        }

        EducationLog.Message($"-> Initiating class '{studyGroup.className}' with teacher {studyGroup.teacher.LabelShort}.");

        if (studyGroup.classroom.restrictReservationsDuringClass)
        {
            InterruptPawnsUsingLearningBenches(studyGroup);
        }

        LordMaker.MakeNewLord(Faction.OfPlayer, lordJob, classroomMap,
            initialPawns);
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        if (Find.TickManager.TicksGame % 180 != 0)
        {
            return;
        }

        foreach (var studyGroup in StudyGroups)
        {
            studyGroup.subjectLogic.HandleStudentLifecycleEvents();
            if (studyGroup.cancelledUntilTick != -1 && Find.TickManager.TicksGame >= studyGroup.cancelledUntilTick)
            {
                studyGroup.cancelledUntilTick = -1;
                TimeAssignmentUtility.ApplyScheduleToPawns(studyGroup, studyGroup.AllParticipants);
            }
            TryInitiateClassForStudyGroup(studyGroup);
        }

        foreach (var classroom in Classrooms
                     .Where(classroom => classroom.LearningBoard == null
                                         || classroom.LearningBoard.parent.Destroyed))
        {
            EducationLog.Message($"EducationManager.WorldComponentTick Classroom '{classroom.name}' has no valid learning board. Removing classroom.");
            RemoveClassroom(classroom);
        }
    }
}
