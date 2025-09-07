using RimWorld;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class Dialog_RemoveStudents : Window
    {
        private readonly StudyGroup studyGroup;
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new(400f, 500f);

        public Dialog_RemoveStudents(StudyGroup studyGroup)
        {
            this.studyGroup = studyGroup;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "PE_RemoveStudents".Translate());
            Text.Font = GameFont.Small;

            float curY = 45f;
            float rowHeight = 80f;
            var listOutRect = new Rect(0f, curY, inRect.width, inRect.height - curY - 45f);
            var listContentRect = new Rect(0f, 0f, listOutRect.width - 16f, studyGroup.students.Count * rowHeight);

            Widgets.BeginScrollView(listOutRect, ref scrollPosition, listContentRect);

            float rowY = 0f;
            foreach (var student in studyGroup.students.ToList())
            {
                var rowRect = new Rect(0f, rowY, listContentRect.width, rowHeight - 5f);
                DrawStudentRow(rowRect, student);
                rowY += rowHeight;
            }

            Widgets.EndScrollView();

            if (Widgets.ButtonText(new Rect((inRect.width / 2f) - 75f, inRect.height - 35f, 150f, 35f), "CloseButton".Translate()))
            {
                Close();
            }
        }

        private void DrawStudentRow(Rect rect, Pawn student)
        {
            Widgets.DrawOptionUnselected(rect);

            float portraitSize = rect.height;
            var portraitRect = new Rect(rect.x + 5f, rect.y + ((rect.height - portraitSize) / 2f), portraitSize, portraitSize);
            GUI.DrawTexture(portraitRect, PortraitsCache.Get(student, new Vector2(portraitSize, portraitSize), Rot4.South, default, 1.2f));

            var nameRect = new Rect(portraitRect.xMax + 10f, rect.y + 10f, rect.width - portraitRect.width - 20f, 25f);
            Widgets.Label(nameRect, student.Name.ToStringFull);

            var removeButtonRect = new Rect(nameRect.x, nameRect.yMax + 5f, 180f, 30f);
            var originalAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            if (Widgets.ButtonText(removeButtonRect, "PE_RemoveFromClass".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PE_ConfirmRemoveStudent".Translate(student.LabelShort), () =>
                {
                    studyGroup.RemoveStudent(student);
                }, true));
            }
            Text.Anchor = originalAnchor;
        }
    }
}
