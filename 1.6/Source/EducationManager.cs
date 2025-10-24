using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace ProgressionEducation
{
    [HotSwappable]
    public class EducationManager : WorldComponent
    {
        private static EducationManager _instance;
        public static EducationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Current.Game.World.GetComponent<EducationManager>();
                }
                else if (_instance.world != Current.Game.World)
                {
                    _instance = Current.Game.World.GetComponent<EducationManager>();
                }
                return _instance;
            }
        }
        private List<Classroom> classrooms = [];
        private List<StudyGroup> studyGroups = [];
        private int nextClassroomId;
        private int nextStudyGroupId;
        public EducationManager(World world) : base(world)
        {
        }

        public List<Classroom> Classrooms => classrooms ??= new List<Classroom>();
        public List<StudyGroup> StudyGroups => studyGroups ??= new List<StudyGroup>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref studyGroups, "studyGroups", LookMode.Deep);
            Scribe_Values.Look(ref nextClassroomId, "nextClassroomId");
            Scribe_Values.Look(ref nextStudyGroupId, "nextStudyGroupId");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                studyGroups ??= [];
                classrooms ??= [];
            }
        }

        public void AddClassroom(Classroom classroom)
        {
            if (!classrooms.Contains(classroom))
            {
                classrooms.Add(classroom);
            }
            EducationLog.Message($"Classroom added: {classroom.GetUniqueLoadID()}");
        }

        public int GetNextClassroomId()
        {
            return nextClassroomId++;
        }

        public int GetNextStudyGroupId()
        {
            return nextStudyGroupId++;
        }

        public void RemoveClassroom(Classroom classroom)
        {
            studyGroups.RemoveAll(sg => sg.classroom == classroom);
            classrooms.Remove(classroom);
            EducationLog.Message($"Classroom removed: {classroom.GetUniqueLoadID()}");
        }

        public void AddStudyGroup(StudyGroup studyGroup)
        {

            studyGroups.Add(studyGroup);
            EducationLog.Message($"Scheduled class added: {studyGroup.className} ({studyGroup.GetUniqueLoadID()})");
        }

        public void RemoveStudyGroup(StudyGroup studyGroup)
        {
            studyGroups.Remove(studyGroup);

            List<Pawn> allParticipants = [studyGroup.teacher, .. studyGroup.students];
            EducationLog.Message($"Removing study group '{studyGroup.className}'. Cleaning up timetables for participants: {allParticipants.ToStringSafeEnumerable()}");

            TimeAssignmentUtility.ClearScheduleFromPawns(studyGroup, allParticipants);
            TimeAssignmentUtility.RemoveTimeAssignmentDef(studyGroup);

        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            EducationLog.Message("Finalizing EducationManager init");
            TimeAssignmentUtility.RemoveAllDynamicTimeAssignmentDefs();
            foreach (var studyGroup in studyGroups)
            {
                EducationLog.Message($"Generating TimeAssignmentDef for study group '{studyGroup.className}'");
                TimeAssignmentUtility.GenerateTimeAssignmentDef(studyGroup);
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Current.Game.tickManager.TicksGame % 180 == 0)
            {
                foreach (var studyGroup in StudyGroups)
                {
                    studyGroup.subjectLogic.HandleStudentLifecycleEvents();
                    TryInitiateClassForStudyGroup(studyGroup);
                }
            }
        }

        public void ApplyScheduleToPawns(StudyGroup studyGroup)
        {
            List<Pawn> allParticipants = [studyGroup.teacher, .. studyGroup.students];
            TimeAssignmentUtility.ApplyScheduleToPawns(studyGroup, allParticipants);
        }

        private HashSet<StudyGroup> checkedStudyGroups = new();
        public void TryInitiateClassForStudyGroup(StudyGroup studyGroup)
        {
            checkedStudyGroups ??= new HashSet<StudyGroup>();
            var currentAssignment = studyGroup.teacher.timetable.CurrentAssignment;
            if (!currentAssignment.IsStudyGroupAssignment() || currentAssignment.defName != studyGroup.timeAssignmentDefName)
            {
                checkedStudyGroups.Remove(studyGroup);
                return;
            }
            bool alreadyGivenMessage = checkedStudyGroups.Contains(studyGroup) is false;
            var validationReport = studyGroup.ValidateClassStatus();
            if (!validationReport.Accepted)
            {
                EducationLog.Message($"Class '{studyGroup.className}' cancelled due to validation failure: {validationReport.Reason}");
                if (alreadyGivenMessage)
                {
                    Messages.Message($"{"PE_ClassCancelledToday".Translate(studyGroup.className)} {validationReport.Reason}", MessageTypeDefOf.NegativeEvent);
                    checkedStudyGroups.Add(studyGroup);
                }
                return;
            }

            var classroomMap = studyGroup.classroom.LearningBoard.parent.Map;
            
            if (!EducationUtility.HasBellOnMap(classroomMap, true))
            {
                EducationLog.Message($"No bell found on map for class '{studyGroup.className}'. Cannot initiate class.");
                return;
            }

            var lord = studyGroup.teacher.GetLord();
            if (lord != null && lord.LordJob is LordJob_AttendClass)
            {
                EducationLog.Message($"Teacher {studyGroup.teacher.LabelShort} is already in a LordJob_AttendClass. Not initiating another class.");
                return;
            }
            
            bool existingLordFound = false;
            foreach (var existingLord in classroomMap.lordManager.lords)
            {
                if (existingLord.LordJob is LordJob_AttendClass attendClassLordJob &&
                    attendClassLordJob.studyGroup == studyGroup)
                {
                    existingLordFound = true;
                    break;
                }
            }

            if (existingLordFound)
            {
                EducationLog.Message($"An existing LordJob_AttendClass was found for class '{studyGroup.className}'. Not initiating another class.");
                return;
            }

            LordJob_AttendClass lordJob = new(studyGroup);
            List<Pawn> initialPawns = [studyGroup.teacher];
            initialPawns.RemoveAll(p => p.GetLord() != null);

            if (initialPawns.Count < 1)
            {
                EducationLog.Message($"All participants for class '{studyGroup.className}' are already in other lords. Cannot initiate class.");
                return;
            }
            EducationLog.Message($"Initiating class '{studyGroup.className}' with teacher {studyGroup.teacher.LabelShort}.");

            if (studyGroup.classroom.restrictReservationsDuringClass)
            {
                InterruptPawnsUsingLearningBenches(studyGroup, classroomMap);
            }

            LordMaker.MakeNewLord(Faction.OfPlayer, lordJob, classroomMap, initialPawns);
        }

        private void InterruptPawnsUsingLearningBenches(StudyGroup studyGroup, Map map)
        {
            var validBenches = studyGroup.subjectLogic.GetValidLearningBenches();
            if (validBenches == null || !validBenches.Any())
            {
                return;
            }

            var classroomRoom = studyGroup.classroom.LearningBoard.parent.GetRoom();
            if (classroomRoom == null)
            {
                return;
            }

            var pawnsInRoom = classroomRoom.ContainedThings<Pawn>().ToList();
            foreach (var pawn in pawnsInRoom)
            {
                var curJob = pawn.CurJob;
                if (curJob != null)
                {
                    bool shouldInterrupt = false;
                    var targets = new[] { curJob.targetA, curJob.targetB };
                    foreach (var target in targets)
                    {
                        if (!target.HasThing) continue;

                        if (validBenches.Contains(target.Thing.def))
                        {
                            shouldInterrupt = true;
                            break;
                        }
                    }

                    if (shouldInterrupt)
                    {
                        pawn.jobs?.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                        EducationLog.Message($"Interrupted pawn {pawn.LabelShort} who was using a learning bench during class initiation.");
                    }
                }
            }
        }
    }
}
