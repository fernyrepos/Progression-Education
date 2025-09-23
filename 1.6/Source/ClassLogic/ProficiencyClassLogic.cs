using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class ProficiencyClassLogic : ClassSubjectLogic
    {
        public const int FirearmTeachingDuration = 6000;
        public const int HighTechTeachingDuration = 12000;
        public ProficiencyLevel proficiencyFocus = ProficiencyLevel.Firearm;

        public ProficiencyClassLogic() : base() { }
        public ProficiencyClassLogic(StudyGroup parent) : base(parent) { }

        public override string Description => "PE_TrainingProficiency".Translate(proficiencyFocus.ToStringHuman());

        public override void GrantCompletionRewards()
        {
            TraitDef traitDef = null;
            switch (proficiencyFocus)
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
            float teacherSocial = teacher.skills.GetSkill(SkillDefOf.Social).Level;
            float teacherIntelligence = teacher.skills.GetSkill(SkillDefOf.Intellectual).Level;
            float progress = teacherSocial * 0.6f + teacherIntelligence * 0.4f;
            float techTraitModifier = 1f;

            if (teacher.story.traits.HasTrait(DefsOf.PE_FirearmProficiency))
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

            if (teacher.story.traits.HasTrait(DefsOf.PE_HighTechProficiency))
            {
                if (proficiencyFocus == ProficiencyLevel.HighTech)
                {
                    techTraitModifier += 0.2f;
                }
                else
                {
                    techTraitModifier -= 0.1f;
                }
            }
            techTraitModifier = Mathf.Max(0.1f, techTraitModifier);
            return progress * techTraitModifier;
        }

        public override float CalculateProgressPerTick()
        {
            var classroom = studyGroup.classroom;
            float boardModifier = classroom.CalculateLearningModifier();
            float modMultiplier = EducationSettings.Instance.proficiencyClassesLearningSpeedModifier;
            float progress = CalculateTeacherScore(studyGroup.teacher) * boardModifier * modMultiplier;
            return progress;
        }

        public override AcceptanceReport IsTeacherQualified(Pawn teacher)
        {
            bool isQualified = false;
            string requiredProficiencyLabel = "";

            switch (proficiencyFocus)
            {
                case ProficiencyLevel.Firearm:
                    requiredProficiencyLabel = ProficiencyLevel.Firearm.ToStringHuman();
                    isQualified = teacher.story.traits.HasTrait(DefsOf.PE_FirearmProficiency) || teacher.story.traits.HasTrait(DefsOf.PE_HighTechProficiency);
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

        public override void AutoAssignStudents(Dialog_CreateClass createClassDialog)
        {
            if (studyGroup.classroom?.LearningBoard != null)
            {
                var room = studyGroup.GetRoom();
                int benchCount = room.ContainedAndAdjacentThings.Count(t => t.def.HasComp(typeof(RimWorld.CompStatEntrySchoolDesk)));
                UnassignUnqualifiedPawns(createClassDialog);
                AssignBestTeacher(createClassDialog);
                HandleRoleAutoAssignment(createClassDialog, createClassDialog.StudentRole, benchCount);
                Log.Message($"Auto-assigned students and teacher for class '{studyGroup.className}'");
            }
        }

        public override AcceptanceReport IsStudentQualified(Pawn student)
        {
            bool isQualified = false;
            string requiredProficiencyLabel = "";

            switch (proficiencyFocus)
            {
                case ProficiencyLevel.Firearm:
                    requiredProficiencyLabel = ProficiencyLevel.Firearm.ToStringHuman();
                    isQualified = student.story.traits.HasTrait(DefsOf.PE_FirearmProficiency)
                        || student.story.traits.HasTrait(DefsOf.PE_HighTechProficiency);
                    break;
                case ProficiencyLevel.HighTech:
                    requiredProficiencyLabel = ProficiencyLevel.HighTech.ToStringHuman();
                    isQualified = student.story.traits.HasTrait(DefsOf.PE_HighTechProficiency);
                    break;
            }

            return isQualified
                ? new AcceptanceReport("PE_StudentAlreadyHasProficiency".Translate(student.LabelShort, requiredProficiencyLabel))
                : AcceptanceReport.WasAccepted;
        }

        public override void DrawConfigurationUI(Rect rect, ref float curY, Map map, Dialog_CreateClass createClassDialog)
        {
            Widgets.Label(new Rect(rect.x, curY, 150f, 25f), "PE_ProficiencyFocus".Translate());
            if (Widgets.ButtonText(new Rect(rect.x + 160f, curY, 200f, 25f), proficiencyFocus.ToStringHuman()))
            {
                List<FloatMenuOption> options =
                [
                    new FloatMenuOption(ProficiencyLevel.Firearm.ToStringHuman(), () => {
                        proficiencyFocus = ProficiencyLevel.Firearm;
                        studyGroup.semesterGoal = FirearmTeachingDuration;
                        studyGroup.subjectLogic.AutoAssignStudents(createClassDialog);
                    }),
                    new FloatMenuOption(ProficiencyLevel.HighTech.ToStringHuman(), () => {
                        proficiencyFocus = ProficiencyLevel.HighTech;
                        studyGroup.semesterGoal = HighTechTeachingDuration;
                        studyGroup.subjectLogic.AutoAssignStudents(createClassDialog);
                    }),
                ];
                Find.WindowStack.Add(new FloatMenu(options));
            }
            curY += 30f;

            Widgets.Label(new Rect(rect.x, curY, 200f, 25f), "PE_Requirements".Translate());
            curY += 25f;

            var room = studyGroup.GetRoom();
            if (room != null)
            {
                int count = studyGroup.students.Count;
                int benchCount = room.ContainedAndAdjacentThings.Count(t => t.def.HasComp(typeof(RimWorld.CompStatEntrySchoolDesk)));
                string label = "PE_SchoolDesks".Translate();
                string presentText = "";
                if (benchCount < count || benchCount < 1)
                {
                    GUI.color = Color.red;
                    presentText = " " + "PE_Present".Translate(benchCount);
                }
                if (benchCount < 1 && count < 1)
                {
                    count = 1;
                }
                Widgets.Label(new Rect(rect.x + 10f, curY, 300f, 25f), $"{count}x {label}{presentText}");
                GUI.color = Color.white;
                curY += 25f;
            }
        }

        public override string TeacherTooltipFor(Pawn pawn)
        {
            var social = pawn.skills.GetSkill(SkillDefOf.Social);
            var intellectual = pawn.skills.GetSkill(SkillDefOf.Intellectual);
            string text = $"{social.def.LabelCap}: {social.Level}\n{intellectual.def.LabelCap}: {intellectual.Level}";
            var map = studyGroup.GetRoom()?.Map;
            if (studyGroup.classroom != null && map != null && studyGroup.semesterGoal > 0)
            {
                float progressPerTick = CalculateProgressPerTick();
                if (progressPerTick > 0f)
                {
                    float num = progressPerTick * 2500f / studyGroup.semesterGoal;
                    text += "\n\n" + "PE_EstimatedHourlyProgress".Translate() + ": +" + num.ToStringPercent() + "PE_PerHour".Translate();
                }
            }
            return text;
        }

        public override string StudentTooltipFor(Pawn pawn)
        {
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref proficiencyFocus, "proficiencyFocus");
        }

        public override AcceptanceReport ArePrerequisitesMet()
        {
            var room = studyGroup.GetRoom();
            if (room != null)
            {
                int benchCount = room.ContainedAndAdjacentThings.Count(t => t.def.HasComp(typeof(RimWorld.CompStatEntrySchoolDesk)));
                if (benchCount < studyGroup.students.Count)
                {
                    return new AcceptanceReport("PE_NotEnoughBenches".Translate("PE_SchoolDesks".Translate(), studyGroup.students.Count, benchCount));
                }
            }
            return AcceptanceReport.WasAccepted;
        }
    }
}