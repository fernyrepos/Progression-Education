using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
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
    private Dictionary<Pawn, Dictionary<string, float>> proficiencyProgressByPawn = [];
    private List<Pawn> savedProgressPawns = [];
    private List<string> savedProgressKeys = [];
    private List<float> savedProgressValues = [];
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
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            FlattenProficiencyProgress();
        }

        Scribe_Collections.Look(ref savedProgressPawns, nameof(savedProgressPawns),
            LookMode.Reference);
        Scribe_Collections.Look(ref savedProgressKeys, nameof(savedProgressKeys),
            LookMode.Value);
        Scribe_Collections.Look(ref savedProgressValues, nameof(savedProgressValues),
            LookMode.Value);
        Scribe_Values.Look(ref nextClassroomId, nameof(nextClassroomId));
        Scribe_Values.Look(ref nextStudyGroupId, nameof(nextStudyGroupId));
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            studyGroups ??= [];
            classrooms ??= [];
            savedProgressPawns ??= [];
            savedProgressKeys ??= [];
            savedProgressValues ??= [];
            RebuildProficiencyProgress();
            MigrateLegacyProficiencyClassProgress();
        }
    }

    private static readonly Dictionary<(string TrackDefName, string TierDefName), string> ProficiencyProgressKeyCache = new();

    private static string BuildProficiencyProgressKey(ProficiencyDef track, ProficiencyTierDef tier)
    {
        if (track == null || tier == null)
        {
            return null;
        }

        var cacheKey = (TrackDefName: track.defName, TierDefName: tier.defName);
        if (!ProficiencyProgressKeyCache.TryGetValue(cacheKey, out var key))
        {
            key = string.Concat(cacheKey.TrackDefName, ":", cacheKey.TierDefName);
            ProficiencyProgressKeyCache[cacheKey] = key;
        }

        return key;
    }

    private void FlattenProficiencyProgress()
    {
        savedProgressPawns = [];
        savedProgressKeys = [];
        savedProgressValues = [];

        if (proficiencyProgressByPawn == null)
        {
            return;
        }

        foreach (var pawnProgress in proficiencyProgressByPawn)
        {
            if (pawnProgress.Key == null || pawnProgress.Value == null)
            {
                continue;
            }

            foreach (var entry in pawnProgress.Value.Where(entry => entry.Value > 0f && !entry.Key.NullOrEmpty()))
            {
                savedProgressPawns.Add(pawnProgress.Key);
                savedProgressKeys.Add(entry.Key);
                savedProgressValues.Add(entry.Value);
            }
        }
    }

    private void RebuildProficiencyProgress()
    {
        proficiencyProgressByPawn = [];

        var entryCount = savedProgressPawns.Count;
        entryCount = Mathf.Min(entryCount, savedProgressKeys.Count);
        entryCount = Mathf.Min(entryCount, savedProgressValues.Count);
        for (var i = 0; i < entryCount; i++)
        {
            var pawn = savedProgressPawns[i];
            var key = savedProgressKeys[i];
            var progress = savedProgressValues[i];
            if (pawn == null || key.NullOrEmpty() || progress <= 0f)
            {
                continue;
            }

            if (!proficiencyProgressByPawn.TryGetValue(pawn, out var pawnProgress))
            {
                pawnProgress = [];
                proficiencyProgressByPawn[pawn] = pawnProgress;
            }

            pawnProgress[key] = progress;
        }
    }

    private void MigrateLegacyProficiencyClassProgress()
    {
        foreach (var studyGroup in studyGroups)
        {
            if (studyGroup?.subjectLogic is not ProficiencyClassLogic proficiencyLogic
                || proficiencyLogic.proficiencyTrack == null
                || proficiencyLogic.targetTier == null
                || studyGroup.semesterGoal <= 0
                || studyGroup.currentProgress <= 0f
                || studyGroup.currentProgress >= studyGroup.semesterGoal
                || studyGroup.students.NullOrEmpty())
            {
                continue;
            }

            var classProgress = Mathf.Clamp(studyGroup.currentProgress, 0f, studyGroup.semesterGoal);
            var activeStudents = studyGroup.students
                .Where(student => student != null
                                  && !ProficiencyUtility.MeetsOrExceedsTier(student, proficiencyLogic.proficiencyTrack, proficiencyLogic.targetTier))
                .ToList();
            if (activeStudents.Count == 0
                || activeStudents.Any(student => GetProficiencyClassProgress(student, proficiencyLogic.proficiencyTrack, proficiencyLogic.targetTier) > 0f))
            {
                continue;
            }

            foreach (var student in activeStudents)
            {
                AddProficiencyClassProgress(student, proficiencyLogic.proficiencyTrack, proficiencyLogic.targetTier, classProgress, studyGroup.semesterGoal);
            }
        }
    }

    public float GetProficiencyClassProgress(Pawn pawn, ProficiencyDef track, ProficiencyTierDef tier)
    {
        var key = BuildProficiencyProgressKey(track, tier);
        if (pawn == null || key == null)
        {
            return 0f;
        }

        if (!proficiencyProgressByPawn.TryGetValue(pawn, out var pawnProgress)
            || !pawnProgress.TryGetValue(key, out var progress))
        {
            return 0f;
        }

        return progress;
    }

    public float AddProficiencyClassProgress(Pawn pawn, ProficiencyDef track, ProficiencyTierDef tier, float amount, float goal)
    {
        if (pawn == null || track == null || tier == null || amount <= 0f)
        {
            return GetProficiencyClassProgress(pawn, track, tier);
        }

        var key = BuildProficiencyProgressKey(track, tier);
        if (key == null)
        {
            return 0f;
        }

        if (!proficiencyProgressByPawn.TryGetValue(pawn, out var pawnProgress))
        {
            pawnProgress = [];
            proficiencyProgressByPawn[pawn] = pawnProgress;
        }

        var progress = pawnProgress.TryGetValue(key, out var existingProgress)
            ? existingProgress + amount
            : amount;
        if (goal > 0f)
        {
            progress = Mathf.Min(progress, goal);
        }

        pawnProgress[key] = Mathf.Max(0f, progress);
        return pawnProgress[key];
    }

    public void ClearProficiencyClassProgress(Pawn pawn, ProficiencyDef track, ProficiencyTierDef tier)
    {
        var key = BuildProficiencyProgressKey(track, tier);
        if (pawn == null || key == null)
        {
            return;
        }

        if (!proficiencyProgressByPawn.TryGetValue(pawn, out var pawnProgress))
        {
            return;
        }

        pawnProgress.Remove(key);
        if (pawnProgress.Count == 0)
        {
            proficiencyProgressByPawn.Remove(pawn);
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

    public void CompleteStudyGroup(StudyGroup studyGroup, Lord lord = null)
    {
        EducationLog.Message($"Class '{studyGroup.className}' has completed its semester goal. Granting rewards and ending class.");
        studyGroup.subjectLogic.GrantCompletionRewards();
        var label = studyGroup.subjectLogic.GetCompletionLetterLabel();
        var text = studyGroup.subjectLogic.GetCompletionLetterText();
        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent);
        RemoveStudyGroup(studyGroup);
        if (lord != null)
        {
            lord.ReceiveMemo("ClassCompleted");
        }
        else
        {
            studyGroup.CancelClass();
        }
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
        if (studyGroup.IsCompleted)
        {
            CompleteStudyGroup(studyGroup);
            return;
        }

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

        foreach (var studyGroup in StudyGroups.ToList())
        {
            if (studyGroup.IsCompleted)
            {
                CompleteStudyGroup(studyGroup);
                continue;
            }

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
