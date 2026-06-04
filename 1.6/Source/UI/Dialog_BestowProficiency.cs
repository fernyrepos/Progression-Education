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

    public override Vector2 InitialSize => new Vector2(450f, 600f);

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

        var profName = extension.trait.LabelCap;
        if (extension.trait.degreeDatas != null && extension.trait.degreeDatas.Count > 0)
        {
            profName = extension.trait.degreeDatas[0].label.CapitalizeFirst();
        }

        var description = "PE_BestowProficiencyDescWithProf".Translate(profName.Colorize(ColorLibrary.SkyBlue));
        var descHeight = Text.CalcHeight(description, inRect.width);
        Widgets.Label(new Rect(0, 35, inRect.width, descHeight), description);

        var pawns = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction
            .Where(p => p.IsFreeColonist && !p.WorkTypeIsDisabled(WorkTypeDefOf.Research)).ToList();

        var listTop = 35f + descHeight + 10f;
        var listRect = new Rect(0, listTop, inRect.width, inRect.height - listTop - 50f);
        var rowHeight = 50f;
        var viewRect = new Rect(0, 0, listRect.width - 16f, pawns.Count * rowHeight);
        Widgets.BeginScrollView(listRect, ref scrollPos, viewRect);
        float curY = 0;
        foreach (var p in pawns)
        {
            var rowRect = new Rect(0, curY, viewRect.width, rowHeight - 4f);

            if (Widgets.ButtonInvisible(rowRect))
            {
                ProficiencyUtility.GrantProficiencyTrait(p, extension.trait);
                Find.LetterStack.ReceiveLetter(extension.title, extension.desc, LetterDefOf.PositiveEvent);
                Close();
            }

            Widgets.DrawOptionBackground(rowRect, false);

            var portraitRect = new Rect(rowRect.x + 4f, rowRect.y + 3f, 40f, 40f);
            var tex = PortraitsCache.Get(p, new Vector2(40f, 40f), Rot4.South);
            GUI.DrawTexture(portraitRect, tex);

            Text.Anchor = TextAnchor.MiddleLeft;
            var labelRect = new Rect(portraitRect.xMax + 10f, rowRect.y, rowRect.width - portraitRect.width - 20f, rowRect.height);
            Widgets.Label(labelRect, p.LabelShortCap);
            Text.Anchor = TextAnchor.UpperLeft;

            curY += rowHeight;
        }
        Widgets.EndScrollView();

        if (canClose && Widgets.ButtonText(new Rect(inRect.width - 100, inRect.height - 40, 100, 35), "CloseButton".Translate()))
        {
            Close();
        }
    }
}