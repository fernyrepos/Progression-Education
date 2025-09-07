using System;
using System.Linq;
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
        public abstract void DrawConfigurationUI(Rect rect, ref float curY, Map map, Dialog_CreateClass createClassDialog);
        public abstract float CalculateProgressPerTick();
        public abstract void GrantCompletionRewards();
        public abstract AcceptanceReport IsTeacherQualified(Pawn teacher);
        public abstract AcceptanceReport IsStudentQualified(Pawn student);
        public virtual void ApplyLearningTick(Pawn student)
        {
            if (student.DevelopmentalStage == DevelopmentalStage.Child)
            {
                float growthPointsPerTick = student.ageTracker.GrowthPointsPerDay / 60000f;
                student.ageTracker.growthPoints += growthPointsPerTick;
            }
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
                        assignmentsManager.TryAssign(existingTeacher, createClassDialog.StudentRole, out _);
                    }
                }
                assignmentsManager.TryAssign(bestTeacher, teacherRole, out _);
            }
        }
        public abstract float CalculateTeacherScore(Pawn p);
        public virtual void AutoAssignStudents(Dialog_CreateClass createClassDialog) { }
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
                        Log.Message($"Unassigning {pawn} from {role} because they are no longer qualified: {canAccept.Reason}");
                    }
                }
            }
        }

        protected void HandleRoleAutoAssignment(Dialog_CreateClass createClassDialog, ClassRole role, int maxCount)
        {
            var assignedPawns = createClassDialog.AssignmentsManager.AssignedPawns(role).ToList();
            Log.Message($"Handling role auto-assignment for {role} with {assignedPawns.Count} assigned pawns and a max count of {maxCount}");
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
            return AcceptanceReport.WasAccepted;
        }

        public virtual void ExposeData() { Scribe_References.Look(ref studyGroup, "studyGroup"); }
    }
}
