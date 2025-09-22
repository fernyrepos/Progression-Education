using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    public class Dialog_RenameClassroom : Dialog_Rename<Classroom>
    {
        private bool createLetter = false;
        
        public Dialog_RenameClassroom(Classroom classroom) : base(classroom)
        {
        }
        
        public Dialog_RenameClassroom(Classroom classroom, bool createLetter) : base(classroom)
        {
            this.createLetter = createLetter;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            bool flag = false;
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                flag = true;
                Event.current.Use();
            }
            Rect rect = new Rect(inRect);
            Text.Font = GameFont.Medium;
            rect.height = Text.LineHeight + 10f;
            Widgets.Label(rect, "PE_NameClassroom".Translate());
            Text.Font = GameFont.Small;
            GUI.SetNextControlName("RenameField");
            string text = Widgets.TextField(new Rect(0f, rect.height, inRect.width, 35f), curName);
            if (AcceptsInput && text.Length < MaxNameLength)
            {
                curName = text;
            }
            else if (!AcceptsInput)
            {
                ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl)).SelectAll();
            }
            if (!focusedRenameField)
            {
                UI.FocusControl("RenameField", this);
                focusedRenameField = true;
            }
            if (!(Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 10f, inRect.width - 15f - 15f, 35f), "OK") || flag))
            {
                return;
            }
            AcceptanceReport acceptanceReport = NameIsValid(curName);
            if (!acceptanceReport.Accepted)
            {
                if (acceptanceReport.Reason.NullOrEmpty())
                {
                    Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                }
                else
                {
                    Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, historical: false);
                }
                return;
            }
            if (renaming != null)
            {
                renaming.RenamableLabel = curName;
            }
            OnRenamed(curName);
            Find.WindowStack.TryRemove(this);
        }

        public override void OnRenamed(string name)
        {
            base.OnRenamed(name);
            
            if (createLetter && renaming != null)
            {
                Find.LetterStack.ReceiveLetter("PE_NewClassroomCreated".Translate(name), "PE_NewClassroomCreatedDesc".Translate(), LetterDefOf.NeutralEvent);
            }
        }
    }
}
