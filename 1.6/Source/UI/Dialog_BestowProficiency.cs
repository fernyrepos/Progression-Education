using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ProgressionEducation;

public class Dialog_BestowProficiency : Window
{
    private ResearchGrantsTrait extension;
    private Vector2 scrollPos;
    private bool canClose;

    public override Vector2 InitialSize => new Vector2(400f, 500f);

    public Dialog_BestowProficiency(ResearchGrantsTrait ext)
    {
        extension = ext;
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;

        var pawnsCount = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction
            .Count(p => p.IsFreeColonist && !p.WorkTypeIsDisabled(WorkTypeDefOf.Research));

        canClose = pawnsCount == 0;
        closeOnCancel = canClose;
        closeOnAccept = canClose;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(0, 0, inRect.width, 35), "PE_BestowProficiency".Translate());
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(0, 35, inRect.width, 40), "PE_BestowProficiencyDesc".Translate());

        var pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction
            .Where(p => p.IsFreeColonist && !p.WorkTypeIsDisabled(WorkTypeDefOf.Research)).ToList();

        var listRect = new Rect(0, 80, inRect.width, inRect.height - 130);
        var viewRect = new Rect(0, 0, listRect.width - 16, pawns.Count * 30);
        Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);
        float curY = 0;
        foreach (var p in pawns)
        {
            if (Widgets.ButtonText(new Rect(0, curY, viewRect.width, 26), p.LabelShort))
            {
                ProficiencyUtility.GrantProficiencyTrait(p, extension.trait);
                Find.LetterStack.ReceiveLetter(extension.title, extension.desc, LetterDefOf.PositiveEvent);
                Close();
            }
            curY += 30;
        }
        Widgets.EndScrollView();

        if (canClose && Widgets.ButtonText(new Rect(inRect.width - 100, inRect.height - 40, 100, 35), "CloseButton".Translate()))
        {
            Close();
        }
    }
}
