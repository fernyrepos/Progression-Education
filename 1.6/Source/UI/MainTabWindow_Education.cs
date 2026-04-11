using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
[StaticConstructorOnStartup]
public class MainTabWindow_Education : MainTabWindow
{
    private const float ClassroomRowHeight = 38f;
    private const float ClassRowHeight = 90f;
    private const float ElementPadding = 2f;
    private const float HeaderHeight = 35f;
    private const float TeacherPortraitSize = ClassRowHeight - WindowPadding;
    private const float ToolbarButtonSize = 30f;
    private const float WindowPadding = 12f;

    private static readonly Texture2D AttendanceFillTexture =
        SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.33f, 0.72f));

    private static readonly Texture2D DeleteIcon = TexButton.Delete;

    private static readonly Texture2D GearIcon = ContentFinder<Texture2D>.Get("UI/GearIcon");

    private static readonly Texture2D ProgressBarFillTexture =
        SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.72f, 0.33f));

    private static readonly Texture2D
        RenameIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");

    private static readonly Texture2D ResumeIcon = ContentFinder<Texture2D>.Get("UI/Unpause");

    private Vector2 classroomScrollPosition = Vector2.zero;
    private Vector2 classScrollPosition = Vector2.zero;

    public override void DoWindowContents(Rect inRect)
    {
        var leftRect = new Rect(inRect.x, inRect.y,
            inRect.width * 0.65f - WindowPadding / 2f,
            inRect.height);
        var rightRect = new Rect(leftRect.xMax + WindowPadding, inRect.y,
            inRect.width * 0.35f - WindowPadding / 2f,
            inRect.height);

        DrawClassList(leftRect);
        DrawClassroomList(rightRect);
    }

    private void DrawBanner(Rect rect, ref float curY)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(
            new Rect(rect.x, curY, rect.width - HeaderHeight,
                HeaderHeight),
            "PE_Classes".Translate());
        Text.Font = GameFont.Small;
        var addButtonRect = new Rect(rect.xMax - HeaderHeight, curY, HeaderHeight,
            HeaderHeight);
        if (Widgets.ButtonImage(addButtonRect, TexButton.Plus))
        {
            if (!EducationUtility.HasBellOnMap(Find.CurrentMap, false))
            {
                Messages.Message("PE_NoBellToCreateClass".Translate(),
                    MessageTypeDefOf.RejectInput);
            }
            else if (EducationManager.Instance.Classrooms.Count == 0)
            {
                Messages.Message("PE_CreateClassroomFirst".Translate(),
                    MessageTypeDefOf.RejectInput);
            }
            else
            {
                Find.WindowStack.Add(new Dialog_CreateClass(Find.CurrentMap));
            }
        }

        curY += HeaderHeight;
    }

    private void DrawClassDescription(Rect rect, StudyGroup studyGroup, ref float curX,
        ref float curY)
    {
        var restoreFont = Text.Font;
        var restoreAnchor = Text.Anchor;
        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.UpperLeft;
        var descriptionRect = new Rect(curX, curY, rect.width - curX, 24f);
        curY += 24;
        Widgets.Label(descriptionRect, studyGroup.subjectLogic.Description);
        Text.Font = restoreFont;
        Text.Anchor = restoreAnchor;
    }

    private void DrawClassHeader(Rect rect, StudyGroup studyGroup, ref float curX, ref float curY)
    {
        var restoreFont = Text.Font;
        var restoreAnchor = Text.Anchor;
        var toolbarPadding = ToolbarButtonSize + ElementPadding;
        var toolX = rect.xMax - ToolbarButtonSize;
        var deleteButtonRect = new Rect(toolX, curY, ToolbarButtonSize,
            ToolbarButtonSize);
        toolX -= toolbarPadding;
        if (Widgets.ButtonImage(deleteButtonRect, DeleteIcon))
        {
            Find.WindowStack.Add(new Dialog_Confirm("PE_ConfirmDeleteClass".Translate(),
                () => EducationManager.Instance.RemoveStudyGroup(studyGroup)));
            return;
        }

        TooltipHandler.TipRegion(deleteButtonRect, "PE_DeleteClass".Translate());
        var expandButtonRect = new Rect(toolX, curY, ToolbarButtonSize,
            ToolbarButtonSize);
        toolX -= toolbarPadding;
        if (Widgets.ButtonImage(expandButtonRect, TexButton.ToggleLog))
        {
            Find.WindowStack.Add(new Dialog_EditClass(studyGroup));
        }

        TooltipHandler.TipRegion(expandButtonRect, "PE_ToggleStudentList".Translate());
        var suspendButtonRect = new Rect(toolX, curY, ToolbarButtonSize,
            ToolbarButtonSize);
        toolX -= toolbarPadding;
        var suspendIcon = studyGroup.suspended ? ResumeIcon : TexButton.Suspend;
        var suspendTooltip = studyGroup.suspended
            ? "PE_ResumeClass".Translate()
            : "PE_SuspendClass".Translate();
        GUI.DrawTexture(suspendButtonRect, suspendIcon);
        TooltipHandler.TipRegion(suspendButtonRect, suspendTooltip);
        if (Widgets.ButtonImage(suspendButtonRect, suspendIcon))
        {
            studyGroup.Suspend(!studyGroup.suspended);
        }

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.UpperLeft;
        var name = studyGroup.className;
        if (studyGroup.suspended)
        {
            name += " " + "PE_Suspended".Translate();
        }

        var nameRect = new Rect(curX, curY, toolX - curX, ToolbarButtonSize);
        Widgets.LabelEllipses(nameRect, name);
        curY += ToolbarButtonSize;
        Text.Font = restoreFont;
        Text.Anchor = restoreAnchor;
    }

    private void DrawClassList(Rect rect)
    {
        var curY = rect.y;
        DrawBanner(rect, ref curY);
        curY += WindowPadding;
        var listOutRect = new Rect(rect.x, curY, rect.width,
            rect.height - curY - WindowPadding);
        var studyGroups = EducationManager.Instance.StudyGroups;

        if (studyGroups.Count == 0)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(listOutRect, "PE_NoClassesScheduled".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            return;
        }

        var totalHeight = studyGroups.Count * ClassRowHeight;
        var listContentRect =
            new Rect(0f, 0f, listOutRect.width - 16f, totalHeight);
        Widgets.BeginScrollView(listOutRect, ref classScrollPosition,
            listContentRect);
        var scrollY = 0f;
        for (var i = 0; i < studyGroups.Count; i++)
        {
            var studyGroup = studyGroups[i];
            var classRect = new Rect(0f, scrollY, listContentRect.width,
                ClassRowHeight);
            DrawClassRow(classRect, studyGroup);
            scrollY += classRect.height + WindowPadding;
        }

        Widgets.EndScrollView();
    }

    private void DrawClassProgress(Rect rect, StudyGroup studyGroup, ref float curX, ref float curY)
    {
        var restoreFont = Text.Font;
        var restoreAnchor = Text.Anchor;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        var progressRect =
            new Rect(curX, curY, rect.width - curX, 24f).LeftPart(0.8f);
        curX += progressRect.width + ElementPadding;
        if (!studyGroup.subjectLogic.IsInfinite)
        {
            var progress = Mathf.Clamp01(studyGroup.ProgressPercentage);
            Widgets.FillableBar(progressRect, progress,
                ProgressBarFillTexture);
            if (studyGroup.subjectLogic is SkillClassLogic)
            {
                Widgets.Label(progressRect,
                    "PE_ProgressFormat".Translate(
                        studyGroup.currentProgress.ToString("F0"),
                        studyGroup.semesterGoal.ToString()));
            }
            else if (studyGroup.subjectLogic is ProficiencyClassLogic)
            {
                Widgets.Label(progressRect,
                    studyGroup.ProgressPercentage.ToStringPercent());
            }
        }
        else
        {
            var maxAttendance = Mathf.Min(studyGroup.GetStudentRole().MaxCount,
                studyGroup.subjectLogic.BenchCount);
            var attendancePercentage = maxAttendance > 0
                ? Mathf.Clamp01((float)studyGroup.students.Count / maxAttendance)
                : 1f;
            Widgets.FillableBar(progressRect, attendancePercentage,
                AttendanceFillTexture);
            Widgets.Label(progressRect,
                "PE_ProgressFormat".Translate(studyGroup.students.Count,
                    maxAttendance));
        }

        Text.Font = restoreFont;
        Text.Anchor = restoreAnchor;
    }

    private void DrawClassroomList(Rect rect)
    {
        var educationManager = EducationManager.Instance;
        var classrooms = educationManager.Classrooms;

        var scheduleButtonRect =
            new Rect(rect.x, rect.y, rect.width, HeaderHeight);
        if (Widgets.ButtonText(scheduleButtonRect, "PE_ClassScheduling".Translate()))
        {
            Find.MainTabsRoot.SetCurrentTab(DefsOf.Schedule);
        }

        Text.Font = GameFont.Medium;
        var headerRect = new Rect(rect.x, rect.y + HeaderHeight, rect.width,
            HeaderHeight);
        var currentY = headerRect.y;
        ListSeparator(headerRect.x, ref currentY, headerRect.width,
            "PE_Classrooms".Translate());
        var descriptionRect = new Rect(rect.x, currentY, rect.width, 40f);
        Text.Font = GameFont.Small;
        Widgets.Label(descriptionRect, "PE_ClassroomsDescription".Translate());

        currentY += 45f;
        var listOutRect = new Rect(rect.x, currentY, rect.width,
            rect.height - currentY);
        var listContentRect = new Rect(0f, 0f, listOutRect.width - 16f,
            classrooms.Count * (ClassroomRowHeight + WindowPadding));

        Widgets.BeginScrollView(listOutRect, ref classroomScrollPosition,
            listContentRect);

        var curY = 0f;
        for (var i = 0; i < classrooms.Count; i++)
        {
            var classroom = classrooms[i];
            var classroomRect = new Rect(0f, curY, listContentRect.width,
                ClassroomRowHeight);

            if (i % 2 == 1)
            {
                Widgets.DrawLightHighlight(classroomRect);
            }

            if (Mouse.IsOver(classroomRect))
            {
                Widgets.DrawHighlight(classroomRect);
            }

            var colorRect = new Rect(classroomRect.x + 4f,
                classroomRect.y + (ClassroomRowHeight - 32f) / 2f, 32f, 32f);
            Widgets.DrawBoxSolid(colorRect, classroom.color);
            if (Widgets.ButtonInvisible(colorRect))
            {
                Find.WindowStack.Add(new Window_ColorPicker(classroom.color,
                    newColor => classroom.color = newColor));
            }

            TooltipHandler.TipRegion(colorRect, "PE_ChangeColor".Translate());

            Text.Anchor = TextAnchor.MiddleLeft;
            var nameRect = new Rect(colorRect.xMax + 8f, classroomRect.y,
                classroomRect.width - 80f,
                classroomRect.height);
            Widgets.Label(nameRect, classroom.name);

            Text.Anchor = TextAnchor.UpperLeft;

            var renameButtonRect = new Rect(classroomRect.xMax - 72f,
                classroomRect.y + (ClassroomRowHeight - 32f) / 2f,
                32f, 32f);
            if (Widgets.ButtonImage(renameButtonRect, RenameIcon))
            {
                Find.WindowStack.Add(new Dialog_RenameClassroom(classroom));
            }

            TooltipHandler.TipRegion(renameButtonRect, "Rename".Translate());

            var settingsButtonRect = new Rect(classroomRect.xMax - 36f,
                classroomRect.y + (ClassroomRowHeight - 32f) / 2f, 32f, 32f);
            if (Widgets.ButtonImage(settingsButtonRect, GearIcon))
            {
                Find.WindowStack.Add(new Dialog_ClassroomSettings(classroom));
            }

            TooltipHandler.TipRegion(settingsButtonRect,
                "PE_ClassroomSettings".Translate());

            if (Widgets.ButtonInvisible(nameRect))
            {
                if (classroom.LearningBoard != null
                    && classroom.LearningBoard.parent != null)
                {
                    Find.Selector.Select(classroom.LearningBoard.parent);
                    Current.Game.CurrentMap = classroom.LearningBoard.parent.Map;
                    Find.CameraDriver.JumpToCurrentMapLoc(classroom.LearningBoard.parent.DrawPos
                                                          + new Vector3(-6f, 0f, 0f));
                }
            }

            curY += ClassroomRowHeight + WindowPadding;
        }

        Widgets.EndScrollView();
    }

    private void DrawClassRow(Rect rect, StudyGroup studyGroup)
    {
        var borderColor = studyGroup.classroom.color;
        var bgColor = Color.Lerp(borderColor, Color.black, 0.65f);
        Widgets.DrawBoxSolidWithOutline(rect, bgColor, borderColor);
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        var innerRect = rect.ContractedBy(WindowPadding / 2f);
        var curX = innerRect.x;
        var curY = innerRect.y;
        DrawClassTeacher(innerRect, studyGroup, ref curX, ref curY);
        DrawClassHeader(innerRect, studyGroup, ref curX, ref curY);
        DrawClassDescription(innerRect, studyGroup, ref curX,
            ref curY);
        DrawClassProgress(innerRect, studyGroup, ref curX, ref curY);
        DrawClassSchedule(innerRect, studyGroup, ref curX, ref curY);
    }

    private void DrawClassSchedule(Rect rect, StudyGroup studyGroup, ref float curX, ref float curY)
    {
        var restoreFont = Text.Font;
        var restoreAnchor = Text.Anchor;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleRight;
        var scheduleRect = new Rect(curX, curY, rect.width - curX, 24f);
        Widgets.Label(scheduleRect,
            "PE_ScheduleTime".Translate(studyGroup.startHour,
                studyGroup.endHour));
        Text.Font = restoreFont;
        Text.Anchor = restoreAnchor;
    }

    private void DrawClassTeacher(Rect _, StudyGroup studyGroup, ref float curX, ref float curY)
    {
        var teacherRect = new Rect(curX, curY, TeacherPortraitSize,
            TeacherPortraitSize);
        curX += TeacherPortraitSize + WindowPadding * 0.5f;
        var material = studyGroup.suspended ? TexUI.GrayscaleGUI : null;
        GenUI.DrawTextureWithMaterial(
            teacherRect,
            PortraitsCache.Get(
                studyGroup.teacher,
                new Vector2(TeacherPortraitSize, TeacherPortraitSize),
                Rot4.South,
                cameraZoom: 1.5f
            ),
            material
        );
        TooltipHandler.TipRegion(teacherRect, studyGroup.teacher.LabelCap);
        if (Mouse.IsOver(teacherRect)
            && Event.current.type == EventType.MouseDown)
        {
            CameraJumper.TryJumpAndSelect(studyGroup.teacher,
                CameraJumper.MovementMode.Cut);
        }
    }

    public static void ListSeparator(float curX, ref float curY, float width, string label)
    {
        var color = GUI.color;
        curY += 3f;
        var rect = new Rect(curX, curY, width, 30f);
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(rect, label);
        curY += 30f;
        GUI.color = Widgets.SeparatorLineColor;
        Widgets.DrawLineHorizontal(curX, curY, width);
        curY += 2f;
        GUI.color = color;
    }
}