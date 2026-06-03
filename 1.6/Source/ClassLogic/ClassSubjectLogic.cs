using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public abstract class ClassSubjectLogic : IExposable
{
    private const int InteractionInterval = 900;
    protected HashSet<ThingDef> cachedValidLearningBenches;
    protected StudyGroup studyGroup;

    protected ClassSubjectLogic()
    {
    }

    protected ClassSubjectLogic(StudyGroup parent)
        : this()
    {
        studyGroup = parent;
    }

    public int BenchCount
    {
        get
        {
            var facility = studyGroup?.classroom?.LearningBoard?.parent?.GetComp<CompFacility>();
            if (facility == null)
            {
                return 0;
            }

            var validBenches = GetValidLearningBenches();
            var count = facility.LinkedBuildings.Count(t => validBenches.Contains(t.def));
            return count;
        }
    }

    public virtual string BenchLabel => "PE_SchoolDesks".Translate();

    public abstract string Description { get; }
    public virtual bool IsInfinite => false;

    public virtual string Label => "None".Translate();
    public virtual string LabelCap => Label.CapitalizeFirst();
    public virtual string LabelFocus => LabelCap;
    public virtual JobDef LearningJob => DefsOf.PE_AttendClass;
    public abstract float LearningSpeedModifier { get; }

    public virtual float ProgressPerTick => 0f;

    public virtual void ExposeData()
    {
        Scribe_References.Look(ref studyGroup, "studyGroup");
    }

    public virtual void AddRequirements(StringBuilder requirements)
    {
        var benchLabel = BenchLabel;
        var benchCount = BenchCount;
        var studentsCount = studyGroup.students.Count;
        if (string.IsNullOrEmpty(benchLabel)
            || (studentsCount == 0 && benchCount == 0))
        {
            return;
        }

        var presentText = benchCount < studentsCount || benchCount < 1
            ? $"{"PE_Present".Translate(benchCount)}".Colorize(ColoredText.ThreatColor)
            : "";
        requirements.AppendLineTagged($"{studentsCount}x {benchLabel} {presentText}");
    }

    protected static void AppendSkillLevel(SkillDef def, Pawn pawn, StringBuilder text)
    {
        if (pawn.skills?.GetSkill(def) is SkillRecord record)
        {
            text.AppendLineTagged($"{record.def.LabelCap}:".AsTipTitle()
                                  + $" {record.Level}");
        }
    }

    public virtual void ApplyLearningTick(Pawn student, int delta)
    {
        if (student.DevelopmentalStage == DevelopmentalStage.Child)
        {
            var growthPointsPerTick = student.ageTracker.GrowthPointsPerDay / GenDate.TicksPerDay;
            student.ageTracker.growthPoints += growthPointsPerTick * delta;
        }

        student.needs?.learning?.Learn(CalculateLearningPerTick(student) * delta);
    }

    public virtual void ApplyTeachingTick(Pawn student, JobDriver_Teach jobDriver, int delta)
    {
        var teacher = jobDriver.pawn;
        if (jobDriver.taughtSkill is SkillDef taughtSkill
            && teacher.IsHashIntervalTick(InteractionInterval, delta))
        {
            teacher.interactions.TryInteractWith(student,
                taughtSkill.lessonInteraction);
            teacher.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.GaveLesson,
                student);
            student.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDefOf.WasTaught,
                teacher);
        }
    }

    public virtual AcceptanceReport ArePrerequisitesMet()
    {
        var benchLabel = BenchLabel;
        if (!string.IsNullOrEmpty(benchLabel)
            && BenchCount < studyGroup.students.Count)
        {
            return new AcceptanceReport("PE_NotEnoughBenches".Translate(benchLabel,
                studyGroup.students.Count,
                BenchCount));
        }

        return AcceptanceReport.WasAccepted;
    }

    public virtual string BaseTooltipFor(Pawn pawn)
    {
        var text = new StringBuilder();
        foreach (var sg in EducationManager.Instance.StudyGroups
                     .Where(s => s.AllParticipants.Contains(pawn))
                     .OrderBy(g => g.startHour))
        {
            text.AppendInNewLine(sg.className);
            text.Append(": ");
            text.Append("PE_ScheduleTime".Translate(sg.startHour, sg.endHour));
        }

        return text.ToString();
    }

    public virtual float CalculateLearningPerTick(Pawn student)
    {
        if (student == null)
        {
            return 0f;
        }

        return LearningUtility.NeedSatisfiedPerTick
               * studyGroup.classroom.ClassSpeed
               * LearningSpeedModifier;
    }

    public virtual float CalculateSocialImpactFactor(Pawn pawn)
    {
        if (pawn == null)
        {
            return 0f;
        }

        var socialImpact = pawn.GetStatValue(StatDefOf.SocialImpact);
        // 0.5 at 0% - 2.0 at 500%
        return 0.5f
               + 0.5f
               * Mathf.Pow(
                   Mathf.Clamp(socialImpact, 0f, 5f),
                   0.683f
               );
    }

    public abstract float CalculateStudentScore(Pawn p);

    public abstract float CalculateTeacherScore(Pawn p);

    public abstract ClassSubjectLogic DeepClone(StudyGroup parent);

    public abstract void DrawConfigurationUI(Rect rect, ref float curY, IClassDialog classDialog);

    public virtual HashSet<ThingDef> GetValidLearningBenches()
    {
        cachedValidLearningBenches ??= DefDatabase<ThingDef>.AllDefsListForReading
            .Where(EducationUtility.IsSchoolDesk)
            .ToHashSet();

        return cachedValidLearningBenches;
    }

    public virtual void GrantCompletionRewards()
    {
    }

    public virtual void HandleStudentLifecycleEvents()
    {
    }

    public virtual AcceptanceReport IsStudentQualified(Pawn student)
    {
        if (student == null)
        {
            return AcceptanceReport.WasRejected;
        }

        if (student.DevelopmentalStage < DevelopmentalStage.Child)
        {
            return new AcceptanceReport("PE_TooYoung".Translate(student.LabelShort));
        }

        return AcceptanceReport.WasAccepted;
    }

    public virtual AcceptanceReport IsTeacherQualified(Pawn teacher)
    {
        if (teacher == null)
        {
            return AcceptanceReport.WasRejected;
        }

        if (teacher.DevelopmentalStage != DevelopmentalStage.Adult)
        {
            return new AcceptanceReport("PE_TeacherRoleOnlyForAdults".Translate());
        }

        var skill = teacher.skills?.GetSkill(SkillDefOf.Social);
        if (skill == null
            || skill.TotallyDisabled)
        {
            return new AcceptanceReport("PE_TeacherRoleRequiresSocialSkill".Translate());
        }

        return AcceptanceReport.WasAccepted;
    }

    public virtual string StudentTooltipFor(Pawn pawn)
    {
        var text = new StringBuilder();
        if (pawn.needs?.learning != null)
        {
            var learningPerHour = CalculateLearningPerTick(pawn) * GenDate.TicksPerHour;
            var learningPerSession = learningPerHour * studyGroup.Duration;
            text.AppendLineIfNotEmpty();
            text.AppendLineTagged(
                $"{NeedDefOf.Learning.LabelCap} {"PE_PerSession".Translate()}:"
                    .AsTipTitle()
                + $" {learningPerSession.ToStringPercent()}"
            );
            var globalLearningFactor = pawn.GetStatValue(StatDefOf.GlobalLearningFactor);
            if (globalLearningFactor > 0)
            {
                text.AppendLine(
                    $" - {StatDefOf.GlobalLearningFactor.LabelCap}: x{globalLearningFactor.ToStringPercent()}");
            }
        }

        return text.ToString().TrimEndNewlines();
    }

    public virtual string TeacherTooltipFor(Pawn pawn)
    {
        return "";
    }

    public void UnassignParticipants(IClassDialog classDialog)
    {
        foreach (var pawn in studyGroup.AllParticipants)
        {
            classDialog.AssignmentsManager.TryUnassignAnyRole(pawn);
        }
    }
}