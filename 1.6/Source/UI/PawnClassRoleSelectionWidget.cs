using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class PawnClassRoleSelectionWidget : PawnRoleSelectionWidgetBase_Fixed<ClassRole>
    {
        public StudyGroup studyGroup;

        public PawnClassRoleSelectionWidget(ILordJobCandidatePool candidatePool, ILordJobAssignmentsManager<ClassRole> assignments)
            : base(candidatePool, assignments)
        {
        }

        public override string SpectatorsLabel()
        {
            return "PE_Spectators".Translate();
        }

        public override string NotParticipatingLabel()
        {
            return "PE_NotParticipating".Translate();
        }

        public override bool ShouldDrawHighlight(ClassRole role, Pawn pawn)
        {
            return false;
        }

        public override string SpectatorFilterReason(Pawn pawn)
        {
            return null;
        }

        public override string ExtraInfoForRole(ClassRole role, Pawn pawnToBeAssigned, IEnumerable<Pawn> currentlyAssigned)
        {
            return null;
        }

        protected override string ExtraTipContents(Pawn pawn)
        {
            if (studyGroup == null)
            {
                return null;
            }
            var text = studyGroup.subjectLogic.BaseTooltipFor(pawn);
            if (studyGroup.teacher != pawn)
            {
                text += "\n" + studyGroup.subjectLogic.StudentTooltipFor(pawn);
            }
            return text;
        }

        public override bool ShouldGrayOut(Pawn pawn, out TaggedString reason)
        {
            return !assignments.CanParticipate(pawn, out reason);
        }
    }
}
