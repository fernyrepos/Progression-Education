using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace ProgressionEducation
{
    [HotSwappable]
    public class Dialog_EditClass : Window, IClassDialog
    {
        private readonly StudyGroup referenceStudyGroup;
        private readonly StudyGroup studyGroup;
        private readonly Map map;
        private readonly TeacherRole teacherRole;
        private readonly StudentRole studentRole;
        private readonly ClassAssignmentsManager assignmentsManager;
        private readonly ClassCandidatePool candidatePool;
        private readonly PawnClassRoleSelectionWidget participantsDrawer;
        public ClassAssignmentsManager AssignmentsManager => assignmentsManager;
        public TeacherRole TeacherRole => teacherRole;
        public StudentRole StudentRole => studentRole;
        public ClassCandidatePool CandidatePool => candidatePool;

        public Dialog_EditClass(StudyGroup studyGroup)
        {
            referenceStudyGroup = studyGroup;
            this.studyGroup = new StudyGroup(studyGroup);
            map = studyGroup.Map;
            teacherRole = studyGroup.GetTeacherRole();
            studentRole = studyGroup.GetStudentRole();
            var forcedRoles = studyGroup.subjectLogic switch
            {
                SkillClassLogic
                or ProficiencyClassLogic => new Dictionary<string, Pawn>
                {
                    [teacherRole.RoleId] = studyGroup.teacher
                },
                _ => null,
            };
            assignmentsManager = new ClassAssignmentsManager(teacherRole, studentRole, map, forcedRoles);
            candidatePool = new ClassCandidatePool(map);
            participantsDrawer = new PawnClassRoleSelectionWidget(candidatePool, assignmentsManager)
            {
                studyGroup = studyGroup
            };

            closeOnAccept = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            forcePause = true;

            assignmentsManager.FillRole(studentRole, studyGroup.students, out _);
            assignmentsManager.FillRole(teacherRole, [studyGroup.teacher], out _);
        }

        public override Vector2 InitialSize => new(845f, 740f);

        public override void DoWindowContents(Rect inRect)
        {
            var enter = Event.current.type == EventType.KeyDown 
                        && (Event.current.keyCode == KeyCode.Return 
                            || Event.current.keyCode == KeyCode.KeypadEnter);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "PE_EditClass".Translate());
            Text.Font = GameFont.Small;
            var descriptionRect = new Rect(inRect.x, inRect.y + 35f, inRect.width, 40f);
            Widgets.Label(descriptionRect, "PE_EditClassDesc".Translate());
            var contentRect = new Rect(inRect.x, inRect.y + 80f, inRect.width, inRect.height - 125f);
            var leftRect = new Rect(contentRect.x, contentRect.y, (contentRect.width * 0.5f) - 5f, contentRect.height);
            var rightRect = new Rect(contentRect.x + (contentRect.width * 0.5f) + 5f, contentRect.y, (contentRect.width * 0.5f) - 5f, contentRect.height);
            participantsDrawer.DrawPawnList(leftRect);
            DrawCustomFields(rightRect);
            var buttonY = inRect.height - 35f;
            if (Widgets.ButtonText(new Rect(inRect.x, buttonY, 150f, 35f), "Cancel".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 150f, buttonY, 150f, 35f), "Save".Translate()) || enter)
            {
                StartClassEditing();
            }
        }

        private void StartClassEditing()
        {
            var teacher = assignmentsManager.FirstAssignedPawn(teacherRole);
            var students = assignmentsManager.AssignedPawns(studentRole).ToList();
            studyGroup.teacher = teacher;
            studyGroup.students = students;
            var validity = studyGroup.IsValid();
            if (!validity.Accepted)
            {
                Messages.Message(validity.Reason, MessageTypeDefOf.RejectInput);
                return;
            }
            if (studyGroup.className.NullOrEmpty())
            {
                studyGroup.className = teacher.LabelShortCap;
            }
            if (studyGroup.subjectLogic is SkillClassLogic { SkillFocus: null })
            {
                Messages.Message("PE_SkillFocusMissing".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            var educationManager = EducationManager.Instance;
            var studyGroups = educationManager.StudyGroups;
            foreach (var otherGroup in studyGroups.Except(referenceStudyGroup))
            {
                if (otherGroup.classroom != studyGroup.classroom
                    || !studyGroup.HasConflict(otherGroup))
                {
                    continue;
                }

                Messages.Message("PE_CannotSchedule".Translate(otherGroup.className, otherGroup.startHour, otherGroup.endHour, otherGroup.classroom.name), MessageTypeDefOf.RejectInput);
                return;
            }
            var prerequisitesMet = studyGroup.ArePrerequisitesMet();
            if (!prerequisitesMet.Accepted)
            {
                Messages.Message(prerequisitesMet.Reason, MessageTypeDefOf.RejectInput);
                return;
            }
            if (educationManager.Classrooms.Count == 0)
            {
                Messages.Message("PE_NoClassroomsAvailable".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            var removedStudents = referenceStudyGroup.students.Except(studyGroup.students).ToList();
            foreach (var student in removedStudents)
            {
                referenceStudyGroup.RemoveStudent(student);
            }
            referenceStudyGroup.CancelClass();
            List<Pawn> allPriorParticipants = [referenceStudyGroup.teacher, .. referenceStudyGroup.students];
            TimeAssignmentUtility.ClearScheduleFromPawns(referenceStudyGroup, allPriorParticipants);
            referenceStudyGroup.teacher = studyGroup.teacher;
            referenceStudyGroup.students = [.. studyGroup.students];
            referenceStudyGroup.className = studyGroup.className;
            referenceStudyGroup.startHour = studyGroup.startHour;
            referenceStudyGroup.endHour = studyGroup.endHour;
            referenceStudyGroup.classroom = studyGroup.classroom;
            referenceStudyGroup.semesterGoal = studyGroup.semesterGoal;
            referenceStudyGroup.Suspend(referenceStudyGroup.suspended || referenceStudyGroup.students.Count == 0);
            EducationManager.ApplyScheduleToPawns(referenceStudyGroup);
            Close();
        }

        private void DrawCustomFields(Rect viewRect)
        {
            var curY = viewRect.y;
            Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f), "PE_ClassName".Translate());
            studyGroup.RenamableLabel = Widgets.TextField(new Rect(viewRect.x + 160f, curY, 200f, 25f), studyGroup.className);
            curY += 30f;
            Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f), "PE_Subject".Translate());
            var subjectLabel = studyGroup.subjectLogic switch
            {
                ProficiencyClassLogic => "PE_SubjectProficiency".Translate(),
                DaycareClassLogic => "PE_SubjectDaycare".Translate(),
                SkillClassLogic => "PE_SubjectSkill".Translate(),
                _ => "NoneBrackets".Translate(),
            };
            Widgets.Label(new Rect(viewRect.x + 160f, curY, 200f, 25f), subjectLabel);
            curY += 30f;
            studyGroup.subjectLogic.DrawConfigurationUI(viewRect, ref curY, this);
            DrawRequirements(viewRect, ref curY);

            Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f), "PE_Classroom".Translate());
            if (Widgets.ButtonText(new Rect(viewRect.x + 160f, curY, 200f, 25f), studyGroup.classroom?.name ?? "PE_SelectClassroom".Translate()))
            {
                List<FloatMenuOption> options =
                [
                    .. EducationManager.Instance.Classrooms
                        .Select(classroom => new FloatMenuOption(classroom.name, () =>
                        {
                            studyGroup.classroom = classroom;
                            ValidateAndRemovePawns();
                        }))
                ];
                if (options.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                else
                {
                    Messages.Message("PE_NoClassroomsAvailable".Translate(), MessageTypeDefOf.RejectInput);
                }
            }
            curY += 30f;
            Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f), "PE_ClassSpeed".Translate());
            Widgets.Label(new Rect(viewRect.x + 160f, curY, 200f, 25f), studyGroup.classroom?.CalculateLearningModifier().ToStringPercent());
            curY += 30f;
            Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f), "PE_ClassHours".Translate());
            if (Widgets.ButtonText(new Rect(viewRect.x + 160f, curY, 90f, 25f), studyGroup.startHour.ToString()))
            {
                var options = GenerateHourSelectionOptions(hour =>
                {
                    studyGroup.startHour = hour;
                    ValidateAndRemovePawns();
                });
                Find.WindowStack.Add(new FloatMenu(options));
            }
            if (Widgets.ButtonText(new Rect(viewRect.x + 270f, curY, 90f, 25f), studyGroup.endHour.ToString()))
            {
                var options = GenerateHourSelectionOptions(hour =>
                {
                    studyGroup.endHour = hour;
                    ValidateAndRemovePawns();
                });
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }


        public override void WindowUpdate()
        {
            base.WindowUpdate();
            participantsDrawer.WindowUpdate();
            var teacher = assignmentsManager.FirstAssignedPawn(teacherRole);
            var students = assignmentsManager.AssignedPawns(studentRole).ToList();
            studyGroup.teacher = teacher;
            studyGroup.students = students;
        }

        private static List<FloatMenuOption> GenerateHourSelectionOptions(System.Action<int> onHourSelected)
        {
            return [.. Enumerable.Range(0, 24)
                .Select(i => new FloatMenuOption(i.ToString(), () => onHourSelected(i)))
            ];
        }

        private Vector2 scrollPosition = Vector2.zero;
        private void DrawRequirements(Rect viewRect, ref float curY)
        {
            var requirements = new List<string>();
            studyGroup.subjectLogic.AddRequirements(requirements);
            if (studyGroup.classroom != null)
            {
                var learningBoardCount = studyGroup.classroom.LearningBoard != null ? 1 : 0;
                var learningBoardPresentText = "";
                if (learningBoardCount < 1)
                {
                    var oldColor = GUI.color;
                    GUI.color = Color.red;
                    learningBoardPresentText = $" {"PE_Present".Translate(learningBoardCount)}";
                    GUI.color = oldColor;
                }
                requirements.Add($"1x {"PE_LearningBoard".Translate()}{learningBoardPresentText}");
            }
            if (!EducationUtility.HasBellOnMap(map, false))
            {
                var oldColor = GUI.color;
                GUI.color = Color.red;
                var bellNotPresentText = $" {"PE_NotPresent".Translate()}";
                GUI.color = oldColor;
                requirements.Add($"1x {"PE_Bell".Translate()}{bellNotPresentText}");
            }
            else
            {
                requirements.Add($"1x {"PE_Bell".Translate()}");
            }

            var fullRequirementsText = string.Join("\n", requirements);
            Widgets.Label(new Rect(viewRect.x, curY, viewRect.width, 25f), "PE_Requirements".Translate());
            var requirementHeight = Mathf.Min(400, Text.CalcHeight(fullRequirementsText, viewRect.width - 160f));
            var requirementsTextRect = new Rect(viewRect.x + 160f, curY, viewRect.width - 160f, requirementHeight);
            Widgets.LabelScrollable(requirementsTextRect, fullRequirementsText, ref scrollPosition);
            curY += requirementHeight;
        }

        private void ValidateAndRemovePawns()
        {
            if (studyGroup.teacher != null)
            {
                var report = teacherRole.CanAcceptPawn(studyGroup.teacher);
                if (!report.Accepted)
                {
                    assignmentsManager.Unassign(studyGroup.teacher, teacherRole);
                    Messages.Message(report.Reason, MessageTypeDefOf.RejectInput);
                }
            }

            List<Pawn> studentsToRemove = [];
            foreach (var student in studyGroup.students)
            {
                var report = studentRole.CanAcceptPawn(student);
                if (report.Accepted)
                {
                    continue;
                }

                studentsToRemove.Add(student);
                Messages.Message(report.Reason, MessageTypeDefOf.RejectInput);
            }

            foreach (var student in studentsToRemove)
            {
                assignmentsManager.Unassign(student, studentRole);
            }
        }
    }
}
