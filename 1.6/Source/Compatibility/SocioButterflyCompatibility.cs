using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ProgressionEducation;

internal static class SocioButterflyCompatibility
{
    private const string PackageId = "LovelyDovey.Recreation.WithEuterpe";
    private const string SocialUtilitiesTypeName =
        "RecreationalSexWithEuterpe.SocialUtilities";

    private static bool initialized;
    private static Func<Pawn, Pawn, InteractionDef> findRandomConversationInteraction;

    public static void TryRunClassConversation(Pawn teacher,
        List<Pawn> attendingStudents)
    {
        if (!TryInitialize()
            || teacher?.interactions == null
            || attendingStudents.NullOrEmpty())
        {
            return;
        }

        var student = attendingStudents
            .Where(candidate => candidate != null
                                && candidate != teacher
                                && candidate.interactions != null)
            .RandomElementWithFallback();
        if (student == null)
        {
            return;
        }

        var initiator = teacher;
        var recipient = student;
        if (Rand.Bool)
        {
            (initiator, recipient) = (recipient, initiator);
        }

        try
        {
            var interaction = findRandomConversationInteraction(initiator,
                recipient);
            if (interaction != null)
            {
                initiator.interactions.TryInteractWith(recipient, interaction);
            }
        }
        catch (Exception exception)
        {
            Log.ErrorOnce(
                $"[Progression: Education] Socio Butterfly class conversation failed: {exception}",
                183746921);
        }
    }

    private static bool TryInitialize()
    {
        if (initialized)
        {
            return findRandomConversationInteraction != null;
        }

        initialized = true;
        if (!ModsConfig.IsActive(PackageId))
        {
            return false;
        }

        var socialUtilitiesType = GenTypes.GetTypeInAnyAssembly(
            SocialUtilitiesTypeName, "RecreationalSexWithEuterpe");
        if (socialUtilitiesType == null)
        {
            Log.ErrorOnce(
                "[Progression: Education] Socio Butterfly is active, but its SocialUtilities type was not found.",
                183746922);
            return false;
        }

        var method = AccessTools.Method(socialUtilitiesType,
            "FindRandomConversationInteraction",
            [typeof(Pawn), typeof(Pawn)]);
        if (method == null)
        {
            Log.ErrorOnce(
                "[Progression: Education] Socio Butterfly is active, but its conversation API was not found.",
                183746923);
            return false;
        }

        try
        {
            findRandomConversationInteraction =
                (Func<Pawn, Pawn, InteractionDef>)Delegate.CreateDelegate(
                    typeof(Func<Pawn, Pawn, InteractionDef>), method);
            return true;
        }
        catch (Exception exception)
        {
            Log.ErrorOnce(
                $"[Progression: Education] Could not bind Socio Butterfly's conversation API: {exception}",
                183746924);
            return false;
        }
    }
}
