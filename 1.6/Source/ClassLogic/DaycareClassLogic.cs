using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public class DaycareClassLogic : ClassSubjectLogic
{
    private List<SkillDef> cachedSkillOptionsForDisplay;
    private int cachedSkillOptionsForDisplayStudentCount;
    private Pawn cachedSkillOptionsForDisplayTeacher;

    private int cachedSkillOptionsForDisplayTick;

    public DaycareClassLogic()
    {
    }

    public DaycareClassLogic(StudyGroup parent)
        : base(parent)
    {
    }


    public override string Description => "PE_Daycare".Translate();

    public override bool IsInfinite => true;

    public override string Label => "PE_SubjectDaycare".Translate();

    public override float LearningSpeedModifier =>
        EducationMod.settings.daycareClassesLearningSpeedModifier;

    public override float ProgressPerTick
    {
        get
        {
            if (studyGroup.teacher == null
                || studyGroup.classroom == null)
            {
                return 0f;
            }

            return Mathf.Max(0,
                CalculateTeacherScore(studyGroup.teacher)
                * studyGroup.classroom.ClassSpeed
                * LearningSpeedModifier);
        }
    }

    public override void ApplyTeachingTick(Pawn student, JobDriver_Teach jobDriver, int delta)
    {
        base.ApplyTeachingTick(student, jobDriver, delta);
        jobDriver.taughtSkill ??= ChooseSkillDef();
        if (jobDriver.taughtSkill is not SkillDef taughtSkill
            || student?.skills?.GetSkill(taughtSkill) == null)
        {
            return;
        }

        var xpPerTick = ProgressPerTick * CalculateStudentScore(student);
        student.skills.Learn(taughtSkill, xpPerTick * delta);
    }

    private static IEnumerable<SkillRecord> AvailableStudySkills(IEnumerable<Pawn> students,
        Pawn teacher)
    {
        if (students == null
            || teacher?.skills?.skills == null)
        {
            yield break;
        }

        var studentSkillDefs = students
            .SelectMany(s => s.skills?.skills ?? [])
            .Where(sk => sk.TotallyDisabled)
            .Select(sk => sk.def)
            .ToHashSet();

        var availableSkills = teacher.skills.skills
            .Where(sk => !sk.TotallyDisabled
                         && !studentSkillDefs.Contains(sk.def));

        foreach (var record in availableSkills)
        {
            yield return record;
        }
    }

    private static IEnumerable<SkillRecord> BestAvailableStudySkills(IEnumerable<Pawn> students,
        Pawn teacher)
    {
        return AvailableStudySkills(students, teacher)
            .OrderByDescending(s => s)
            .Take(4);
    }

    public override float CalculateLearningPerTick(Pawn student)
    {
        return 2f * base.CalculateLearningPerTick(student);
    }

    public override float CalculateStudentScore(Pawn p)
    {
        return 1f;
    }

    public override float CalculateTeacherScore(Pawn teacher)
    {
        if (teacher == null
            || studyGroup.classroom == null)
        {
            return 0f;
        }

        var social = teacher.skills.GetSkill(SkillDefOf.Social).Level;
        var intelligence = teacher.skills.GetSkill(SkillDefOf.Intellectual).Level;
        var socialImpact = CalculateSocialImpactFactor(teacher);
        var score = (social * 0.5f + intelligence * 0.5f) * socialImpact;
        return score * 0.02f;
    }

    private SkillDef ChooseSkillDef()
    {
        return BestAvailableStudySkills(studyGroup.students, studyGroup.teacher)
            .Select(s => s.def)
            .RandomElementWithFallback(SkillDefOf.Social);
    }

    public override ClassSubjectLogic DeepClone(StudyGroup parent)
    {
        return new DaycareClassLogic(parent);
    }

    public override void DrawConfigurationUI(Rect rect, ref float curY, IClassDialog classDialog)
    {
    }

    private List<SkillDef> GetSkillOptionsForDisplay(Pawn pawn)
    {
        if (Find.TickManager.TicksGame == cachedSkillOptionsForDisplayTick
            && pawn == cachedSkillOptionsForDisplayTeacher
            && studyGroup.students.Count == cachedSkillOptionsForDisplayStudentCount)
        {
            return cachedSkillOptionsForDisplay;
        }

        cachedSkillOptionsForDisplay =
            BestAvailableStudySkills(studyGroup.students, pawn)
                .Select(l => l.def)
                .Append(SkillDefOf.Social)
                .Distinct()
                .Take(4)
                .ToList();
        cachedSkillOptionsForDisplayStudentCount = studyGroup.students.Count;
        cachedSkillOptionsForDisplayTeacher = pawn;
        cachedSkillOptionsForDisplayTick = Find.TickManager.TicksGame;
        return cachedSkillOptionsForDisplay;
    }

    public override void HandleStudentLifecycleEvents()
    {
        foreach (var student in studyGroup.students
                     .Where(student => student.DevelopmentalStage != DevelopmentalStage.Child))
        {
            studyGroup.RemoveStudent(student);
        }
    }

    public override AcceptanceReport IsStudentQualified(Pawn student)
    {
        var baseReport = base.IsStudentQualified(student);
        if (!baseReport.Accepted)
        {
            return baseReport;
        }

        if (student.DevelopmentalStage != DevelopmentalStage.Child)
        {
            return new AcceptanceReport("PE_MustBeChild".Translate());
        }

        if (student.needs?.learning == null)
        {
            return new AcceptanceReport("PE_NoLearningNeed".Translate());
        }

        return AcceptanceReport.WasAccepted;
    }

    public override AcceptanceReport IsTeacherQualified(Pawn teacher)
    {
        var baseReport = base.IsTeacherQualified(teacher);
        if (!baseReport.Accepted)
        {
            return baseReport;
        }

        if (teacher.DevelopmentalStage < DevelopmentalStage.Adult)
        {
            return new AcceptanceReport("PE_TeacherRoleOnlyForAdults".Translate());
        }

        var skill = teacher.skills?.GetSkill(SkillDefOf.Social);
        if (skill == null
            || skill.TotallyDisabled)
        {
            return new AcceptanceReport(
                "PE_TeacherSkillDisabled".Translate(teacher.LabelShortCap,
                    SkillDefOf.Social.LabelCap));
        }

        return AcceptanceReport.WasAccepted;
    }

    public override string StudentTooltipFor(Pawn pawn)
    {
        if (pawn == null
            || !IsStudentQualified(pawn)
            || studyGroup is not { classroom: not null })
        {
            return "";
        }

        return base.StudentTooltipFor(pawn);
    }

    public override string TeacherTooltipFor(Pawn pawn)
    {
        if (pawn == null
            || !IsTeacherQualified(pawn)
            || studyGroup is not { classroom: not null })
        {
            return "";
        }

        var text = new StringBuilder(base.TeacherTooltipFor(pawn));
        text.AppendLineIfNotEmpty();
        AppendSkillLevel(SkillDefOf.Social, pawn, text);
        AppendSkillLevel(SkillDefOf.Intellectual, pawn, text);

        text.AppendLineIfNotEmpty();
        var xpPerHour = CalculateTeacherScore(pawn) * GenDate.TicksPerHour;
        if (xpPerHour > 0)
        {
            text.AppendLineTagged($"{"PE_HourlyTeaching".Translate().AsTipTitle()}: {xpPerHour
                * LearningSpeedModifier
                * studyGroup.classroom.ClassSpeed:F0}");
            text.AppendLine();
            var lessonOptions = GetSkillOptionsForDisplay(pawn);
            text.AppendLineTagged($"{"PE_LessonOptions".Translate()}:".AsTipTitle());
            foreach (var lessonOption in lessonOptions)
            {
                text.AppendLine($" - {lessonOption.LabelCap}");
            }

            text.AppendLine();
        }

        var socialImpact = CalculateSocialImpactFactor(pawn);
        text.AppendLineTagged(
            $"{"PE_TeachingFactor".Translate()}:".AsTipTitle()
            + $" {(LearningSpeedModifier
                 * studyGroup.classroom.ClassSpeed
                 * socialImpact
                ).ToStringPercent()}"
        );
        text.AppendLine($" - {"StatsReport_BaseValue".Translate()}: x{LearningSpeedModifier.ToStringPercent()}");
        text.AppendLine($" - {"PE_ClassSpeed".Translate()}: x{studyGroup.classroom.ClassSpeed.ToStringPercent()}");
        text.AppendLine(
            $" - {StatDefOf.SocialImpact.LabelCap}: x{socialImpact.ToStringPercent()}");

        return text.ToString();
    }
}
