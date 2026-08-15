using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public class Alert_ClassSuspended : Alert
{
    private readonly List<StudyGroup> suspendedClasses = [];

    public Alert_ClassSuspended()
    {
        defaultLabel = "PE_ClassSuspended".Translate();
        defaultExplanation = "PE_ClassSuspendedDesc".Translate();
        defaultPriority = AlertPriority.High;
    }

    public override string GetLabel()
    {
        CollectSuspendedClasses();
        if (suspendedClasses.Count == 1)
        {
            return "PE_ClassSuspendedNamed".Translate(suspendedClasses[0].className);
        }

        return "PE_ClassesSuspended".Translate(suspendedClasses.Count);
    }

    public override TaggedString GetExplanation()
    {
        CollectSuspendedClasses();
        var sb = new StringBuilder();
        sb.AppendLine("PE_ClassSuspendedDesc".Translate());
        foreach (var group in suspendedClasses)
        {
            var teacher = group.teacher != null
                ? group.teacher.LabelShortCap
                : "PE_ClassSuspendedNoTeacher".Translate().ToString();
            sb.AppendLine("PE_ClassSuspendedLine".Translate(group.className, teacher));
        }

        return sb.ToString().TrimEnd();
    }

    public override AlertReport GetReport()
    {
        if (!EducationMod.settings.showClassSuspendedAlert)
        {
            return false;
        }

        CollectSuspendedClasses();
        return suspendedClasses.Count > 0;
    }

    public override void OnClick()
    {
        if (DefsOf.PE_Education == null || Find.MainTabsRoot == null)
        {
            return;
        }

        Find.MainTabsRoot.SetCurrentTab(DefsOf.PE_Education);
    }

    private void CollectSuspendedClasses()
    {
        suspendedClasses.Clear();
        if (Current.ProgramState != ProgramState.Playing || Find.World == null)
        {
            return;
        }

        var manager = EducationManager.Instance;
        if (manager == null)
        {
            return;
        }

        foreach (var group in manager.StudyGroups)
        {
            if (group == null || !group.suspended || group.IsCompleted)
            {
                continue;
            }

            suspendedClasses.Add(group);
        }
    }
}
