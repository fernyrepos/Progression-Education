using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public abstract class ClassSubjectLogic : IExposable
    {
        protected StudyGroup studyGroup;
        public ClassSubjectLogic() { }
        public ClassSubjectLogic(StudyGroup parent) { studyGroup = parent; }

        public ClassSubjectLogic(ClassSubjectLogic other, StudyGroup parent)
        {
            studyGroup = parent;
        }

        private const int InteractionInterval = 900;
        public abstract string Description { get; }
        public virtual bool IsInfinite => false;
        public virtual void AddRequirements(List<string> requirements)
        {
            string benchLabel = BenchLabel;
            if (!string.IsNullOrEmpty(benchLabel))
            {
                int count = studyGroup.students.Count;
                int benchCount = BenchCount;
                string presentText = "";
                if (benchCount < count || benchCount < 1)
                {
                    presentText = $" <color=red>{"PE_Present".Translate(benchCount)}</color>";
                }
                if (benchCount < 1 && count < 1)
                {
                    count = 1;
                }
                requirements.Add($"{count}x {benchLabel}{presentText}");
            }
        }
        public int BenchCount
        {
            get
            {
                var facility = studyGroup.classroom?.LearningBoard?.parent?.GetComp<CompFacility>();
                if (facility is null)
                {
                    return 0;
                }
                var validBenches = GetValidLearningBenches();
                var count = facility.LinkedBuildings.Count(t => validBenches.Contains(t.def));
                return count;
            }
        }
        public virtual string BenchLabel => "PE_SchoolDesks".Translate();
        public abstract void DrawConfigurationUI(Rect rect, ref float curY, IClassDialog classDialog);

        public virtual float CalculateProgressPerTick(Pawn teacher)
        {
            return 0f;
        }
        public virtual void GrantCompletionRewards()
        {
        }

        public abstract AcceptanceReport IsTeacherQualified(Pawn teacher);
        public abstract AcceptanceReport IsStudentQualified(Pawn student);
        public abstract float LearningSpeedModifier { get; }
        public virtual float CalculateLearningPerTick(Pawn student)
        {
            return LearningUtility.NeedSatisfiedPerTick 
                   * LearningUtility.LearningRateFactor(student) 
                   * studyGroup.classroom.CalculateLearningModifier() 
                   * LearningSpeedModifier;
        }

        public virtual void ApplyLearningTick(Pawn student, int delta)
        {
            if (student.DevelopmentalStage == DevelopmentalStage.Child)
            {
                var growthPointsPerTick = student.ageTracker.GrowthPointsPerDay / GenDate.TicksPerDay;
                student.ageTracker.growthPoints += growthPointsPerTick * delta;
            }

            student.needs?.learning?.Learn(CalculateLearningPerTick(student) * delta);
        }

        public virtual void ApplyTeachingTick(Pawn student, JobDriver_Teach jobDriver, int delta)
        {
            var teacher = jobDriver.pawn;
            if (jobDriver.taughtSkill is SkillDef taughtSkill 
                && teacher.IsHashIntervalTick(InteractionInterval, delta))
            {
                teacher.interactions.TryInteractWith(student, taughtSkill.lessonInteraction);
                teacher.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.GaveLesson, student);
                student.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.WasTaught, teacher);
            }
        }
        public void AssignBestTeacher(IClassDialog classDialog)
        {
            var teacherRole = classDialog.TeacherRole;
            var assignmentsManager = classDialog.AssignmentsManager;

            var bestTeacher = classDialog.CandidatePool.AllCandidatePawns
                .Where(p => teacherRole.CanAcceptPawn(p).Accepted)
                .OrderByDescending(CalculateTeacherScore)
                .FirstOrDefault();

            if (bestTeacher != null)
            {
                var existingTeacher = studyGroup.teacher;
                if (existingTeacher != null)
                {
                    assignmentsManager.Unassign(existingTeacher, teacherRole);
                    if (classDialog.StudentRole.CanAcceptPawn(existingTeacher).Accepted)
                    {
                        bool assigned = assignmentsManager.TryAssign(existingTeacher, classDialog.StudentRole, out _);
                        if (assigned)
                        {
                            EducationLog.Message($"Reassigned previous teacher {existingTeacher} to student role in class '{studyGroup.className}'");
                        }
                        else
                        {
                            EducationLog.Warning($"Failed to reassign previous teacher {existingTeacher} to student role in class '{studyGroup.className}'");
                        }
                    }
                    else
                    {
                        EducationLog.Message($"Unassigning previous teacher {existingTeacher} from class '{studyGroup.className}' because they are no longer qualified as a student.");
                    }
                }
                else
                {
                    EducationLog.Message($"No existing teacher assigned for class '{studyGroup.className}'");
                }
                assignmentsManager.TryAssign(bestTeacher, teacherRole, out _);
            }
            else
            {
                EducationLog.Warning($"No qualified teacher found for class '{studyGroup.className}'");
            }
        }
        public abstract float CalculateTeacherScore(Pawn p);

        public virtual bool CanAutoAssign => studyGroup.classroom?.LearningBoard != null;

        public virtual void AutoAssignStudents(IClassDialog classDialog)
        {
            if (!CanAutoAssign)
            {
                EducationLog.Warning($"Cannot auto-assign students for class '{studyGroup.className}' because it lacks a learning board.");
                return;
            }
            EducationLog.Message($"Auto-assigning students and teacher for class '{studyGroup.className}'");

            classDialog.AssignmentsManager.UnassignUnqualifiedPawns();
            AssignBestTeacher(classDialog);
            classDialog.AssignmentsManager.HandleRoleAutoAssignment(classDialog.CandidatePool, classDialog.StudentRole, BenchCount);
            EducationLog.Message($"Auto-assigned students and teacher for class '{studyGroup.className}'");
        }
        public virtual string BaseTooltipFor(Pawn pawn)
        {
            var text = new StringBuilder();
            foreach (var sg in EducationManager.Instance.StudyGroups
                         .Where(s => s.teacher == pawn || s.students.Contains(pawn))
                         .OrderBy(g => g.startHour))
            {
                text.AppendInNewLine(sg.className);
                text.Append(": ");
                text.Append("PE_ScheduleTime".Translate(sg.startHour, sg.endHour));
            }
            return text.ToString();
        }
        public virtual string TeacherTooltipFor(Pawn pawn)
        {
            return "";
        }
        public virtual string StudentTooltipFor(Pawn pawn)
        {
            var text = new StringBuilder();
            text.AppendInNewLine(StatDefOf.GlobalLearningFactor.LabelCap);
            text.Append(": ");
            text.Append(pawn.GetStatValue(StatDefOf.GlobalLearningFactor).ToStringPercent());
            if (pawn.needs?.learning != null)
            {
                var learningPerHour = CalculateLearningPerTick(pawn) * GenDate.TicksPerHour;
                var learningPerSession = learningPerHour * studyGroup.Duration;
                text.AppendInNewLine("PE_Learning".Translate());
                text.Append(": ");
                text.Append(learningPerSession.ToStringPercent());
                text.Append("PE_PerSession".Translate());
            }
            return text.ToString();
        }

        public virtual AcceptanceReport ArePrerequisitesMet()
        {
            var benchLabel = BenchLabel;
            if (!string.IsNullOrEmpty(benchLabel) && BenchCount < studyGroup.students.Count)
            {
                return new AcceptanceReport("PE_NotEnoughBenches".Translate(benchLabel, studyGroup.students.Count, BenchCount));
            }
            return AcceptanceReport.WasAccepted;
        }

        public virtual void ExposeData()
        {
            Scribe_References.Look(ref studyGroup, "studyGroup");
        }
        protected HashSet<ThingDef> validLearningBenches;
        public virtual JobDef LearningJob => DefsOf.PE_AttendClass;
        public virtual void HandleStudentLifecycleEvents() { }
        public virtual HashSet<ThingDef> GetValidLearningBenches()
        {
            validLearningBenches ??= [ .. DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def.IsSchoolDesk())
            ];
            return validLearningBenches;
        }
    }
}
