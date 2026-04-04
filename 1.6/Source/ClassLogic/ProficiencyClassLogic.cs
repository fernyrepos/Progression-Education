using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class ProficiencyClassLogic : ClassSubjectLogic
    {
        public override float LearningSpeedModifier => 
            EducationSettings.Instance.proficiencyClassesLearningSpeedModifier;
        public const int FirearmTeachingDuration = 30000;
        public const int HighTechTeachingDuration = 60000;
        private ProficiencyLevel proficiencyFocus = ProficiencyLevel.Firearm;
        public ProficiencyLevel ProficiencyFocus
        {
            get => proficiencyFocus;
            set
            {
                if (proficiencyFocus != value)
                {
                    proficiencyFocus = value;
                    validLearningBenches = null;
                }
            }
        }

        public ProficiencyClassLogic() : base() { }
        public ProficiencyClassLogic(StudyGroup parent) : base(parent) { }

        public ProficiencyClassLogic(ProficiencyClassLogic other, StudyGroup parent) : base(other, parent)
        {
            proficiencyFocus = other.proficiencyFocus;
        }

        public override string Description =>
            "PE_TrainingProficiency".Translate(ProficiencyFocus.ToStringHuman());

        public float CalculateTechTraitModifier(Pawn pawn)
        {
            if (pawn == null)
            {
                return 1f;
            }
            var techTraitModifier = 1f;
            if (pawn.story.traits.HasTrait(DefsOf.PE_FirearmProficiency))
            {
                if (proficiencyFocus == ProficiencyLevel.Firearm)
                {
                    techTraitModifier += 0.2f;
                }
                else
                {
                    techTraitModifier -= 0.1f;
                }
            }
            if (pawn.story.traits.HasTrait(DefsOf.PE_HighTechProficiency))
            {
                if (ProficiencyFocus == ProficiencyLevel.HighTech)
                {
                    techTraitModifier += 0.2f;
                }
                else
                {
                    techTraitModifier -= 0.1f;
                }
            }
            return techTraitModifier;
        }

        public override void GrantCompletionRewards()
        {
            TraitDef traitDef = null;
            switch (ProficiencyFocus)
            {
                case ProficiencyLevel.Firearm:
                    traitDef = DefsOf.PE_FirearmProficiency;
                    break;
                case ProficiencyLevel.HighTech:
                    traitDef = DefsOf.PE_HighTechProficiency;
                    break;
            }
            foreach (var student in studyGroup.students)
            {
                ProficiencyUtility.GrantProficiencyTrait(student, traitDef);
            }
        }

        public override float CalculateTeacherScore(Pawn teacher)
        {
            if (!studyGroup.GetTeacherRole().CanAcceptPawn(teacher))
            {
                return 0f;
            }
            var social = teacher.skills.GetSkill(SkillDefOf.Social).Level;
            var intelligence = teacher.skills.GetSkill(SkillDefOf.Intellectual).Level;
            var socialImpact = teacher.GetStatValue(StatDefOf.SocialImpact);
            var techTraitModifier = CalculateTechTraitModifier(teacher);
            var progress = (social * 0.6f + intelligence * 0.4f) * socialImpact;
            return progress * techTraitModifier * 0.02f;
        }

        public override float CalculateProgressPerTick(Pawn teacher)
        {
            if (teacher == null || studyGroup.classroom == null)
            {
                return 0f;
            }
            var progress = CalculateTeacherScore(teacher);
            var classroomModifier = studyGroup.classroom.CalculateLearningModifier();
            progress *= classroomModifier;
            progress *= LearningSpeedModifier;
            return progress;
        }

        public override AcceptanceReport IsTeacherQualified(Pawn teacher)
        {
            var isQualified = false;
            var requiredProficiencyLabel = "";

            switch (ProficiencyFocus)
            {
                case ProficiencyLevel.Firearm:
                    requiredProficiencyLabel = ProficiencyLevel.Firearm.ToStringHuman();
                    isQualified = teacher.story.traits.HasTrait(DefsOf.PE_FirearmProficiency) 
                                  || teacher.story.traits.HasTrait(DefsOf.PE_HighTechProficiency);
                    break;
                case ProficiencyLevel.HighTech:
                    requiredProficiencyLabel = ProficiencyLevel.HighTech.ToStringHuman();
                    isQualified = teacher.story.traits.HasTrait(DefsOf.PE_HighTechProficiency);
                    break;
            }

            return !isQualified
                ? new AcceptanceReport("PE_TeacherNotQualifiedProficiency".Translate(teacher.LabelShort, requiredProficiencyLabel))
                : AcceptanceReport.WasAccepted;
        }

        public override AcceptanceReport IsStudentQualified(Pawn student)
        {
            if (student.DevelopmentalStage < DevelopmentalStage.Child)
            {
                return new AcceptanceReport("PE_TooYoung".Translate(student.LabelShortCap));
            }
            if (studyGroup.currentProgress > 0f && !studyGroup.students.NotNullAndContains(student))
            {
                return new AcceptanceReport("PE_CannotAddOngoing".Translate());
            }
            var hasProficiency = false;
            var requiredProficiencyLabel = "";
            switch (ProficiencyFocus)
            {
                case ProficiencyLevel.Firearm:
                    requiredProficiencyLabel = ProficiencyLevel.Firearm.ToStringHuman();
                    hasProficiency = student.story.traits.HasTrait(DefsOf.PE_FirearmProficiency)
                        || student.story.traits.HasTrait(DefsOf.PE_HighTechProficiency);
                    break;
                case ProficiencyLevel.HighTech:
                    requiredProficiencyLabel = ProficiencyLevel.HighTech.ToStringHuman();
                    hasProficiency = student.story.traits.HasTrait(DefsOf.PE_HighTechProficiency);
                    break;
            }

            return hasProficiency
                ? new AcceptanceReport("PE_StudentAlreadyHasProficiency".Translate(student.LabelShort, requiredProficiencyLabel))
                : AcceptanceReport.WasAccepted;
        }

        public override void DrawConfigurationUI(Rect rect, ref float curY, IClassDialog classDialog)
        {
            DrawProficiencyUI(rect, ref curY, classDialog);
            var progressPerTick = CalculateProgressPerTick(studyGroup.teacher);
            if (progressPerTick > 0)
            {
                var estimatedTicks = Mathf.CeilToInt(studyGroup.semesterGoal / progressPerTick);
                Widgets.Label(new Rect(rect.x, curY, 360f, 25f), "PE_StudyTimeNeeded".Translate(estimatedTicks.ToStringTicksToPeriod()));
                curY += 30f;
                var sessionsNeeded = Mathf.CeilToInt((float)estimatedTicks / (GenDate.TicksPerHour * studyGroup.Duration));
                Widgets.Label(new Rect(rect.x, curY, 360f, 25f), "PE_StudySessionsNeeded".Translate(sessionsNeeded));
                curY += 30f;
            }
        }

        private void DrawProficiencyUI(Rect rect, ref float curY, IClassDialog classDialog)
        {
            var proficiencyLabel = ProficiencyFocus.ToStringHuman();
            switch (classDialog)
            {
                case Dialog_CreateClass:
                    Widgets.Label(new Rect(rect.x, curY, 150f, 25f), "PE_ProficiencyFocus".Translate());
                    if (Widgets.ButtonText(new Rect(rect.x + 160f, curY, 200f, 25f), proficiencyLabel))
                    {
                        List<FloatMenuOption> options =
                        [
                            new FloatMenuOption(ProficiencyLevel.Firearm.ToStringHuman().CapitalizeFirst(), () => {
                                ProficiencyFocus = ProficiencyLevel.Firearm;
                                studyGroup.semesterGoal = FirearmTeachingDuration;
                                studyGroup.subjectLogic.AutoAssignStudents(classDialog);
                            }),
                            new FloatMenuOption(ProficiencyLevel.HighTech.ToStringHuman().CapitalizeFirst(), () => {
                                ProficiencyFocus = ProficiencyLevel.HighTech;
                                studyGroup.semesterGoal = HighTechTeachingDuration;
                                studyGroup.subjectLogic.AutoAssignStudents(classDialog);
                            }),
                        ];
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                    curY += 30;
                    break;
                case Dialog_EditClass:
                    Widgets.Label(new Rect(rect.x, curY, 150f, 25f), "PE_ProficiencyFocus".Translate());
                    Widgets.Label(new Rect(rect.x + 160f, curY, 200f, 25f), proficiencyLabel);
                    curY += 30;
                    break;
            }
        }

        public override string TeacherTooltipFor(Pawn pawn)
        {
            if (pawn == null)
            {
                return "";
            }
            var text = new StringBuilder(base.TeacherTooltipFor(pawn));
            text.AppendLineIfNotEmpty();
            if (studyGroup is { classroom: not null, Map: not null, semesterGoal: > 0 })
            {
                if (pawn.skills.GetSkill(SkillDefOf.Social) is SkillRecord social)
                {
                    text.AppendInNewLine(SkillDefOf.Social.LabelCap);
                    text.Append(": ");
                    text.Append(social.Level);
                }
                if (pawn.skills.GetSkill(SkillDefOf.Intellectual) is SkillRecord intellectual)
                {
                    text.AppendInNewLine(SkillDefOf.Intellectual.LabelCap);
                    text.Append(": ");
                    text.Append(intellectual.Level);
                }
                if (pawn.GetStatValue(StatDefOf.SocialImpact) is var socialImpact)
                {
                    text.AppendInNewLine(StatDefOf.SocialImpact.LabelCap);
                    text.Append(": ");
                    text.Append(socialImpact.ToStringPercent());
                }
                if (CalculateTechTraitModifier(pawn) is var techTraitModifier)
                {
                    text.AppendInNewLine("PE_ProficiencyFocus".Translate());
                    text.Append(": ");
                    text.Append(techTraitModifier.ToString("F1"));
                }
                text.AppendLineIfNotEmpty();

                if (CalculateTeacherScore(pawn) is var progressPerTick and > 0f)
                {
                    var progressPerHour = progressPerTick * GenDate.TicksPerHour;
                    var progressPercentagePerHour = progressPerHour / studyGroup.semesterGoal;
                    text.AppendInNewLine("PE_TeachingHourlyBase".Translate());
                    text.Append(": ");
                    text.Append(progressPercentagePerHour.ToStringPercent());
                    text.Append("PE_PerHour".Translate());
                }
            }
            return text.ToString();
        }

        public override string StudentTooltipFor(Pawn pawn)
        {
            return "";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref proficiencyFocus, "proficiencyFocus");
        }

    }
}
