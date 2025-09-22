using RimWorld;
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
