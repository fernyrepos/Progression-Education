using RimWorld;
using System.Collections.Generic;
using System.Text;
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
            var text = new StringBuilder();
            text.AppendInNewLine(studyGroup.subjectLogic.BaseTooltipFor(pawn));
            if (studyGroup.teacher != pawn)
            {
                var studentTooltip = studyGroup.subjectLogic.StudentTooltipFor(pawn);
                if (!studentTooltip.NullOrEmpty())
                {
                    text.AppendLineIfNotEmpty();
                    text.AppendInNewLine("PE_AsAStudent".Translate(studentTooltip));
                    text.AppendInNewLine("================");
                    text.AppendInNewLine(studentTooltip);
                }
            }
            if (!studyGroup.students.Contains(pawn))
            {
                var teacherTooltip = studyGroup.subjectLogic.TeacherTooltipFor(pawn);
                if (!teacherTooltip.NullOrEmpty())
                {
                    text.AppendLineIfNotEmpty();
                    text.AppendInNewLine("PE_AsATeacher".Translate(teacherTooltip));
                    text.AppendInNewLine("================");
                    text.AppendInNewLine(teacherTooltip);
                }
            }
            return text.ToString();
        }

        public override bool ShouldGrayOut(Pawn pawn, out TaggedString reason)
        {
            return !assignments.CanParticipate(pawn, out reason);
        }
    }
}
