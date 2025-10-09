using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class Dialog_CreateClass : Window
    {
        private readonly StudyGroup studyGroup;
        private readonly Map map;
        private readonly TeacherRole teacherRole;
        private readonly StudentRole studentRole;
        private readonly ClassAssignmentsManager assignmentsManager;
        private readonly ClassCandidatePool candidatePool;
        private readonly PawnClassRoleSelectionWidget participantsDrawer;
        private readonly SkillClassLogic skillClassLogic;
        private readonly ProficiencyClassLogic proficiencyClassLogic;
        private readonly DaycareClassLogic daycareClassLogic;
        public ClassAssignmentsManager AssignmentsManager => assignmentsManager;
        public TeacherRole TeacherRole => teacherRole;
        public StudentRole StudentRole => studentRole;
        public ClassCandidatePool CandidatePool => candidatePool;

        public Dialog_CreateClass(Map map)
        {
            this.map = map;
            studyGroup = new StudyGroup(
                null,
                [],
                "",
                1000,
                8,
                10
            );
            teacherRole = studyGroup.GetTeacherRole();
            studentRole = studyGroup.GetStudentRole();
            assignmentsManager = new ClassAssignmentsManager(teacherRole, studentRole, map);
            candidatePool = new ClassCandidatePool(map);
            participantsDrawer = new PawnClassRoleSelectionWidget(candidatePool, assignmentsManager)
            {
                studyGroup = studyGroup
            };
            skillClassLogic = new SkillClassLogic(studyGroup);
            proficiencyClassLogic = new ProficiencyClassLogic(studyGroup);
            daycareClassLogic = new DaycareClassLogic(studyGroup);
            studyGroup.subjectLogic = skillClassLogic;

            var educationManager = EducationManager.Instance;
            if (educationManager.Classrooms.Count > 0)
            {
                studyGroup.classroom = educationManager.Classrooms[0];
            }
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            forcePause = true;
            assignmentsManager.FillPawns();
            studyGroup.subjectLogic.AssignBestTeacher(this);
        }

        public override Vector2 InitialSize => new(845f, 740f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "PE_CreateClass".Translate());
            Text.Font = GameFont.Small;
            var descriptionRect = new Rect(inRect.x, inRect.y + 35f, inRect.width, 40f);
            Widgets.Label(descriptionRect, "PE_CreateClassDesc".Translate());
            var contentRect = new Rect(inRect.x, inRect.y + 80f, inRect.width, inRect.height - 125f);
            var leftRect = new Rect(contentRect.x, contentRect.y, (contentRect.width * 0.5f) - 5f, contentRect.height);
            var rightRect = new Rect(contentRect.x + (contentRect.width * 0.5f) + 5f, contentRect.y, (contentRect.width * 0.5f) - 5f, contentRect.height);
            participantsDrawer.DrawPawnList(leftRect);
            DrawCustomFields(rightRect);
            float buttonY = inRect.height - 35f;
            if (Widgets.ButtonText(new Rect(inRect.x, buttonY, 150f, 35f), "Cancel".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.width - 150f, buttonY, 150f, 35f), "PE_Create".Translate()))
            {
                StartClassCreation();
            }
        }

        private void StartClassCreation()
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
            if (studyGroup.subjectLogic is SkillClassLogic skillLogic && skillLogic.skillFocus == null)
            {
                Messages.Message("PE_SelectSkillFocusError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            var prerequisitesMet = studyGroup.ArePrerequisitesMet();
            if (!prerequisitesMet.Accepted)
            {
                Messages.Message(prerequisitesMet.Reason, MessageTypeDefOf.RejectInput);
                return;
            }
            var educationManager = EducationManager.Instance;
            if (educationManager.Classrooms.Count == 0)
            {
                Messages.Message("PE_NoClassroomsAvailable".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            educationManager.AddStudyGroup(studyGroup);

            TimeAssignmentUtility.GenerateTimeAssignmentDef(studyGroup);
            educationManager.ApplyScheduleToPawns(studyGroup);
            Close();
        }

        private void DrawCustomFields(Rect viewRect)
        {
            float curY = viewRect.y;
            Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f), "PE_ClassName".Translate());
            studyGroup.RenamableLabel = Widgets.TextField(new Rect(viewRect.x + 160f, curY, 200f, 25f), studyGroup.className);
            curY += 30f;
            Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f), "PE_Subject".Translate());
            string subjectLabel = "PE_SubjectSkill".Translate();
            if (studyGroup.subjectLogic is ProficiencyClassLogic)
            {
                subjectLabel = "PE_SubjectProficiency".Translate();
            }
            else if (studyGroup.subjectLogic is DaycareClassLogic)
            {
                subjectLabel = "PE_SubjectDaycare".Translate();
            }
            if (Widgets.ButtonText(new Rect(viewRect.x + 160f, curY, 200f, 25f), subjectLabel))
            {
                List<FloatMenuOption> options =
                [
                    new FloatMenuOption("PE_SubjectSkill".Translate(), () => {
                        studyGroup.subjectLogic = skillClassLogic;
                        studyGroup.subjectLogic.AutoAssignStudents(this);
                    }),
                    new FloatMenuOption("PE_SubjectProficiency".Translate(), () => {
                        studyGroup.subjectLogic = proficiencyClassLogic;
                        if (proficiencyClassLogic.proficiencyFocus == ProficiencyLevel.Firearm)
                        {
                            studyGroup.semesterGoal = ProficiencyClassLogic.FirearmTeachingDuration;
                        }
                        else
                        {
                            studyGroup.semesterGoal = ProficiencyClassLogic.HighTechTeachingDuration;
                        }
                        studyGroup.subjectLogic.AutoAssignStudents(this);
                    }),
                ];
                if (ModsConfig.BiotechActive)
                {
                    options.Add(
                    new FloatMenuOption("PE_SubjectDaycare".Translate(), () =>
                    {
                        studyGroup.subjectLogic = daycareClassLogic;
                        studyGroup.subjectLogic.AutoAssignStudents(this);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            curY += 30f;
            studyGroup.subjectLogic.DrawConfigurationUI(viewRect, ref curY, map, this);
            DrawGeneralRequirements(viewRect, ref curY);

            Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f), "PE_Classroom".Translate());
            if (Widgets.ButtonText(new Rect(viewRect.x + 160f, curY, 200f, 25f), studyGroup.classroom?.name ?? "PE_SelectClassroom".Translate()))
            {
                var educationManager = EducationManager.Instance;
                List<FloatMenuOption> options = [];
                foreach (var classroom in educationManager.Classrooms)
                {
                    options.Add(new FloatMenuOption(classroom.name, () =>
                    {
                        studyGroup.classroom = classroom;
                        studyGroup.subjectLogic.AutoAssignStudents(this);
                    }));
                }
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
            Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f), "PE_ClassHours".Translate());
            if (Widgets.ButtonText(new Rect(viewRect.x + 160f, curY, 90f, 25f), studyGroup.startHour.ToString()))
            {
                var options = GenerateHourSelectionOptions(hour => {
                    studyGroup.startHour = hour;
                    ValidateAndRemovePawns();
                });
                Find.WindowStack.Add(new FloatMenu(options));
            }
            if (Widgets.ButtonText(new Rect(viewRect.x + 270f, curY, 90f, 25f), studyGroup.endHour.ToString()))
            {
                var options = GenerateHourSelectionOptions(hour => {
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


        public static List<FloatMenuOption> GenerateHourSelectionOptions(System.Action<int> onHourSelected)
        {
            List<FloatMenuOption> options = [];
            for (int i = 0; i < 24; i++)
            {
                int hour = i;
                options.Add(new FloatMenuOption(hour.ToString(), () => onHourSelected(hour)));
            }
            return options;
        }

        private void DrawGeneralRequirements(Rect viewRect, ref float curY)
        {
            if (studyGroup.classroom != null)
            {
                int learningBoardCount = studyGroup.classroom.LearningBoard != null ? 1 : 0;
                string learningBoardPresentText = "";
                if (learningBoardCount < 1)
                {
                    GUI.color = Color.red;
                    learningBoardPresentText = " " + "PE_Present".Translate(learningBoardCount);
                }
                Widgets.Label(new Rect(viewRect.x + 10f, curY, 300f, 25f), $"1x {"PE_LearningBoard".Translate()}{learningBoardPresentText}");
                GUI.color = Color.white;
                curY += 25f;
            }
            if (!EducationUtility.HasBellOnMap(map, false))
            {
                GUI.color = Color.red;
                Widgets.Label(new Rect(viewRect.x + 10f, curY, 300f, 25f), $"1x {"PE_Bell".Translate()} ({"PE_NotPresent".Translate()})");
                GUI.color = Color.white;
            }
            else
            {
                Widgets.Label(new Rect(viewRect.x + 10f, curY, 300f, 25f), $"1x {"PE_Bell".Translate()}");
            }
            curY += 30f;
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
                if (!report.Accepted)
                {
                    studentsToRemove.Add(student);
                    Messages.Message(report.Reason, MessageTypeDefOf.RejectInput);
                }
            }

            foreach (var student in studentsToRemove)
            {
                assignmentsManager.Unassign(student, studentRole);
            }
        }
    }
}
