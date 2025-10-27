using UnityEngine;
using RimWorld;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class Dialog_ClassroomSettings : Window
    {
        private Classroom classroom;
        
        public override Vector2 InitialSize => new Vector2(350f, 350f);
        
        private string Title => "PE_ClassroomSettings".Translate();
        
        public Dialog_ClassroomSettings(Classroom classroom)
        {
            this.classroom = classroom;
            forcePause = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), Title);
            Text.Font = GameFont.Small;
            float curY = 40f;
            float rowHeight = 35f;
            Widgets.CheckboxLabeled(new Rect(15f, curY, inRect.width - 30f, rowHeight), "PE_RestrictReservationsDuringClass".Translate(), ref classroom.restrictReservationsDuringClass);
            TooltipHandler.TipRegion(new Rect(15f, curY, inRect.width - 30f, rowHeight), "PE_RestrictReservationsDuringClass_Desc".Translate());
            
            curY += rowHeight + 10f;
            Widgets.CheckboxLabeled(new Rect(15f, curY, inRect.width - 30f, rowHeight), "PE_InterruptJobsDuringClass".Translate(), ref classroom.interruptJobs);
            TooltipHandler.TipRegion(new Rect(15f, curY, inRect.width - 30f, rowHeight), "PE_InterruptJobsDuringClass_Desc".Translate());
            
            curY += rowHeight + 10f;
            Rect buttonRect = new Rect((inRect.width - 150f) / 2f, inRect.height - 40f, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, "Close".Translate()))
            {
                Close();
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                Event.current.Use();
                Close();
            }
        }
    }
}
