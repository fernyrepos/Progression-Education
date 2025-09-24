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

        private static readonly Texture2D ParticipantIcon = ContentFinder<Texture2D>.Get("UI/Icons/Members");
        private static readonly Texture2D RenameIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");
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
                Find.WindowStack.Add(new Dialog_CreateClass(Find.CurrentMap));
            }

            var listOutRect = new Rect(rect.x, rect.y + HeaderHeight + WindowPadding, rect.width, rect.height - (HeaderHeight + WindowPadding));
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

        private void DrawClassRow(Rect rect, StudyGroup studyGroup)
        {
            var bgColor = studyGroup.classroom.color;
            var borderColor = bgColor;
            bgColor = Color.Lerp(bgColor, Color.black, 0.5f);
            
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

            var participantIconRect = new Rect(nameRect.xMax, innerRect.y, 24f, 24f);
            GUI.DrawTexture(participantIconRect, ParticipantIcon);
            TooltipHandler.TipRegion(participantIconRect, "PE_Participants".Translate());
            var participantCountRect = new Rect(participantIconRect.xMax + 2f, innerRect.y, 40f, 24f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(participantCountRect, studyGroup.students.Count.ToString());
            Text.Anchor = TextAnchor.UpperLeft;

            string description = studyGroup.subjectLogic.Description;
            var descriptionRect = new Rect(innerRect.x, nameRect.yMax, innerRect.width, 24f);
            Widgets.Label(descriptionRect, description);

            if (studyGroup.subjectLogic is SkillClassLogic)
            {
                var progressBarRect = new Rect(innerRect.x, innerRect.yMax - 18f, innerRect.width * 0.8f, 18f);
                Widgets.FillableBar(progressBarRect, studyGroup.ProgressPercentage, ProgressBarFillTexture);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(progressBarRect, "PE_ProgressFormat".Translate(studyGroup.currentProgress.ToString("F0"), studyGroup.semesterGoal.ToString()));
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else if (studyGroup.subjectLogic is ProficiencyClassLogic)
            {
                var progressBarRect = new Rect(innerRect.x, innerRect.yMax - 18f, innerRect.width * 0.8f, 18f);
                Widgets.FillableBar(progressBarRect, studyGroup.ProgressPercentage, ProgressBarFillTexture);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(progressBarRect, studyGroup.ProgressPercentage.ToStringPercent());
                Text.Anchor = TextAnchor.UpperLeft;
            }

            var deleteButtonRect = new Rect(innerRect.xMax - 30f, innerRect.y, 30f, 30f);
            if (Widgets.ButtonImage(deleteButtonRect, DeleteIcon))
            {
                EducationManager.Instance.RemoveStudyGroup(studyGroup);
                return;
            }
            TooltipHandler.TipRegion(deleteButtonRect, "Delete".Translate());

            var rescheduleButtonRect = new Rect(deleteButtonRect.x - 32f, innerRect.y, 30f, 30f);
            if (Widgets.ButtonImage(rescheduleButtonRect, TexButton.ReorderUp))
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

            var manageStudentsButtonRect = new Rect(renameButtonRect.x - 32f, innerRect.y, 30f, 30f);
            if (Widgets.ButtonImage(manageStudentsButtonRect, ParticipantIcon))
            {
                Find.WindowStack.Add(new Dialog_RemoveStudents(studyGroup));
            }
            TooltipHandler.TipRegion(manageStudentsButtonRect, "PE_ManageStudents".Translate());
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
            var headerRect = new Rect(rect.x, rect.y + HeaderHeight + WindowPadding, rect.width, HeaderHeight);
            Widgets.Label(headerRect, "PE_Classrooms".Translate());
            Text.Font = GameFont.Small;

            var listOutRect = new Rect(rect.x, headerRect.yMax + WindowPadding, rect.width, rect.height - (headerRect.yMax + WindowPadding));
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

                var renameButtonRect = new Rect(classroomRect.xMax - 36f, classroomRect.y + ((ClassroomRowHeight - 32f) / 2f), 32f, 32f);
                if (Widgets.ButtonImage(renameButtonRect, RenameIcon))
                {
                    Find.WindowStack.Add(new Dialog_RenameClassroom(classroom));
                }
                TooltipHandler.TipRegion(renameButtonRect, "Rename".Translate());

                curY += ClassroomRowHeight + WindowPadding;
            }

            Widgets.EndScrollView();
        }
    }
}
