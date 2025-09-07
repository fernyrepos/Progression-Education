using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class SkillClassLogic : ClassSubjectLogic
    {
        public SkillDef skillFocus;

        public SkillClassLogic() : base() { }
        public SkillClassLogic(StudyGroup studyGroup) : base(studyGroup) { }

        public override string Description => "PE_TrainingSkill".Translate(skillFocus.label);

        public override void GrantCompletionRewards()
        {
        }

        public override float CalculateTeacherScore(Pawn teacher)
        {
            if (skillFocus == null)
            {
                return 0f;
            }
            float teacherSocial = teacher.skills.GetSkill(SkillDefOf.Social).Level;
            float relevantSkill = teacher.skills.GetSkill(skillFocus).Level;
            float teacherIntelligence = teacher.skills.GetSkill(SkillDefOf.Intellectual).Level;
            return (teacherSocial * 0.4f) + (relevantSkill * 0.3f) + (teacherIntelligence * 0.3f);
        }

        public override float CalculateProgressPerTick()
        {
            if (skillFocus == null || studyGroup.teacher == null)
            {
                return 0f;
            }
            var classroom = studyGroup.classroom;
            float boardModifier = classroom.CalculateLearningModifier();
            float modMultiplier = EducationSettings.Instance.skillClassesLearningSpeedModifier;
            float progress = CalculateTeacherScore(studyGroup.teacher) * boardModifier * modMultiplier;
            return progress;
        }

        public override AcceptanceReport IsTeacherQualified(Pawn teacher)
        {
            return skillFocus != null && teacher.skills.GetSkill(skillFocus).Level < 5
                ? new AcceptanceReport("PE_TeacherNotQualifiedSkill".Translate(teacher.LabelShort, skillFocus.LabelCap))
                : AcceptanceReport.WasAccepted;
        }

        public override AcceptanceReport IsStudentQualified(Pawn student)
        {
            return skillFocus != null && student.skills.GetSkill(skillFocus).TotallyDisabled
                ? new AcceptanceReport("PE_StudentSkillDisabled".Translate(student.LabelShort, skillFocus.LabelCap))
                : AcceptanceReport.WasAccepted;
        }

        public override void AutoAssignStudents(Dialog_CreateClass createClassDialog)
        {
            if (studyGroup.classroom?.LearningBoard != null && skillFocus != null)
            {
                UnassignUnqualifiedPawns(createClassDialog);

                var requirement = skillFocus.GetModExtension<SkillBuildingRequirement>();
                if (requirement != null && !requirement.requiredBuildings.NullOrEmpty())
                {
                    var room = studyGroup.GetRoom();
                    int benchCount = room.ContainedAndAdjacentThings.Count(t => requirement.requiredBuildings.Contains(t.def));
                    HandleRoleAutoAssignment(createClassDialog, createClassDialog.StudentRole, benchCount);
                    AssignBestTeacher(createClassDialog);
                }
            }
        }

        public override void ApplyLearningTick(Pawn student)
        {
            base.ApplyLearningTick(student);
            var classroom = studyGroup.classroom;
            float boardModifier = classroom.CalculateLearningModifier();
            float modMultiplier = EducationSettings.Instance.skillClassesLearningSpeedModifier;
            float xpGain = 1f;
            float finalXpGain = xpGain * boardModifier * modMultiplier * EducationSettings.Instance.globalLearningSpeedModifier;
            student.skills.Learn(skillFocus, finalXpGain, false);
        }

        public override void DrawConfigurationUI(Rect rect, ref float curY, Map map, Dialog_CreateClass createClassDialog)
        {
            Widgets.Label(new Rect(rect.x, curY, 150f, 25f), "PE_SkillFocus".Translate());
            if (Widgets.ButtonText(new Rect(rect.x + 160f, curY, 200f, 25f), skillFocus?.LabelCap ?? "PE_Select".Translate()))
            {
                List<FloatMenuOption> options = [];
                foreach (var skillDef in DefDatabase<SkillDef>.AllDefs)
                {
                    options.Add(new FloatMenuOption(skillDef.LabelCap, () =>
                    {
                        skillFocus = skillDef;
                        studyGroup.subjectLogic.AutoAssignStudents(createClassDialog);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            curY += 30f;

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, curY, 360f, 25f), "PE_SemesterGoal".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            curY += 30f;

            var sliderRect = new Rect(rect.x, curY, 360f, 25f);
            int newSemesterGoal = (int)Widgets.HorizontalSlider(sliderRect, studyGroup.semesterGoal, 1000, 100000, false, null, "1,000 xp", "100,000 xp", 100);
            if (newSemesterGoal != studyGroup.semesterGoal)
            {
                studyGroup.semesterGoal = newSemesterGoal;
            }
            curY += 30f;

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, curY - 15f, 360f, 25f), studyGroup.semesterGoal.ToString("N0") + " xp");
            Text.Anchor = TextAnchor.UpperLeft;
            curY += 20f;

            if (studyGroup.classroom != null)
            {
                float progressPerTick = studyGroup.CalculateProgressPerTick();
                if (progressPerTick > 0)
                {
                    int estimatedTicks = (int)(studyGroup.semesterGoal / progressPerTick);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(new Rect(rect.x, curY, 360f, 25f), "PE_StudyTimeNeeded".Translate(estimatedTicks.ToStringTicksToPeriod()));
                    Text.Anchor = TextAnchor.UpperLeft;
                    curY += 30f;
                }
            }

            Widgets.Label(new Rect(rect.x, curY, 200f, 25f), "PE_Requirements".Translate());
            curY += 25f;

            var room = studyGroup.GetRoom();

            var requirement = skillFocus?.GetModExtension<SkillBuildingRequirement>();
            if (requirement != null && !requirement.requiredBuildings.NullOrEmpty())
            {
                int count = studyGroup.students.Count;
                string label = !string.IsNullOrEmpty(requirement.requirementLabel) ? requirement.requirementLabel : requirement.requiredBuildings.Select(b => b.label).ToCommaList();
                int benchCount = room.ContainedAndAdjacentThings.Count(t => requirement.requiredBuildings.Contains(t.def));
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
            var relevantSkill = skillFocus != null ? pawn.skills.GetSkill(skillFocus) : null;
            string text = $"{social.def.LabelCap}: {social.Level}\n{intellectual.def.LabelCap}: {intellectual.Level}";
            if (relevantSkill != null && relevantSkill != social && relevantSkill != intellectual)
            {
                text += $"\n{relevantSkill.def.LabelCap}: {relevantSkill.Level}";
            }
            var map = studyGroup.GetRoom()?.Map;
            if (studyGroup.classroom != null && map != null)
            {
                float progressPerTick = CalculateProgressPerTick();
                if (progressPerTick > 0f)
                {
                    float num = progressPerTick * 2500f;
                    text += "\n\n" + "PE_EstimatedHourlyProgress".Translate() + ": " + num.ToString("F0") + " " + "PE_XpPerHour".Translate();
                }
            }
            return text;
        }

        public override string StudentTooltipFor(Pawn pawn)
        {
            if (skillFocus != null)
            {
                return $"{skillFocus.LabelCap}: {pawn.skills.GetSkill(skillFocus).Level}";
            }
            return null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref skillFocus, "skillFocus");
        }

        public override AcceptanceReport ArePrerequisitesMet()
        {
            var requirement = skillFocus.GetModExtension<SkillBuildingRequirement>();
            if (requirement != null && !requirement.requiredBuildings.NullOrEmpty())
            {
                var room = studyGroup.GetRoom();
                if (room != null)
                {
                    int benchCount = room.ContainedAndAdjacentThings.Count(t => requirement.requiredBuildings.Contains(t.def));
                    if (benchCount < studyGroup.students.Count)
                    {
                        return new AcceptanceReport("PE_NotEnoughBenches".Translate(requirement.requiredBuildings.Select(b => b.label).ToCommaList(), studyGroup.students.Count, benchCount));
                    }
                }
            }
            return AcceptanceReport.WasAccepted;
        }
    }
}
