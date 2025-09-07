using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProgressionEducation
{
    [HotSwappable]
    public class ClassAssignmentsManager : ILordJobAssignmentsManager<ClassRole>
    {
        private readonly Dictionary<ClassRole, List<Pawn>> assignedPawns = [];
        private readonly List<Pawn> spectators = [];
        private readonly List<ClassRole> roles = [];
        private readonly List<Pawn> allPawns = [];

        public List<ClassRole> Roles => roles;

        public ClassAssignmentsManager()
        {
        }

        public ClassAssignmentsManager(TeacherRole teacherRole, StudentRole studentRole, Map map)
        {
            EducationLog.Message($"ClassAssignmentsManager constructor called");
            EducationLog.Message($"teacherRole: {teacherRole?.Label ?? "null"}, category: {teacherRole?.CategoryLabel ?? "null"}");
            EducationLog.Message($"studentRole: {studentRole?.Label ?? "null"}, category: {studentRole?.CategoryLabel ?? "null"}");

            roles.Add(teacherRole);
            roles.Add(studentRole);
            assignedPawns[teacherRole] = [];
            assignedPawns[studentRole] = [];
            allPawns.AddRange(map.mapPawns.FreeColonistsAndPrisonersSpawned);

            EducationLog.Message($"roles count after adding: {roles.Count}");
        }

        public bool SpectatorsAllowed => false;

        public List<Pawn> SpectatorsForReading => spectators;

        public IEnumerable<IGrouping<string, ClassRole>> RoleGroups()
        {
            var grouped = roles.GroupBy(r => r.CategoryLabel.Resolve());
            return grouped;
        }

        public IEnumerable<Pawn> AssignedPawns(ClassRole role)
        {
            return assignedPawns[role];
        }

        public ClassRole ForcedRole(Pawn pawn)
        {
            return null;
        }

        public ClassRole RoleForPawn(Pawn pawn, bool includeForced = true)
        {
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
            if (assignedPawns.ContainsKey(role) && assignedPawns[role].Count > 0)
            {
                var firstPawn = assignedPawns[role][0];
                return firstPawn;
            }
            return null;
        }

        public bool Required(Pawn pawn)
        {
            return false;
        }

        public bool PawnParticipating(Pawn pawn)
        {
            bool isParticipating = assignedPawns.Values.Any(list => list.Contains(pawn)) || spectators.Contains(pawn);
            return isParticipating;
        }

        public bool PawnSpectating(Pawn pawn)
        {
            EducationLog.Message($"PawnSpectating called for pawn: {pawn?.LabelShort ?? "null"}");
            bool isSpectating = spectators.Contains(pawn);
            EducationLog.Message($"PawnSpectating - pawn is spectating: {isSpectating}");
            return isSpectating;
        }

        public bool CanParticipate(Pawn pawn, out TaggedString reason)
        {
            reason = TaggedString.Empty;
            var teacherRole = roles.FirstOrDefault(r => r is TeacherRole);
            if (FirstAssignedPawn(teacherRole) is Pawn pawn2 && pawn2 != pawn)
            {
                var studentRole = roles.FirstOrDefault(r => r is StudentRole);
                var acceptanceReport = studentRole.CanAcceptPawn(pawn);
                if (!acceptanceReport.Accepted)
                {
                    reason = acceptanceReport.Reason.CapitalizeFirst().EndWithPeriod();
                    return false;
                }
            }
            foreach (var role in roles)
            {
                var acceptanceReport = role.CanAcceptPawn(pawn);
                if (acceptanceReport.Accepted)
                {
                    reason = TaggedString.Empty;
                    return true;
                }
                if (reason.NullOrEmpty())
                {
                    reason = acceptanceReport.Reason.CapitalizeFirst().EndWithPeriod();
                }
            }
            return false;
        }

        public bool TryAssignSpectate(Pawn pawn, Pawn insertBefore = null)
        {
            return TryAssignToFirstAvailableRole(pawn, insertBefore);
        }

        public bool TryAssignToFirstAvailableRole(Pawn pawn, Pawn insertBefore = null)
        {
            EducationLog.Message($"TryAssignToFirstAvailableRole called for pawn: {pawn?.LabelShort ?? "null"}");
            foreach (var role in roles)
            {
                if (assignedPawns.ContainsKey(role) &&
                    (role.MaxCount <= 0 || assignedPawns[role].Count < role.MaxCount))
                {
                    if (role.CanAcceptPawn(pawn).Accepted)
                    {
                        foreach (var kvp in assignedPawns)
                        {
                            kvp.Value.Remove(pawn);
                        }
                        if (insertBefore != null && assignedPawns[role].Contains(insertBefore))
                        {
                            EducationLog.Message($"TryAssignToFirstAvailableRole - inserting pawn before: {insertBefore?.LabelShort ?? "null"} in role {role.Label}");
                            int index = assignedPawns[role].IndexOf(insertBefore);
                            assignedPawns[role].Insert(index, pawn);
                        }
                        else
                        {
                            EducationLog.Message($"TryAssignToFirstAvailableRole - adding pawn to role {role.Label}");
                            assignedPawns[role].Add(pawn);
                        }
                        EducationLog.Message($"TryAssignToFirstAvailableRole - successfully assigned pawn to role {role.Label}");
                        return true;
                    }
                }
            }

            EducationLog.Message($"TryAssignToFirstAvailableRole - pawn could not be assigned to any role");
            return false;
        }

        public bool TryAssign(Pawn pawn, ClassRole role, out PsychicRitualRoleDef.Reason reason, PsychicRitualRoleDef.Context context = PsychicRitualRoleDef.Context.Dialog_BeginPsychicRitual, Pawn insertBefore = null)
        {
            EducationLog.Message($"TryAssign called for pawn: {pawn?.LabelShort ?? "null"} to role: {role?.Label ?? "null"}");
            reason = PsychicRitualRoleDef.Reason.None;
            var forcedRole = ForcedRole(pawn);
            if (forcedRole != null && role != forcedRole)
            {
                EducationLog.Message($"TryAssign - pawn has forced role {forcedRole?.Label ?? "null"} which is different from requested role {role?.Label ?? "null"}");
                return false;
            }
            var acceptanceReport = role.CanAcceptPawn(pawn);
            if (!acceptanceReport.Accepted)
            {
                EducationLog.Message($"TryAssign - pawn is not valid for role: {acceptanceReport.Reason}");
                return false;
            }
            if (role.MaxCount > 0 && assignedPawns[role].Count >= role.MaxCount)
            {
                EducationLog.Message($"TryAssign - role max count reached: {role.MaxCount}");
                return false;
            }
            EducationLog.Message($"TryAssign - removing pawn from existing roles");
            foreach (var kvp in assignedPawns)
            {
                kvp.Value.Remove(pawn);
            }
            EducationLog.Message($"TryAssign - removing pawn from spectators");
            spectators.Remove(pawn);
            if (insertBefore != null && assignedPawns[role].Contains(insertBefore))
            {
                EducationLog.Message($"TryAssign - inserting pawn before: {insertBefore?.LabelShort ?? "null"}");
                int index = assignedPawns[role].IndexOf(insertBefore);
                assignedPawns[role].Insert(index, pawn);
            }
            else
            {
                EducationLog.Message($"TryAssign - adding pawn to role");
                assignedPawns[role].Add(pawn);
            }
            EducationLog.Message($"TryAssign - successfully assigned pawn to role");
            return true;
        }

        public bool TryUnassignAnyRole(Pawn pawn)
        {
            EducationLog.Message($"TryUnassignAnyRole called for pawn: {pawn?.LabelShort ?? "null"}");
            var pawnRole = RoleForPawn(pawn);
            if (pawnRole == null)
            {
                EducationLog.Message($"TryUnassignAnyRole - pawn has no role assigned");
                return false;
            }
            var forcedRole = ForcedRole(pawn);
            if (pawnRole == forcedRole)
            {
                EducationLog.Message($"TryUnassignAnyRole - pawn has forced role, cannot unassign");
                return false;
            }
            EducationLog.Message($"TryUnassignAnyRole - removing pawn from role: {pawnRole?.Label ?? "null"}");
            assignedPawns[pawnRole].Remove(pawn);
            return true;
        }

        public void Unassign(Pawn pawn, ClassRole role)
        {
            assignedPawns[role].Remove(pawn);
        }

        public void RemoveParticipant(Pawn pawn)
        {
            EducationLog.Message($"RemoveParticipant called for pawn: {pawn?.LabelShort ?? "null"}");
            TryUnassignAnyRole(pawn);
            EducationLog.Message($"RemoveParticipant - removing pawn from spectators");
            spectators.Remove(pawn);
            EducationLog.Message($"RemoveParticipant completed for pawn: {pawn?.LabelShort ?? "null"}");
        }

        public string PawnNotAssignableReason(Pawn p, ClassRole role)
        {
            EducationLog.Message($"PawnNotAssignableReason called for pawn: {p?.LabelShort ?? "null"} and role: {role?.Label ?? "null"}");
            if (role != null)
            {
                var forcedRole = ForcedRole(p);
                if (forcedRole != null && role != forcedRole)
                {
                    EducationLog.Message($"PawnNotAssignableReason - pawn has forced role {forcedRole?.Label ?? "null"} which is different from requested role {role?.Label ?? "null"}");
                    return "RoleIsLocked".Translate(role.Label);
                }
                var acceptanceReport = role.CanAcceptPawn(p);
                if (!acceptanceReport.Accepted)
                {
                    EducationLog.Message($"PawnNotAssignableReason - pawn is not valid for role: {acceptanceReport.Reason}");
                    return acceptanceReport.Reason;
                }

                EducationLog.Message($"PawnNotAssignableReason - pawn can be assigned to specific role, returning null");
                return null;
            }
            if (CanParticipate(p, out var reason))
            {
                EducationLog.Message($"PawnNotAssignableReason - pawn can participate in some role, returning null");
                return null;
            }

            EducationLog.Message($"PawnNotAssignableReason - pawn cannot participate in any role: {reason}");
            return reason;
        }

        public ClassRole SuggestRoleForPawn(Pawn pawn)
        {
            EducationLog.Message($"SuggestRoleForPawn called for pawn: {pawn?.LabelShort ?? "null"}, stage: {pawn?.DevelopmentalStage.ToString() ?? "null"}");
            var teacherRole = roles.OfType<TeacherRole>().FirstOrDefault();
            if (teacherRole != null && teacherRole.CanAcceptPawn(pawn).Accepted)
            {
                EducationLog.Message($"SuggestRoleForPawn - suggesting teacher role for pawn");
                return teacherRole;
            }
            var studentRole = roles.OfType<StudentRole>().FirstOrDefault();
            if (studentRole != null && studentRole.CanAcceptPawn(pawn).Accepted)
            {
                EducationLog.Message($"SuggestRoleForPawn - suggesting student role for pawn");
                return studentRole;
            }

            EducationLog.Message($"SuggestRoleForPawn - no suitable role found");
            return null;
        }

        public void FillPawns()
        {
            EducationLog.Message($"FillPawns called, allPawns count: {allPawns.Count}");
            foreach (var kvp in assignedPawns)
            {
                EducationLog.Message($"FillPawns - clearing role: {kvp.Key?.Label ?? "null"}, current count: {kvp.Value.Count}");
                kvp.Value.Clear();
            }
            EducationLog.Message($"FillPawns - clearing spectators, current count: {spectators.Count}");
            spectators.Clear();
            foreach (var pawn in allPawns)
            {
                EducationLog.Message($"FillPawns - processing pawn: {pawn?.LabelShort ?? "null"}, stage: {pawn?.DevelopmentalStage.ToString() ?? "null"}");
                if (pawn.Dead || pawn.Downed)
                {
                    EducationLog.Message($"FillPawns - skipping dead/downed pawn: {pawn?.LabelShort ?? "null"}");
                    continue;
                }
                bool assigned = false;
                foreach (var role in roles)
                {
                    if (role != null && role.CanAcceptPawn(pawn).Accepted && assignedPawns[role].Count < role.MaxCount)
                    {
                        EducationLog.Message($"FillPawns - assigning pawn to role: {role.Label}");
                        assignedPawns[role].Add(pawn);
                        assigned = true;
                        break;
                    }
                    else if (role != null && !role.CanAcceptPawn(pawn).Accepted)
                    {
                        EducationLog.Message($"FillPawns - pawn cannot be assigned to role {role.Label}: {role.CanAcceptPawn(pawn).Reason}");
                    }
                    else if (role != null && assignedPawns[role].Count >= role.MaxCount)
                    {
                        EducationLog.Message($"FillPawns - role {role.Label} is full");
                    }
                }

                if (assigned)
                {
                    continue;
                }
                if (SpectatorsAllowed)
                {
                    EducationLog.Message($"FillPawns - adding pawn to spectators");
                    spectators.Add(pawn);
                }
                else
                {
                    EducationLog.Message($"FillPawns - spectators not allowed, pawn not assigned");
                }
            }
            EducationLog.Message($"FillPawns completed");
        }
    }
}
