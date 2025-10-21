using System;
using System.Collections.Generic;
using System.Linq;
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
                if (facility is null) return 0;
                var validBenches = GetValidLearningBenches();
                var count = facility.LinkedBuildings.Count(t => validBenches.Contains(t.def));
                EducationLog.Message($"Found {count} valid benches for class '{studyGroup.className}'");
                return count;
            }
        }
        public virtual string BenchLabel => "PE_SchoolDesks".Translate();
        public abstract void DrawConfigurationUI(Rect rect, ref float curY, Map map, Dialog_CreateClass createClassDialog);
        public abstract float CalculateProgressPerTick();
        public abstract void GrantCompletionRewards();
        public abstract AcceptanceReport IsTeacherQualified(Pawn teacher);
        public abstract AcceptanceReport IsStudentQualified(Pawn student);
        public abstract float LearningSpeedModifier { get; }

        public virtual void ApplyLearningTick(Pawn student)
        {
            if (student.DevelopmentalStage == DevelopmentalStage.Child)
            {
                float growthPointsPerTick = student.ageTracker.GrowthPointsPerDay / 60000f;
                student.ageTracker.growthPoints += growthPointsPerTick;
            }
            if (student.needs?.learning != null)
            {
                float learningRateFactor = LearningUtility.LearningRateFactor(student) * this.studyGroup.classroom.CalculateLearningModifier() * LearningSpeedModifier;
                student.needs.learning.Learn(1.2E-05f * learningRateFactor * 3f);
            }
        }

        public virtual void ApplyTeachingTick(Pawn student, JobDriver_Teach jobDriver)
        {
        }
        public void AssignBestTeacher(Dialog_CreateClass createClassDialog)
        {
            var teacherRole = createClassDialog.TeacherRole;
            var assignmentsManager = createClassDialog.AssignmentsManager;

            var bestTeacher = createClassDialog.CandidatePool.AllCandidatePawns
                .Where(p => teacherRole.CanAcceptPawn(p).Accepted)
                .OrderByDescending(p => CalculateTeacherScore(p))
                .FirstOrDefault();
            
            if (bestTeacher != null)
            {
                var existingTeacher = studyGroup.teacher;
                if (existingTeacher != null)
                {
                    assignmentsManager.Unassign(existingTeacher, teacherRole);
                    if (createClassDialog.StudentRole.CanAcceptPawn(existingTeacher).Accepted)
                    {
                        bool assigned = assignmentsManager.TryAssign(existingTeacher, createClassDialog.StudentRole, out _);
                        if (assigned)
                        {
                            EducationLog.Message($"Reassigned previous teacher {existingTeacher} to student role in class '{studyGroup.className}'");
                        }
                        else
                        {
                            Log.Warning($"Failed to reassign previous teacher {existingTeacher} to student role in class '{studyGroup.className}'");
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
                Log.Warning($"No qualified teacher found for class '{studyGroup.className}'");
            }
        }
        public abstract float CalculateTeacherScore(Pawn p);

        public virtual bool CanAutoAssign => studyGroup.classroom?.LearningBoard != null;

        public virtual void AutoAssignStudents(Dialog_CreateClass createClassDialog)
        {
            if (!CanAutoAssign)
            {
                Log.Warning($"Cannot auto-assign students for class '{studyGroup.className}' because it lacks a learning board.");
                return;
            }
            EducationLog.Message($"Auto-assigning students and teacher for class '{studyGroup.className}'");
            
            UnassignUnqualifiedPawns(createClassDialog);
            AssignBestTeacher(createClassDialog);
            HandleRoleAutoAssignment(createClassDialog, createClassDialog.StudentRole, BenchCount);
            EducationLog.Message($"Auto-assigned students and teacher for class '{studyGroup.className}'");
        }
        public abstract string TeacherTooltipFor(Pawn pawn);
        public abstract string StudentTooltipFor(Pawn pawn);

        protected void UnassignUnqualifiedPawns(Dialog_CreateClass createClassDialog)
        {
            foreach (var role in createClassDialog.AssignmentsManager.Roles)
            {
                var assignedPawns = createClassDialog.AssignmentsManager.AssignedPawns(role).ToList();
                for (int i = assignedPawns.Count - 1; i >= 0; i--)
                {
                    var pawn = assignedPawns[i];
                    var canAccept = role.CanAcceptPawn(pawn);
                    if (!canAccept.Accepted)
                    {
                        createClassDialog.AssignmentsManager.TryUnassignAnyRole(pawn);
                        EducationLog.Message($"Unassigning {pawn} from {role} because they are no longer qualified: {canAccept.Reason}");
                    }
                }
            }
        }

        protected void DrawBenchRequirementUI(Rect rect, ref float curY)
        {
            string label = BenchLabel;
            if (string.IsNullOrEmpty(label))
            {
                return;
            }
            Widgets.Label(new Rect(rect.x, curY, 200f, 25f), "PE_Requirements".Translate());
            curY += 25f;
            int count = studyGroup.students.Count;
            int benchCount = BenchCount;
            string presentText = "";
            if (benchCount < count || benchCount < 1)
            {
                GUI.color = Color.red;
                presentText = " " + "PE_Present".Translate(benchCount);
            }
            if (benchCount < 1 && count < 1)
            {
                count = 1;
            }
            Widgets.Label(new Rect(rect.x + 10f, curY, 300f, 25f), $"{count}x {label}{presentText}");
            GUI.color = Color.white;
            curY += 25f;
        }

        protected void HandleRoleAutoAssignment(Dialog_CreateClass createClassDialog, ClassRole role, int maxCount)
        {
            var assignedPawns = createClassDialog.AssignmentsManager.AssignedPawns(role).ToList();
            EducationLog.Message($"Handling role auto-assignment for {role} with {assignedPawns.Count} assigned pawns and a max count of {maxCount}");
            if (assignedPawns.Count > maxCount)
            {
                int pawnsToUnassign = assignedPawns.Count - maxCount;
                for (int i = 0; i < pawnsToUnassign; i++)
                {
                    createClassDialog.AssignmentsManager.TryUnassignAnyRole(assignedPawns[i]);
                }
            }
            else if (assignedPawns.Count < maxCount)
            {
                int pawnsToAdd = maxCount - assignedPawns.Count;
                var availablePawns = createClassDialog.CandidatePool.AllCandidatePawns.Where(p => !createClassDialog.AssignmentsManager.PawnParticipating(p) && role.CanAcceptPawn(p).Accepted).ToList();
                for (int i = 0; i < pawnsToAdd && i < availablePawns.Count; i++)
                {
                    createClassDialog.AssignmentsManager.TryAssign(availablePawns[i], role, out _);
                }
            }
        }

        public virtual AcceptanceReport ArePrerequisitesMet()
        {
            string benchLabel = BenchLabel;
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
        protected HashSet<ThingDef> _validLearningBenches;
        public virtual JobDef LearningJob => DefsOf.PE_AttendClass;
        public virtual void HandleStudentLifecycleEvents() { }
        public virtual HashSet<ThingDef> GetValidLearningBenches()
        {
            if (_validLearningBenches == null)
            {
                _validLearningBenches = [];
                var allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
                foreach (var def in allDefs)
                {
                    if (def.IsSchoolDesk())
                    {
                        _validLearningBenches.Add(def);
                    }
                }
            }
            return _validLearningBenches;
        }
    }
}
