using System.Linq;
using RimWorld;
using Verse;

namespace ProgressionEducation
{
    public class ClassRole : ILordJobRole
    {
        private readonly string roleId;
        private readonly int maxCount;
        private readonly int minCount;
        private TaggedString label;
        private TaggedString categoryLabel;
        public StudyGroup studyGroup;

        public ClassRole(string roleId, int maxCount, int minCount, TaggedString label, TaggedString categoryLabel, StudyGroup studyGroup)
        {
            this.roleId = roleId;
            this.maxCount = maxCount;
            this.minCount = minCount;
            this.label = label;
            this.categoryLabel = categoryLabel;
            this.studyGroup = studyGroup;
        }

        public virtual AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return AcceptanceReport.WasRejected;
            }

            if (EducationManager.Instance.StudyGroups
                    .Except(studyGroup)
                    .FirstOrDefault(sg => (sg.students.Contains(pawn) || sg.teacher == pawn) 
                                          && studyGroup.HasConflict(sg)) is StudyGroup otherGroup)
            {
                return new AcceptanceReport("PE_CannotParticipateScheduled".Translate(
                    otherGroup.startHour,
                    otherGroup.endHour, 
                    otherGroup.className)
                );
            }
            return AcceptanceReport.WasAccepted;
        }

        public int MaxCount => maxCount;

        public int MinCount => minCount;

        public TaggedString Label => label;

        public TaggedString LabelCap => label.CapitalizeFirst();

        public TaggedString CategoryLabel => categoryLabel;

        public TaggedString CategoryLabelCap => categoryLabel.CapitalizeFirst();

        public string RoleId => roleId;
    }
}