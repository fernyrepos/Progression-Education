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
        private bool retroactivelyApplied = false;

        public EducationManager(World world) : base(world)
        {
        }

        public List<Classroom> Classrooms => classrooms;
        public List<StudyGroup> StudyGroups => studyGroups;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref studyGroups, "studyGroups", LookMode.Deep);
            Scribe_Values.Look(ref nextClassroomId, "nextClassroomId");
            Scribe_Values.Look(ref nextStudyGroupId, "nextStudyGroupId");
            Scribe_Values.Look(ref retroactivelyApplied, "retroactivelyApplied", defaultValue: false);
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

            EducationLog.Message($"Removing study group '{studyGroup.className}'. Cleaning up pawn timetables...");
            List<Pawn> allParticipants = [studyGroup.teacher, .. studyGroup.students];
            TimeAssignmentUtility.ClearScheduleFromPawns(studyGroup, allParticipants);
            TimeAssignmentUtility.RemoveTimeAssignmentDef(studyGroup);

        }

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);
            EducationLog.Message("Finalizing EducationManager init");
            if (!retroactivelyApplied)
            {
                retroactivelyApplied = true;
                EducationLog.Message("Applying proficiency traits to existing humanlike pawns");
                ApplyProficiencyTraitsToHumanlikePawns();
            }
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

        public void TryInitiateClassForStudyGroup(StudyGroup studyGroup)
        {
            var currentAssignment = studyGroup.teacher.timetable.CurrentAssignment;
            if (!currentAssignment.IsStudyGroupAssignment() || currentAssignment.defName != studyGroup.timeAssignmentDefName)
            {
                EducationLog.Message($"Current assignment for teacher {studyGroup.teacher.LabelShort} is not a study group assignment.");
                return;
            }

            var lord = studyGroup.teacher.GetLord();
            if (lord != null && lord.LordJob is LordJob_AttendClass)
            {
                EducationLog.Message($"Teacher {studyGroup.teacher.LabelShort} is already in a LordJob_AttendClass. Not initiating another class.");
                return;
            }
            var classroomMap = studyGroup.classroom.LearningBoard.parent.Map;
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

            if (studyGroup.classroom is null || studyGroup.classroom.LearningBoard?.parent == null || studyGroup.classroom.LearningBoard.parent.Map == null)
            {
                EducationLog.Message($"Classroom or learning board is null for class '{studyGroup.className}'. Cannot initiate class.");
                return;
            }
            
            if (!EducationUtility.HasBellOnMap(classroomMap, true))
            {
                EducationLog.Message($"No bell found on map for class '{studyGroup.className}'. Cannot initiate class.");
                return;
            }
            var teacherRole = studyGroup.GetTeacherRole();
            var teacherQualification = teacherRole.CanAcceptPawn(studyGroup.teacher);
            if (!teacherQualification.Accepted)
            {
                Messages.Message("PE_TeacherUnassigned".Translate(studyGroup.teacher.LabelShort, teacherQualification.Reason), MessageTypeDefOf.RejectInput);
                EducationLog.Message($"Teacher {studyGroup.teacher.LabelShort} no longer qualifies for class '{studyGroup.className}'. Cannot initiate class.");
                return;
            }
            var studentRole = studyGroup.GetStudentRole();
            List<Pawn> qualifiedStudents = [];

            foreach (var student in studyGroup.students)
            {
                var studentQualification = studentRole.CanAcceptPawn(student);
                if (studentQualification.Accepted)
                {
                    qualifiedStudents.Add(student);
                }
                else
                {
                    Messages.Message("PE_StudentUnqualified".Translate(student.LabelShort, studentQualification.Reason), MessageTypeDefOf.RejectInput);
                    EducationLog.Message($"Student {student.LabelShort} no longer qualifies for class '{studyGroup.className}': {studentQualification.Reason}");
                }
            }
            if (qualifiedStudents.Count == 0)
            {
                EducationLog.Message($"No qualified students for class '{studyGroup.className}'. Cannot initiate class.");
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
            EducationLog.Message($"Initiating class '{studyGroup.className}' with teacher {studyGroup.teacher.LabelShort} and {qualifiedStudents.Count} qualified students.");

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

        private void ApplyProficiencyTraitsToHumanlikePawns()
        {
            var humanlikePawns = PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(pawn =>
                pawn.RaceProps.Humanlike &&
                pawn.story?.traits != null).ToList();

            foreach (var pawn in humanlikePawns)
            {
                ProficiencyUtility.ApplyProficiencyTraitToPawn(pawn);
            }
        }
    }
}
