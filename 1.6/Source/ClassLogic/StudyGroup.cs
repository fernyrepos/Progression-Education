using System;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProgressionEducation
{
    [HotSwappable]
    public class StudyGroup : IExposable, ILoadReferenceable, IRenameable
    {
        public const int MaxTeacherWaitingTicks = GenDate.TicksPerHour * 2;
        public int id = -1;
        public Pawn teacher;
        public List<Pawn> students = [];
        public string className;
        public ClassSubjectLogic subjectLogic;
        public int semesterGoal;
        public float currentProgress;
        public Classroom classroom;
        public int startHour;
        public int endHour;
        public string timeAssignmentDefName;
        public bool suspended = false;
        private static readonly Type t_MapParent_Vehicle =
            GenTypes.GetTypeInAnyAssembly("VehicleMapFramework.MapParent_Vehicle", "VehicleMapFramework");

        public StudyGroup()
        {

        }

        public StudyGroup(Pawn teacher, List<Pawn> students, string className, int semesterGoal, int startHour, int endHour)
        {
            id = EducationManager.Instance.GetNextStudyGroupId();
            this.teacher = teacher;
            this.students = students;
            this.className = className;
            this.semesterGoal = semesterGoal;
            currentProgress = 0;
            classroom = null;
            this.startHour = startHour;
            this.endHour = endHour;
            timeAssignmentDefName = TimeAssignmentUtility.DynamicClassPrefix + GetUniqueLoadID();
        }

        public AcceptanceReport IsValid()
        {
            if (string.IsNullOrEmpty(className))
            {
                return new AcceptanceReport("PE_EnterClassName".Translate());
            }

            if (teacher == null)
            {
                return new AcceptanceReport("PE_SelectTeacher".Translate());
            }

            if (students.NullOrEmpty())
            {
                return new AcceptanceReport("PE_SelectAtLeastOneStudent".Translate());
            }

            var prerequisitesMet = subjectLogic.ArePrerequisitesMet();
            return !prerequisitesMet.Accepted ? prerequisitesMet : AcceptanceReport.WasAccepted;
        }

        public AcceptanceReport ArePrerequisitesMet()
        {
            var subjectPrerequisites = subjectLogic.ArePrerequisitesMet();
            if (!subjectPrerequisites.Accepted)
            {
                return subjectPrerequisites;
            }

            if (classroom?.LearningBoard?.parent == null)
            {
                return new AcceptanceReport("PE_NoLearningBoard".Translate());
            }

            if (!EducationUtility.HasBellOnMap(classroom.LearningBoard.parent.Map, false))
            {
                return new AcceptanceReport("PE_NoBell".Translate());
            }

            return AcceptanceReport.WasAccepted;
        }

        public AcceptanceReport AreWorkspacesAvailable()
        {
            string benchLabel = subjectLogic.BenchLabel;
            if (!string.IsNullOrEmpty(benchLabel) && subjectLogic.BenchCount < students.Count)
            {
                return new AcceptanceReport("PE_NotEnoughBenches".Translate(benchLabel, students.Count, subjectLogic.BenchCount));
            }

            if (!EducationUtility.HasBellOnMap(Map, false))
            {
                return new AcceptanceReport("PE_NoBell".Translate());
            }
            return AcceptanceReport.WasAccepted;
        }

        public void AddProgress(float amount)
        {
            currentProgress += amount;
        }

        public float ProgressPercentage => (float)currentProgress / semesterGoal;
        public bool IsCompleted => !subjectLogic.IsInfinite && currentProgress >= semesterGoal;

        public float CalculateProgressPerTick()
        {
            return subjectLogic.CalculateProgressPerTick();
        }
        public void RemoveStudent(Pawn student)
        {
            if (students.Contains(student))
            {
                students.Remove(student);
                TimeAssignmentUtility.ClearScheduleFromPawns(this, new List<Pawn> { student });
                var lord = student.GetLord();
                if (lord != null && lord.LordJob is LordJob_AttendClass)
                {
                    lord.RemovePawn(student);
                    student.jobs.StopAll();
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id", -1);
            Scribe_References.Look(ref teacher, "teacher");
            Scribe_Collections.Look(ref students, "students", LookMode.Reference);
            Scribe_Values.Look(ref className, "className");
            Scribe_Deep.Look(ref subjectLogic, "subjectLogic", this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && subjectLogic == null)
            {
                subjectLogic = new SkillClassLogic(this);
            }
            Scribe_Values.Look(ref semesterGoal, "semesterGoal");
            Scribe_Values.Look(ref currentProgress, "currentProgress");
            Scribe_References.Look(ref classroom, "classroom");
            Scribe_Values.Look(ref startHour, "startHour", 0);
            Scribe_Values.Look(ref endHour, "endHour", 0);
            Scribe_Values.Look(ref timeAssignmentDefName, "timeAssignmentDefName");
            Scribe_Values.Look(ref suspended, "suspended", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var pawns = new List<Pawn>
                {
                    teacher
                }.Concat(students).ToList();
                foreach (var pawn in pawns)
                {
                    TimeAssignmentUtility.TryRepairTimetable(pawn);
                }
            }
        }

        public string GetUniqueLoadID()
        {
            return "StudyGroup_" + id;
        }

        public string RenamableLabel
        {
            get => className;
            set => className = value;
        }

        public string InspectLabel => className;

        public string BaseLabel => className;

        public void Suspend(bool suspend)
        {
            if (suspended != suspend)
            {
                suspended = suspend;
                var participants = new List<Pawn> { teacher }.Concat(students).ToList();
                if (suspend)
                {
                    TimeAssignmentUtility.ClearScheduleFromPawns(this, participants);
                }
                else
                {
                    TimeAssignmentUtility.ApplyScheduleToPawns(this, participants);
                }

                if (suspended)
                {
                    var map = Map;
                    if (map != null)
                    {
                        Lord lordToCancel = map.lordManager.lords.FirstOrDefault(l =>
                            l.LordJob is LordJob_AttendClass lordJob && lordJob.studyGroup == this);

                        if (lordToCancel != null)
                        {
                            lordToCancel.ReceiveMemo(LordJob_AttendClass.MemoClassCancelled);
                        }
                    }
                }
            }
        }

        public TeacherRole GetTeacherRole()
        {
            return new TeacherRole(this);
        }

        public StudentRole GetStudentRole()
        {
            return new StudentRole(this);
        }

        public Map Map => classroom?.LearningBoard?.parent?.Map;

        public bool ClassIsActive()
        {
            if (subjectLogic is DaycareClassLogic)
            {
                return true;
            }

            if (students.NullOrEmpty())
            {
                return false;
            }

            if (subjectLogic is SkillClassLogic)
            {
                var teachingPawn = teacher;
                if (teachingPawn?.jobs?.curDriver is JobDriver_Teach teachDriver)
                {
                    if (teachDriver.waitingTicks >= MaxTeacherWaitingTicks)
                    {
                        return true;
                    }
                }
            }
            return AllStudentsPresent();
        }

        public bool AllStudentsPresent()
        {
            foreach (var student in students)
            {
                if (student.Dead || student.Downed)
                {
                    return false;
                }
                if (student.jobs?.curDriver is not JobDriver_AttendClass attendClassDriver)
                {
                    return false;
                }

                if (attendClassDriver is JobDriver_AttendMeleeClass)
                {
                    if (!GenAdj.CellsAdjacent8Way(attendClassDriver.TargetA.Thing).Contains(student.Position))
                    {
                        return false;
                    }
                }
                else if (student.Position != JobDriver_AttendClass.DeskSpotStudent(attendClassDriver.job.GetTarget(TargetIndex.A).Thing))
                {
                    return false;
                }
            }
            return true;
        }

        public AcceptanceReport ValidateClassStatus()
        {
            if (suspended)
            {
                return AcceptanceReport.WasRejected;
            }
            var learningBoard = classroom?.LearningBoard?.parent;
            if (classroom is null || learningBoard?.Map == null)
            {
                return new AcceptanceReport("PE_NoLearningBoard".Translate());
            }
            var facingCell = learningBoard.Position + learningBoard.Rotation.FacingCell;
            var blockingBuilding = facingCell.GetFirstBuilding(classroom.LearningBoard.parent.Map);
            if (blockingBuilding != null && blockingBuilding.def.passability != Traversability.Standable)
            {
                return new AcceptanceReport("PE_LearningBoardIsBlocked".Translate(classroom.LearningBoard.parent.def.label));
            }

            var workspaceReport = AreWorkspacesAvailable();
            if (!workspaceReport.Accepted)
            {
                return workspaceReport;
            }

            var learningBoardSourceMap = MapOrSourceMap(classroom.LearningBoard.parent);
            if (!teacher.Spawned || MapOrSourceMap(teacher) != learningBoardSourceMap)
            {
                return new AcceptanceReport("PE_TeacherOffMap".Translate());
            }

            var teacherRole = GetTeacherRole();
            var teacherQualification = teacherRole.CanAcceptPawn(teacher);
            if (!teacherQualification.Accepted)
            {
                return teacherQualification;
            }

            var studentRole = GetStudentRole();
            List<Pawn> studentsOffMap = [];
            List<Pawn> unqualifiedStudents = [];

            foreach (var student in students)
            {
                if (!student.Spawned || MapOrSourceMap(student) != learningBoardSourceMap)
                {
                    if (student.Map is not null && student.Map.Parent is PocketMapParent mapParent && mapParent.sourceMap == classroom.LearningBoard.parent.Map)
                    {
                        continue;
                    }
                    studentsOffMap.Add(student);
                    continue;
                }

                var studentQualification = studentRole.CanAcceptPawn(student);
                if (!studentQualification.Accepted)
                {
                    unqualifiedStudents.Add(student);
                    continue;
                }
            }

            if (studentsOffMap.Count > 0)
            {
                return new AcceptanceReport("PE_StudentsOffMap".Translate());
            }

            if (unqualifiedStudents.Count > 0)
            {
                return new AcceptanceReport("PE_StudentsUnqualified".Translate());
            }

            if (students.Count == 0)
            {
                return new AcceptanceReport("PE_NoStudents".Translate());
            }

            return AcceptanceReport.WasAccepted;

            Map MapOrSourceMap(Thing thing)
            {
                Map map = thing.Map;
                Map sourceMap = map.PocketMapParent?.sourceMap;
                if (sourceMap != null && map.Parent.GetType().SameOrSubclassOf(t_MapParent_Vehicle))
                {
                    return sourceMap;
                }
                return map;
            }
        }
    }
}
