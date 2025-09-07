using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    public class Dialog_EditClassroom : Window
    {
        private readonly Classroom classroom;
        private string newName;
        private Color newColor;

        public override Vector2 InitialSize => new(400f, 200f);

        public Dialog_EditClassroom(Classroom classroom)
        {
            this.classroom = classroom;
            newName = classroom.name;
            newColor = classroom.color;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "PE_EditClassroom".Translate());
            Text.Font = GameFont.Small;

            var nameRect = new Rect(inRect.x, inRect.y + 45f, inRect.width, 30f);
            newName = Widgets.TextField(nameRect, newName);

            var colorButtonRect = new Rect(inRect.x, inRect.y + 85f, inRect.width, 30f);
            if (Widgets.ButtonText(colorButtonRect, "PE_ChangeColor".Translate()))
            {
                Find.WindowStack.Add(new Window_ColorPicker(newColor, (Color color) => newColor = color));
            }

            var okButtonRect = new Rect(inRect.x, inRect.yMax - 30f, 100f, 30f);
            if (Widgets.ButtonText(okButtonRect, "OK".Translate()))
            {
                classroom.name = newName;
                classroom.color = newColor;
                Close();
            }

            var cancelButtonRect = new Rect(inRect.xMax - 100f, inRect.yMax - 30f, 100f, 30f);
            if (Widgets.ButtonText(cancelButtonRect, "Cancel".Translate()))
            {
                Close();
            }
        }
    }
}
