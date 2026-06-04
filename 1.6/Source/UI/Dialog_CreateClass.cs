using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public class Dialog_CreateClass : Window, IClassDialog
{
    private readonly DaycareClassLogic daycareClassLogic;
    private readonly Map map;
    private readonly PawnClassRoleSelectionWidget participantsDrawer;
    private readonly ProficiencyClassLogic proficiencyClassLogic;
    private readonly SkillClassLogic skillClassLogic;
    private readonly StudyGroup studyGroup;

    private Vector2 scrollPosition = Vector2.zero;

    public Dialog_CreateClass(Map map)
    {
        this.map = map;
        studyGroup = new StudyGroup(
            null,
            [],
            "",
            10000,
            8,
            15
        );
        TeacherRole = studyGroup.GetTeacherRole();
        StudentRole = studyGroup.GetStudentRole();
        AssignmentsManager = new ClassAssignmentsManager(TeacherRole,
            StudentRole, map);
        CandidatePool = new ClassCandidatePool(map);
        participantsDrawer = new PawnClassRoleSelectionWidget(CandidatePool,
            AssignmentsManager)
        {
            studyGroup = studyGroup,
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

        closeOnAccept = false;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = true;
        forcePause = true;
        AssignmentsManager.FillPawns();
    }

    public override Vector2 InitialSize => new(845f, 740f);
    public ClassAssignmentsManager AssignmentsManager { get; }

    public TeacherRole TeacherRole { get; }

    public StudentRole StudentRole { get; }

    public ClassCandidatePool CandidatePool { get; }

    public override void DoWindowContents(Rect inRect)
    {
        var enter = Event.current.type == EventType.KeyDown
                      && (Event.current.keyCode == KeyCode.Return
                          || Event.current.keyCode == KeyCode.KeypadEnter);
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f),
            "PE_CreateClass".Translate());
        Text.Font = GameFont.Small;
        var descriptionRect =
            new Rect(inRect.x, inRect.y + 35f, inRect.width, 40f);
        Widgets.Label(descriptionRect, "PE_CreateClassDesc".Translate());
        var contentRect = new Rect(inRect.x, inRect.y + 80f, inRect.width,
            inRect.height - 125f);
        var leftRect = new Rect(contentRect.x, contentRect.y,
            contentRect.width * 0.5f - 5f,
            contentRect.height);
        var rightRect = new Rect(contentRect.x + contentRect.width * 0.5f + 5f, contentRect.y,
            contentRect.width * 0.5f - 5f,
            contentRect.height);
        participantsDrawer.DrawPawnList(leftRect);
        DrawCustomFields(rightRect);
        var buttonY = inRect.height - 35f;
        if (Widgets.ButtonText(new Rect(inRect.x, buttonY, 150f, 35f),
                "Cancel".Translate()))
        {
            Close();
        }

        if (Widgets.ButtonText(
                new Rect(inRect.width - 150f, buttonY, 150f, 35f),
                "PE_Create".Translate())
            || enter)
        {
            StartClassCreation();
        }
    }

    private void DrawCustomFields(Rect viewRect)
    {
        var curY = viewRect.y;
        Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f),
            "PE_ClassName".Translate());
        studyGroup.RenamableLabel =
            Widgets.TextField(
                new Rect(viewRect.x + 160f, curY, 200f, 25f),
                studyGroup.className);
        curY += 30f;
        Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f),
            "PE_Subject".Translate());
        if (Widgets.ButtonText(
                new Rect(viewRect.x + 160f, curY, 200f, 25f),
                studyGroup.subjectLogic.LabelCap))
        {
            var options = new List<FloatMenuOption>
                {
                    new(skillClassLogic.LabelCap, () =>
                    {
                        studyGroup.subjectLogic = skillClassLogic;
                        studyGroup.semesterGoal = 10000;
                        studyGroup.subjectLogic.UnassignParticipants(this);
                    }),
                };

            if (EducationMod.settings.enableProficiencySystem)
            {
                options.Add(new FloatMenuOption(proficiencyClassLogic.LabelCap, () =>
                {
                    studyGroup.subjectLogic = proficiencyClassLogic;
                    studyGroup.semesterGoal = proficiencyClassLogic.targetTier.semesterGoal;
                    studyGroup.subjectLogic.UnassignParticipants(this);
                }));
            }

            options.Add(new FloatMenuOption(daycareClassLogic.LabelCap, () =>
            {
                studyGroup.subjectLogic = daycareClassLogic;
                studyGroup.subjectLogic.UnassignParticipants(this);
            }));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        curY += 30f;
        studyGroup.subjectLogic.DrawConfigurationUI(viewRect, ref curY,
            this);
        DrawRequirements(viewRect, ref curY);

        Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f),
            "PE_Classroom".Translate());
        if (Widgets.ButtonText(
                new Rect(viewRect.x + 160f, curY, 200f, 25f),
                studyGroup.classroom?.name ?? "PE_SelectClassroom".Translate()))
        {
            var options = EducationManager.Instance.Classrooms
                .OrderBy(c => c.name)
                .Select(c => new FloatMenuOption(c.name, () =>
            {
                studyGroup.classroom = c;
                ValidateAndRemovePawns();
            }))
                .ToList();

            if (options.Count > 0)
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
            else
            {
                Messages.Message("PE_NoClassroomsAvailable".Translate(),
                    MessageTypeDefOf.RejectInput);
            }
        }

        curY += 30f;
        Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f),
            "PE_ClassSpeed".Translate());
        Widgets.Label(new Rect(viewRect.x + 160f, curY, 200f, 25f),
            studyGroup.classroom?.ClassSpeed.ToStringPercent());
        curY += 30f;
        Widgets.Label(new Rect(viewRect.x, curY, 150f, 25f),
            "PE_ClassHours".Translate());
        if (Widgets.ButtonText(
                new Rect(viewRect.x + 160f, curY, 90f, 25f),
                studyGroup.startHour.ToString()))
        {
            var options = GenerateHourSelectionOptions(hour =>
            {
                studyGroup.startHour = hour;
                ValidateAndRemovePawns();
            });
            Find.WindowStack.Add(new FloatMenu(options));
        }

        if (Widgets.ButtonText(
                new Rect(viewRect.x + 270f, curY, 90f, 25f),
                studyGroup.endHour.ToString()))
        {
            var options = GenerateHourSelectionOptions(hour =>
            {
                studyGroup.endHour = hour;
                ValidateAndRemovePawns();
            });
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private void DrawRequirements(Rect viewRect, ref float curY)
    {
        var requirements = new StringBuilder();
        studyGroup.subjectLogic.AddRequirements(requirements);
        if (studyGroup.classroom != null)
        {
            var learningBoardCount = studyGroup.classroom.LearningBoard != null ? 1 : 0;
            var learningBoardPresentText = "";
            if (learningBoardCount < 1)
            {
                learningBoardPresentText =
                    $" {"PE_Present".Translate(learningBoardCount)}".Colorize(ColoredText
                        .ThreatColor);
            }

            requirements.AppendLineTagged($"1x {"PE_LearningBoard".Translate()}{learningBoardPresentText}");
        }

        if (!EducationUtility.HasBellOnMap(map, false))
        {
            var bellNotPresentText =
                $" {"PE_NotPresent".Translate()}".Colorize(ColoredText.ThreatColor);
            requirements.AppendLineTagged($"1x {"PE_Bell".Translate()}{bellNotPresentText}");
        }
        else
        {
            requirements.AppendLine($"1x {"PE_Bell".Translate()}");
        }

        var fullRequirementsText = requirements.ToString();
        Widgets.Label(new Rect(viewRect.x, curY, viewRect.width, 25f),
            "PE_Requirements".Translate());
        var requirementHeight = Mathf.Min(400f,
            Text.CalcHeight(fullRequirementsText, viewRect.width - 160f - 24f));
        var requirementsTextRect =
            new Rect(viewRect.x + 160f, curY, viewRect.width - 160f,
                requirementHeight);
        Widgets.LabelScrollable(requirementsTextRect, fullRequirementsText,
            ref scrollPosition);
        curY += requirementHeight;
    }

    private static List<FloatMenuOption> GenerateHourSelectionOptions(Action<int> onHourSelected)
    {
        return Enumerable.Range(0, 24)
            .Select(i => new FloatMenuOption(i.ToString(), () => onHourSelected(i)))
            .ToList();
    }

    private void StartClassCreation()
    {
        studyGroup.teacher = AssignmentsManager.FirstAssignedPawn(TeacherRole);
        studyGroup.students = AssignmentsManager.AssignedPawns(StudentRole).ToList();
        var validity = studyGroup.IsValid();
        if (!validity.Accepted)
        {
            Messages.Message(validity.Reason, MessageTypeDefOf.RejectInput);
            return;
        }

        if (studyGroup.className.NullOrEmpty())
        {
            studyGroup.className = $"{studyGroup.teacher.LabelShortCap} ({studyGroup.subjectLogic.LabelFocus})";
        }

        if (studyGroup.subjectLogic is SkillClassLogic classLogic
            && classLogic.SkillFocus == null)
        {
            Messages.Message("PE_SkillFocusMissing".Translate(),
                MessageTypeDefOf.RejectInput);
            return;
        }

        if (EducationManager.Instance.StudyGroups
                .FirstOrDefault(group =>
                    group.classroom == studyGroup.classroom
                    && studyGroup.HasConflict(group)
                    && !group.suspended
                )
            is { } otherGroup)
        {
            Messages.Message(
                "PE_CannotSchedule".Translate(
                    otherGroup.className,
                    otherGroup.startHour,
                    otherGroup.endHour,
                    otherGroup.classroom.name),
                MessageTypeDefOf.RejectInput
            );
            return;
        }

        var prerequisitesMet = studyGroup.ArePrerequisitesMet();
        if (!prerequisitesMet.Accepted)
        {
            Messages.Message(prerequisitesMet.Reason, MessageTypeDefOf.RejectInput);
            return;
        }

        if (EducationManager.Instance.Classrooms.Count == 0)
        {
            Messages.Message("PE_NoClassroomsAvailable".Translate(),
                MessageTypeDefOf.RejectInput);
            return;
        }

        EducationManager.Instance.AddStudyGroup(studyGroup);
        TimeAssignmentUtility.GenerateTimeAssignmentDef(studyGroup);
        EducationManager.ApplyScheduleToPawns(studyGroup);
        if (studyGroup.students.Count < 1)
        {
            studyGroup.Suspend(true);
        }
        Close();
    }

    private void ValidateAndRemovePawns()
    {
        if (studyGroup.teacher != null)
        {
            var reason =
                AssignmentsManager.PawnNotAssignableReason(studyGroup.teacher,
                    TeacherRole);
            if (!reason.NullOrEmpty())
            {
                AssignmentsManager.Unassign(studyGroup.teacher, TeacherRole);
                Messages.Message(reason, MessageTypeDefOf.RejectInput);
            }
        }

        List<Pawn> studentsToRemove = [];
        foreach (var student in studyGroup.students)
        {
            var reason =
                AssignmentsManager.PawnNotAssignableReason(student, StudentRole);
            if (reason.NullOrEmpty())
            {
                continue;
            }

            studentsToRemove.Add(student);
            Messages.Message(reason, MessageTypeDefOf.RejectInput);
        }

        foreach (var student in studentsToRemove)
        {
            AssignmentsManager.Unassign(student, StudentRole);
        }
    }

    public override void WindowUpdate()
    {
        base.WindowUpdate();
        participantsDrawer.WindowUpdate();
        var teacher = AssignmentsManager.FirstAssignedPawn(TeacherRole);
        var students = AssignmentsManager.AssignedPawns(StudentRole).ToList();
        studyGroup.teacher = teacher;
        studyGroup.students = students;
    }
}
