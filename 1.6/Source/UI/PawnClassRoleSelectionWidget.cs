using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
[StaticConstructorOnStartup]
public class PawnClassRoleSelectionWidget(
    ILordJobCandidatePool candidatePool,
    ILordJobAssignmentsManager<ClassRole> assignments)
    : PawnRoleSelectionWidgetBase_Fixed<ClassRole>(candidatePool,
        assignments)
{
    public StudyGroup studyGroup;

    public override string ExtraInfoForRole(ClassRole role, Pawn pawnToBeAssigned,
        IEnumerable<Pawn> currentlyAssigned)
    {
        return null;
    }

    protected override string ExtraTipContents(Pawn pawn)
    {
        if (studyGroup == null)
        {
            return null;
        }

        var text = new StringBuilder(studyGroup.subjectLogic.BaseTooltipFor(pawn));
        text.AppendLineIfNotEmpty();
        text.AppendLineIfNotEmpty();
        if (!studyGroup.students.Contains(pawn))
        {
            var teacherTooltip = studyGroup.subjectLogic.TeacherTooltipFor(pawn);
            if (!teacherTooltip.NullOrEmpty())
            {
                text.AppendLineTagged("PE_AsATeacher".Translate()
                    .Colorize(ColoredText.SubtleGrayColor));
                text.AppendLine();
                text.AppendLine(teacherTooltip);
                text.AppendLine();
            }
        }

        if (studyGroup.teacher != pawn)
        {
            var studentTooltip = studyGroup.subjectLogic.StudentTooltipFor(pawn);
            if (!studentTooltip.NullOrEmpty())
            {
                text.AppendLineTagged("PE_AsAStudent".Translate()
                    .Colorize(ColoredText.SubtleGrayColor));
                text.AppendLine();
                text.AppendLine(studentTooltip);
                text.AppendLine();
            }
        }

        return text.ToString().TrimEndNewlines();
    }

    public override string NotParticipatingLabel()
    {
        return "PE_NotParticipating".Translate();
    }

    public override bool ShouldDrawHighlight(ClassRole role, Pawn pawn)
    {
        return false;
    }

    public override bool ShouldGrayOut(Pawn pawn, out TaggedString reason)
    {
        return !assignments.CanParticipate(pawn, out reason);
    }

    public override string SpectatorFilterReason(Pawn pawn)
    {
        return null;
    }

    public override string SpectatorsLabel()
    {
        return "PE_Spectators".Translate();
    }
}