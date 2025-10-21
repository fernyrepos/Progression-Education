using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class MainTabWindow_Education : MainTabWindow
    {
        private Vector2 classScrollPosition = Vector2.zero;
        private Vector2 classroomScrollPosition = Vector2.zero;
        private const float ClassRowHeight = 90f;
        private const float ClassroomRowHeight = 38f;
        private const float WindowPadding = 12f;
        private const float HeaderHeight = 35f;

        private float TeacherPortraitSize => 50f;
        private static readonly Texture2D ParticipantIcon = ContentFinder<Texture2D>.Get("UI/RemoveStudent");
        private static readonly Texture2D RenameIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");
        private static readonly Texture2D ResheduleIcon = ContentFinder<Texture2D>.Get("UI/Reschedule");
        private static readonly Texture2D GearIcon = ContentFinder<Texture2D>.Get("UI/GearIcon");

        private static readonly Texture2D DeleteIcon = TexButton.Delete;
        private static readonly Texture2D ProgressBarFillTexture = SolidColorMaterials.NewSolidColorTexture(new Color(0.34f, 0.72f, 0.33f));
        public override void DoWindowContents(Rect inRect)
        {
            var leftRect = new Rect(inRect.x, inRect.y, (inRect.width * 0.65f) - (WindowPadding / 2f), inRect.height);
            var rightRect = new Rect(leftRect.xMax + WindowPadding, inRect.y, (inRect.width * 0.35f) - (WindowPadding / 2f), inRect.height);

            DrawClassList(leftRect);
            DrawClassroomList(rightRect);
        }

        private void DrawClassList(Rect rect)
        {
            var educationManager = EducationManager.Instance;
            var studyGroups = educationManager.StudyGroups;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, HeaderHeight), "PE_Classes".Translate());
            Text.Font = GameFont.Small;

            var addButtonRect = new Rect(rect.xMax - HeaderHeight, rect.y, HeaderHeight, HeaderHeight);
            if (Widgets.ButtonImage(addButtonRect, TexButton.Plus))
            {
                if (educationManager.Classrooms.Count == 0)
                {
                    Messages.Message("PE_CreateClassroomFirst".Translate(), MessageTypeDefOf.RejectInput);
                }
                else
                {
                    Find.WindowStack.Add(new Dialog_CreateClass(Find.CurrentMap));
                }
            }

            var listOutRect = new Rect(rect.x, rect.y + HeaderHeight + WindowPadding, rect.width, rect.height - (HeaderHeight + WindowPadding));
            
            if (studyGroups.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(listOutRect, "PE_NoClassesScheduled".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                var listContentRect = new Rect(0f, 0f, listOutRect.width - 16f, studyGroups.Count * (ClassRowHeight + WindowPadding));

                Widgets.BeginScrollView(listOutRect, ref classScrollPosition, listContentRect);

                float curY = 0f;
                for (int i = 0; i < studyGroups.Count; i++)
                {
                    var studyGroup = studyGroups[i];
                    var classRect = new Rect(0f, curY, listContentRect.width, ClassRowHeight);
                    DrawClassRow(classRect, studyGroup);
                    curY += ClassRowHeight + WindowPadding;
                }

                Widgets.EndScrollView();
            }
        }

        private void DrawClassRow(Rect rect, StudyGroup studyGroup)
        {
            var bgColor = studyGroup.classroom.color;
            var borderColor = bgColor;
            bgColor = Color.Lerp(bgColor, Color.black, 0.65f);

            Widgets.DrawBoxSolidWithOutline(rect, bgColor, borderColor);

            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            var innerRect = rect.ContractedBy(WindowPadding / 2f);

            Text.Font = GameFont.Medium;
            var nameRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.7f, 30f);
            Widgets.Label(nameRect, studyGroup.className);
            Text.Font = GameFont.Small;

            string description = studyGroup.subjectLogic.Description;
            var descriptionRect = new Rect(innerRect.x, nameRect.yMax, innerRect.width, 24f);
            Widgets.Label(descriptionRect, description);

            float progressBarWidth = innerRect.width * 0.7f;
            if (!studyGroup.subjectLogic.IsInfinite)
            {
                var progressBarRect = new Rect(innerRect.x, innerRect.yMax - 18f, progressBarWidth, 18f);
                Widgets.FillableBar(progressBarRect, studyGroup.ProgressPercentage, ProgressBarFillTexture);
                Text.Anchor = TextAnchor.MiddleCenter;
                if (studyGroup.subjectLogic is SkillClassLogic)
                {
                    Widgets.Label(progressBarRect, "PE_ProgressFormat".Translate(studyGroup.currentProgress.ToString("F0"), studyGroup.semesterGoal.ToString()));
                }
                else if (studyGroup.subjectLogic is ProficiencyClassLogic)
                {
                    Widgets.Label(progressBarRect, studyGroup.ProgressPercentage.ToStringPercent());
                }
                Text.Anchor = TextAnchor.UpperLeft;
            }

            var teacherPortraitRect = new Rect(innerRect.x + progressBarWidth, innerRect.yMax - TeacherPortraitSize, TeacherPortraitSize, TeacherPortraitSize);
            GUI.DrawTexture(teacherPortraitRect, PortraitsCache.Get(studyGroup.teacher, new Vector2(TeacherPortraitSize, TeacherPortraitSize), Rot4.South, cameraZoom: 1.5f));
            TooltipHandler.TipRegion(teacherPortraitRect, studyGroup.teacher.LabelCap);

            var scheduleInfoRect = new Rect(teacherPortraitRect.xMax, teacherPortraitRect.y + 20, innerRect.width - teacherPortraitRect.width - WindowPadding, TeacherPortraitSize);
            DrawScheduleInfo(scheduleInfoRect, studyGroup);
            
            var deleteButtonRect = new Rect(innerRect.xMax - 30f, innerRect.y, 30f, 30f);
            if (Widgets.ButtonImage(deleteButtonRect, DeleteIcon))
            {
                Find.WindowStack.Add(new Dialog_Confirm("PE_ConfirmDeleteClass".Translate(), () => EducationManager.Instance.RemoveStudyGroup(studyGroup)));
                return;
            }
            var rescheduleButtonRect = new Rect(deleteButtonRect.x - 32f, innerRect.y, 30f, 30f);
            if (Widgets.ButtonImage(rescheduleButtonRect, ResheduleIcon))
            {
                Find.WindowStack.Add(new Dialog_RescheduleClass(studyGroup));
            }
            TooltipHandler.TipRegion(rescheduleButtonRect, "PE_Reschedule".Translate());

            var renameButtonRect = new Rect(rescheduleButtonRect.x - 32f, innerRect.y, 30f, 30f);
            if (Widgets.ButtonImage(renameButtonRect, RenameIcon))
            {
                Find.WindowStack.Add(new Dialog_RenameClass(studyGroup));
            }
            TooltipHandler.TipRegion(renameButtonRect, "Rename".Translate());

            var participantIconRect = new Rect(renameButtonRect.x - 35, innerRect.y + 2, 24f, 24f);
            GUI.DrawTexture(participantIconRect, ParticipantIcon);
            TooltipHandler.TipRegion(participantIconRect, "PE_ManageStudents".Translate());
            if (Widgets.ButtonImage(participantIconRect, ParticipantIcon))
            {
                Find.WindowStack.Add(new Dialog_RemoveStudents(studyGroup));
            }
            var participantCountRect = new Rect(participantIconRect.xMax + 2f, participantIconRect.y, 40f, 24f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(participantCountRect, studyGroup.students.Count.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawScheduleInfo(Rect rect, StudyGroup studyGroup)
        {
            Text.Font = GameFont.Tiny;
            var scheduleRect = new Rect(rect.x, rect.y, rect.width, 24f);
            Widgets.Label(scheduleRect, "PE_ScheduleTime".Translate(studyGroup.startHour, studyGroup.endHour));
            Text.Font = GameFont.Small;
        }

        private void DrawClassroomList(Rect rect)
        {
            var educationManager = EducationManager.Instance;
            var classrooms = educationManager.Classrooms;

            var scheduleButtonRect = new Rect(rect.x, rect.y, rect.width, HeaderHeight);
            if (Widgets.ButtonText(scheduleButtonRect, "PE_ClassScheduling".Translate()))
            {
                Find.MainTabsRoot.SetCurrentTab(DefsOf.Schedule);
            }

            Text.Font = GameFont.Medium;
            var headerRect = new Rect(rect.x, rect.y + HeaderHeight, rect.width, HeaderHeight);
            float currentY = headerRect.y;
            ListSeparator(headerRect.x, ref currentY, headerRect.width, "PE_Classrooms".Translate());
            var descriptionRect = new Rect(rect.x, currentY, rect.width, 40f);
            Text.Font = GameFont.Small;
            Widgets.Label(descriptionRect, "PE_ClassroomsDescription".Translate());

            currentY += 45f;
            var listOutRect = new Rect(rect.x, currentY, rect.width, rect.height - currentY);
            var listContentRect = new Rect(0f, 0f, listOutRect.width - 16f, classrooms.Count * (ClassroomRowHeight + WindowPadding));

            Widgets.BeginScrollView(listOutRect, ref classroomScrollPosition, listContentRect);

            float curY = 0f;
            for (int i = 0; i < classrooms.Count; i++)
            {
                var classroom = classrooms[i];
                var classroomRect = new Rect(0f, curY, listContentRect.width, ClassroomRowHeight);

                if (i % 2 == 1)
                {
                    Widgets.DrawLightHighlight(classroomRect);
                }
                if (Mouse.IsOver(classroomRect))
                {
                    Widgets.DrawHighlight(classroomRect);
                }

                var colorRect = new Rect(classroomRect.x + 4f, classroomRect.y + ((ClassroomRowHeight - 32f) / 2f), 32f, 32f);
                Widgets.DrawBoxSolid(colorRect, classroom.color);
                if (Widgets.ButtonInvisible(colorRect))
                {
                    Find.WindowStack.Add(new Window_ColorPicker(classroom.color, (Color newColor) => classroom.color = newColor));
                }
                TooltipHandler.TipRegion(colorRect, "PE_ChangeColor".Translate());

                Text.Anchor = TextAnchor.MiddleLeft;
                var nameRect = new Rect(colorRect.xMax + 8f, classroomRect.y, classroomRect.width - 80f, classroomRect.height);
                Widgets.Label(nameRect, classroom.name);
                
                Text.Anchor = TextAnchor.UpperLeft;

                var renameButtonRect = new Rect(classroomRect.xMax - 72f, classroomRect.y + ((ClassroomRowHeight - 32f) / 2f), 32f, 32f);
                if (Widgets.ButtonImage(renameButtonRect, RenameIcon))
                {
                    Find.WindowStack.Add(new Dialog_RenameClassroom(classroom));
                }
                TooltipHandler.TipRegion(renameButtonRect, "Rename".Translate());

                var settingsButtonRect = new Rect(classroomRect.xMax - 36f, classroomRect.y + ((ClassroomRowHeight - 32f) / 2f), 32f, 32f);
                if (Widgets.ButtonImage(settingsButtonRect, GearIcon))
                {
                    Find.WindowStack.Add(new Dialog_ClassroomSettings(classroom));
                }
                TooltipHandler.TipRegion(settingsButtonRect, "PE_ClassroomSettings".Translate());
                
                if (Widgets.ButtonInvisible(nameRect))
                {
                    if (classroom.LearningBoard != null && classroom.LearningBoard.parent != null)
                    {
                        Find.Selector.Select(classroom.LearningBoard.parent);
                        Current.Game.CurrentMap = classroom.LearningBoard.parent.Map;
                        Find.CameraDriver.JumpToCurrentMapLoc(classroom.LearningBoard.parent.DrawPos + new Vector3(-6f, 0f, 0f));
                    }
                }
                curY += ClassroomRowHeight + WindowPadding;
            }

            Widgets.EndScrollView();
        }
        public static void ListSeparator(float curX, ref float curY, float width, string label)
        {
            Color color = GUI.color;
            curY += 3f;
            Rect rect = new Rect(curX, curY, width, 30f);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(rect, label);
            curY += 30f;
            GUI.color = Widgets.SeparatorLineColor;
            Widgets.DrawLineHorizontal(curX, curY, width);
            curY += 2f;
            GUI.color = color;
        }
    }
}
