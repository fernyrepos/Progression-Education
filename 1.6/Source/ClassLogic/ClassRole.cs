using System.Linq;
using RimWorld;
using Verse;

namespace ProgressionEducation;

public class ClassRole(
    string roleId,
    int maxCount,
    int minCount,
    TaggedString label,
    TaggedString categoryLabel,
    StudyGroup studyGroup)
    : ILordJobRole
{
    public readonly StudyGroup studyGroup = studyGroup;
    private TaggedString categoryLabel = categoryLabel;
    private TaggedString label = label;

    public string RoleId { get; } = roleId;

    public int MaxCount { get; } = maxCount;

    public int MinCount { get; } = minCount;

    public TaggedString Label => label;

    public TaggedString LabelCap => label.CapitalizeFirst();

    public TaggedString CategoryLabel => categoryLabel;

    public TaggedString CategoryLabelCap => categoryLabel.CapitalizeFirst();

    public virtual AcceptanceReport CanAcceptPawn(Pawn pawn)
    {
        if (pawn == null
            || studyGroup == null)
        {
            return AcceptanceReport.WasRejected;
        }

        if (EducationManager.Instance.StudyGroups
                .Except(studyGroup)
                .FirstOrDefault(sg => !sg.suspended
                                      && (sg.students.Contains(pawn) || sg.teacher == pawn)
                                      && studyGroup.HasConflict(sg))
            is StudyGroup otherGroup)
        {
            return new AcceptanceReport("PE_CannotParticipateScheduled".Translate(
                otherGroup.startHour,
                otherGroup.endHour,
                otherGroup.className)
            );
        }

        return AcceptanceReport.WasAccepted;
    }

    public virtual float ScoreFor(Pawn pawn)
    {
        return 0f;
    }
}