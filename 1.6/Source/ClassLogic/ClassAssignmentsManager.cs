using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ProgressionEducation;

[HotSwappable]
public class ClassAssignmentsManager : ILordJobAssignmentsManager<ClassRole>
{
    private readonly List<Pawn> allPawns = [];
    private readonly Dictionary<ClassRole, List<Pawn>> assignedPawns = [];
    private readonly Dictionary<string, Pawn> forcedRoles;
    private readonly List<Pawn> requiredPawns;

    public ClassAssignmentsManager()
    {
    }

    public ClassAssignmentsManager(
        TeacherRole teacherRole,
        StudentRole studentRole,
        Map map,
        Dictionary<string, Pawn> forcedRoles = null)
    {
        EducationLog.Message("ClassAssignmentsManager constructor called");
        EducationLog.Message(
            $"teacherRole: {teacherRole?.Label ?? "null"}, category: {teacherRole?.CategoryLabel ?? "null"}");
        EducationLog.Message(
            $"studentRole: {studentRole?.Label ?? "null"}, category: {studentRole?.CategoryLabel ?? "null"}");

        Roles.Add(teacherRole);
        Roles.Add(studentRole);
        assignedPawns[teacherRole!] = [];
        assignedPawns[studentRole!] = [];
        allPawns.AddRange(map.mapPawns.FreeColonistsAndPrisonersSpawned);
        this.forcedRoles = forcedRoles;
        requiredPawns = [];
        if (forcedRoles != null)
        {
            requiredPawns.AddRange(forcedRoles.Values);
        }

        EducationLog.Message($"roles count after adding: {Roles.Count}");
    }

    public List<ClassRole> Roles { get; } = [];

    public string PawnNotAssignableReason(Pawn pawn, ClassRole role)
    {
        if (role == null)
        {
            return CanParticipate(pawn, out var reason) ? null : reason;
        }

        var forcedRole = ForcedRole(pawn);
        if (forcedRole != null
            && forcedRole != role)
        {
            return "RoleIsLocked".Translate(role.LabelCap);
        }

        var report = role.CanAcceptPawn(pawn);
        return report.Accepted ? null : report.Reason.CapitalizeFirst().EndWithPeriod();
    }

    public bool SpectatorsAllowed => false;

    public List<Pawn> SpectatorsForReading { get; } = [];

    public IEnumerable<IGrouping<string, ClassRole>> RoleGroups()
    {
        var grouped = Roles.GroupBy(r => r.CategoryLabel.Resolve());
        return grouped;
    }

    public IEnumerable<Pawn> AssignedPawns(ClassRole role)
    {
        return assignedPawns[role];
    }

    public ClassRole ForcedRole(Pawn pawn)
    {
        if (forcedRoles == null)
        {
            return null;
        }

        return GetRole(forcedRoles.FirstOrDefault(kvp => kvp.Value == pawn).Key);
    }

    public ClassRole RoleForPawn(Pawn pawn, bool includeForced = true)
    {
        if (SpectatorsForReading.Contains(pawn))
        {
            return null;
        }

        if (includeForced && forcedRoles != null)
        {
            return GetRole(forcedRoles.FirstOrDefault(kvp => kvp.Value == pawn).Key);
        }

        foreach (var kvp in assignedPawns)
        {
            if (kvp.Value.Contains(pawn))
            {
                return kvp.Key;
            }
        }

        return null;
    }

    public Pawn FirstAssignedPawn(ClassRole role)
    {
        if (assignedPawns.ContainsKey(role)
            && assignedPawns[role].Count > 0)
        {
            var firstPawn = assignedPawns[role][0];
            return firstPawn;
        }

        return null;
    }

    public bool Required(Pawn pawn)
    {
        return requiredPawns.NotNullAndContains(pawn);
    }

    public bool PawnParticipating(Pawn pawn)
    {
        var isParticipating =
            assignedPawns.Values.Any(list => list.Contains(pawn))
            || SpectatorsForReading.Contains(pawn);
        return isParticipating;
    }

    public bool PawnSpectating(Pawn pawn)
    {
        EducationLog.Message($"PawnSpectating called for pawn: {pawn?.LabelShort ?? "null"}");
        var isSpectating = SpectatorsForReading.Contains(pawn);
        EducationLog.Message($"PawnSpectating - pawn is spectating: {isSpectating}");
        return isSpectating;
    }

    public bool CanParticipate(Pawn pawn, out TaggedString reason)
    {
        reason = null;
        foreach (var role in Roles)
        {
            var report = role.CanAcceptPawn(pawn);
            if (report.Accepted)
            {
                reason = null;
                break;
            }

            reason = report.Reason;
        }

        return reason.NullOrEmpty();
    }

    public bool TryAssignSpectate(Pawn pawn, Pawn insertBefore = null)
    {
        return TryAssignToFirstAvailableRole(pawn, insertBefore);
    }

    public bool TryAssign(
        Pawn pawn,
        ClassRole role,
        out PsychicRitualRoleDef.Reason reason,
        PsychicRitualRoleDef.Context context =
            PsychicRitualRoleDef.Context.Dialog_BeginPsychicRitual,
        Pawn insertBefore = null)
    {
        EducationLog.Message(
            $"TryAssign called for pawn: {pawn?.LabelShort ?? "null"} to role: {role?.LabelCap ?? "null"}");
        reason = PsychicRitualRoleDef.Reason.None;
        if (pawn == null
            || role == null)
        {
            EducationLog.Message("-> pawn or role is null, cannot assign");
            return false;
        }

        if (!assignedPawns.ContainsKey(role))
        {
            EducationLog.Message(
                $"-> role is not one of the supported roles: {assignedPawns.Keys.ToStringSafeEnumerable()}");
            return false;
        }

        if (ForcedRole(pawn) is ClassRole forcedRole
            && role != forcedRole)
        {
            EducationLog.Message(
                $"-> pawn has forced role {forcedRole.LabelCap} which is different from requested role {role.LabelCap}");
            return false;
        }

        var report = role.CanAcceptPawn(pawn);
        if (!report.Accepted)
        {
            EducationLog.Message($"-> pawn is not valid for role: {report.Reason}");
            return false;
        }

        if (role.MaxCount > 0
            && assignedPawns[role].Count >= role.MaxCount)
        {
            EducationLog.Message($"-> role max count reached: {role.MaxCount}");
            return false;
        }

        EducationLog.Message("-> removing pawn from existing roles");
        foreach (var kvp in assignedPawns)
        {
            kvp.Value.Remove(pawn);
        }

        EducationLog.Message("-> removing pawn from spectators");
        SpectatorsForReading.Remove(pawn);
        if (insertBefore != null
            && assignedPawns[role].IndexOf(insertBefore) is var index and >= 0)
        {
            EducationLog.Message($"-> inserting pawn before: {insertBefore.LabelShort}");
            assignedPawns[role].Insert(index, pawn);
        }
        else
        {
            EducationLog.Message("-> adding pawn to role");
            assignedPawns[role].Add(pawn);
        }

        EducationLog.Message("-> successfully assigned pawn to role");
        return true;
    }

    public bool TryUnassignAnyRole(Pawn pawn)
    {
        EducationLog.Message($"TryUnassignAnyRole called for pawn: {pawn?.LabelShort ?? "null"}");
        if (pawn == null)
        {
            EducationLog.Message("-> pawn is null, cannot unassign");
            return false;
        }

        var forcedRole = ForcedRole(pawn);
        foreach (var kvp in assignedPawns)
        {
            if (kvp.Key == forcedRole)
            {
                EducationLog.Message("-> pawn has forced role, cannot unassign");
                continue;
            }

            if (kvp.Value.Remove(pawn))
            {
                EducationLog.Message("-> completed");
                if (SpectatorsAllowed)
                {
                    EducationLog.Message("-> adding to spectators");
                    SpectatorsForReading.Add(pawn);
                }

                return true;
            }
        }

        return false;
    }

    public void RemoveParticipant(Pawn pawn)
    {
        EducationLog.Message($"RemoveParticipant called for pawn: {pawn?.LabelShort ?? "null"}");
        if (pawn == null)
        {
            EducationLog.Message("-> pawn is null, cannot unassign");
            return;
        }

        TryUnassignAnyRole(pawn);
        EducationLog.Message("-> removing pawn from spectators");
        SpectatorsForReading.Remove(pawn);
        EducationLog.Message("-> completed");
        allPawns.Remove(pawn);
        allPawns.Add(pawn);
    }

    public void FillPawns()
    {
        EducationLog.Message($"FillPawns called, allPawns count: {allPawns.Count}");
        foreach (var kvp in assignedPawns)
        {
            EducationLog.Message(
                $"FillPawns - clearing role: {kvp.Key?.Label ?? "null"}, current count: {kvp.Value.Count}");
            kvp.Value.Clear();
        }

        EducationLog.Message(
            $"FillPawns - clearing spectators, current count: {SpectatorsForReading.Count}");
        SpectatorsForReading.Clear();
        var remainingPawns = new List<Pawn>(allPawns);
        foreach (var role in Roles)
        {
            FillRole(role, remainingPawns, out var unassignedPawns);
            remainingPawns = unassignedPawns;
        }

        foreach (var pawn in remainingPawns)
        {
            if (SpectatorsAllowed)
            {
                EducationLog.Message("FillPawns - adding pawn to spectators");
                SpectatorsForReading.Add(pawn);
            }
            else
            {
                EducationLog.Message("FillPawns - spectators not allowed, pawn not assigned");
            }
        }

        EducationLog.Message("FillPawns completed");
    }

    public void FillRole(ClassRole role, List<Pawn> pawns, out List<Pawn> unassignedPawns)
    {
        EducationLog.Message(
            $"FillRole called for role {role?.LabelCap ?? "null"} with {pawns.ToStringSafeEnumerable()}");
        unassignedPawns = [];
        if (role == null)
        {
            EducationLog.Message("-> No role given. Returning all pawns as unassigned.");
            unassignedPawns = new List<Pawn>(pawns);
            return;
        }

        foreach (var pawn in pawns)
        {
            EducationLog.Message(
                $"-> Processing pawn: {pawn.LabelShort}, stage: {pawn.DevelopmentalStage}");
            if (!pawn.CanAttendClass())
            {
                EducationLog.Message(
                    $"-> Skipping {pawn.LabelShort} because they are unable to attend classes");
                continue;
            }

            var report = role.CanAcceptPawn(pawn);
            var isFull = assignedPawns[role].Count >= role.MaxCount;

            if (report.Accepted
                && !isFull)
            {
                EducationLog.Message($"-> Assigning role: {role.LabelCap}");
                assignedPawns[role].Add(pawn);
            }
            else
            {
                if (!report.Accepted)
                {
                    EducationLog.Message(
                        $"-> Cannot assign to role {role.LabelCap} because: {report.Reason}");
                }
                else if (isFull)
                {
                    EducationLog.Message(
                        $"-> Cannot assign to role {role.LabelCap} because it is full");
                }

                unassignedPawns.Add(pawn);
            }
        }
    }

    public ClassRole GetRole(string roleId)
    {
        if (roleId == null)
        {
            return null;
        }

        if (!Roles.NullOrEmpty())
        {
            foreach (var role in Roles)
            {
                if (role.RoleId == roleId)
                {
                    return role;
                }
            }
        }

        return null;
    }

    public bool TryAssignToFirstAvailableRole(Pawn pawn, Pawn insertBefore = null)
    {
        EducationLog.Message(
            $"TryAssignToFirstAvailableRole called for pawn: {pawn?.LabelShort ?? "null"}");
        foreach (var role in Roles
                     .Where(role => assignedPawns.ContainsKey(role)
                                    && (role.MaxCount <= 0
                                        || assignedPawns[role].Count < role.MaxCount)
                                    && role.CanAcceptPawn(pawn).Accepted))
        {
            foreach (var kvp in assignedPawns)
            {
                kvp.Value.Remove(pawn);
            }

            if (insertBefore != null
                && assignedPawns[role].Contains(insertBefore))
            {
                EducationLog.Message(
                    $"TryAssignToFirstAvailableRole - inserting pawn before: {insertBefore.LabelShort ?? "null"} in role {role.Label}");
                var index = assignedPawns[role].IndexOf(insertBefore);
                assignedPawns[role].Insert(index, pawn);
            }
            else
            {
                EducationLog.Message(
                    $"TryAssignToFirstAvailableRole - adding pawn to role {role.Label}");
                assignedPawns[role].Add(pawn);
            }

            EducationLog.Message(
                $"TryAssignToFirstAvailableRole - successfully assigned pawn to role {role.Label}");
            return true;
        }

        EducationLog.Message(
            "TryAssignToFirstAvailableRole - pawn could not be assigned to any role");
        return false;
    }

    public void Unassign(Pawn pawn, ClassRole role)
    {
        assignedPawns[role].Remove(pawn);
    }
}