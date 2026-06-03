using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.Steam;

namespace ProgressionEducation;

[StaticConstructorOnStartup]
public abstract class PawnRoleSelectionWidgetBase_Fixed<RoleType>(
    ILordJobCandidatePool candidatePool,
    ILordJobAssignmentsManager<RoleType> assignments)
    : IPawnRoleSelectionWidget
    where RoleType : class, ILordJobRole
{
    public const int EdgeScrollSpeedWhileDragging = 1000;

    public const int HeadlineIconSize = 20;

    public const int PawnPortraitHeight = 50;

    public const int PawnPortraitHeightTotal = 70;

    public const int PawnPortraitIconSize = 20;

    public const int PawnPortraitLabelHeight = 20;

    public const int PawnPortraitMargin = 4;

    public const int PawnPortraitWidth = 50;

    public const int PawnsListHorizontalGap = 26;
    public const int PawnsListPadding = 4;

    private static readonly object DropContextNotParticipating = new();

    private static readonly object DropContextSpectator = new();

    private static readonly List<Pawn> nonParticipatingPawnCandidatesTmp = new();

    private static readonly List<IGrouping<string, RoleType>> rolesGroupedTmp = new();

    private static readonly List<Pawn> tmpAssignedPawns = new();

    private static readonly List<Action> tmpDelayedGuiCalls = new();

    private static readonly List<Pawn> tmpSelectedPawns = new();

    private static readonly Texture2D WarningIcon =
        Resources.Load<Texture2D>("Textures/UI/Widgets/Warning");

    private readonly StringBuilder tipSB = new();

    protected ILordJobAssignmentsManager<RoleType> assignments = assignments;
    protected ILordJobCandidatePool candidatePool = candidatePool;

    private int dragAndDropGroup;

    private RoleType highlightedRole;

    private Rect? lastHoveredDropArea;

    protected float listScrollViewHeight;

    private int pawnsListEdgeScrollDirection;

    protected Vector2 scrollPositionPawns;

    public bool showIdeoIcon = true;

    public void DrawPawnList(Rect listRectPawns)
    {
        var num = listScrollViewHeight > listRectPawns.height ? 16f : 0f;
        var viewRect = new Rect(0f, 0f, listRectPawns.width - num,
            listScrollViewHeight);
        Widgets.BeginScrollView(listRectPawns, ref scrollPositionPawns,
            viewRect);
        try
        {
            DrawPawnListInternal(viewRect, listRectPawns);
        }
        finally
        {
            Widgets.EndScrollView();
        }
    }

    public void WindowUpdate()
    {
        scrollPositionPawns.y += pawnsListEdgeScrollDirection * 1000 * Time.deltaTime;
    }

    private string CannotAssignReason(
        Pawn draggable,
        IEnumerable<RoleType> roles,
        out RoleType firstRole,
        out bool mustReplace,
        bool isReplacing = false)
    {
        var num = 0;
        var num2 = 0;
        var lordJobRoles = roles?.ToList() ?? [];
        firstRole = lordJobRoles.FirstOrDefault();
        mustReplace = false;
        var text = assignments.PawnNotAssignableReason(draggable, firstRole)
            .CapitalizeFirst();
        if (text == null
            && firstRole == null)
        {
            text = SpectatorFilterReason(draggable);
        }

        if (text == null
            && firstRole != null)
        {
            var flag = true;
            foreach (var role in lordJobRoles)
            {
                if (!assignments.AssignedPawns(role).Any())
                {
                    flag = false;
                    break;
                }

                if (assignments.AssignedPawns(role)
                    .Any(item => assignments.ForcedRole(item) != role))
                {
                    flag = false;
                }
            }

            if (flag)
            {
                text = "RoleIsLocked".Translate(firstRole.Label);
            }
        }

        if (text != null
            || lordJobRoles.Any(r => assignments.RoleForPawn(draggable) == r))
        {
            return text;
        }

        foreach (var role2 in lordJobRoles)
        {
            if (role2.MaxCount <= 0)
            {
                num = -1;
            }

            if (num != -1)
            {
                num += role2.MaxCount;
            }

            num2 += assignments.AssignedPawns(role2).Count();
        }

        if (num < 0
            || num > num2)
        {
            return null;
        }

        mustReplace = true;
        if (isReplacing)
        {
            return null;
        }

        if (firstRole != null)
        {
            text = "MaxPawnsPerRole".Translate(firstRole.Label, num);
        }

        return text;
    }

    private bool DoTryAssign(
        Pawn pawn,
        IEnumerable<RoleType> roleGroup,
        bool sendMessage = true,
        Pawn insertBefore = null,
        bool doSound = true)
    {
        var lordJobRoles = roleGroup.ToList();
        if (sendMessage && SendTryAssignMessages(pawn, lordJobRoles))
        {
            return false;
        }

        if (!sendMessage
            && CannotAssignReason(pawn, lordJobRoles, out _,
                out _)
            != null)
        {
            return false;
        }

        if (!lordJobRoles
                .Any(item => assignments.TryAssign(pawn, item, out _,
                    default, insertBefore)))
        {
            return false;
        }

        Notify_AssignmentsChanged();
        if (doSound)
        {
            SoundDefOf.DropElement.PlayOneShotOnCamera();
        }

        return true;
    }

    private void DrawPawnListInternal(Rect viewRect, Rect listRect)
    {
        rolesGroupedTmp.Clear();
        rolesGroupedTmp.AddRange(assignments.RoleGroups());
        try
        {
            var num = DragAndDropWidget.NewGroup();
            dragAndDropGroup = num == -1 ? dragAndDropGroup : num;
            var maxPawnsPerRow = Mathf.FloorToInt((viewRect.width - 8f) / 54f);
            var rowHeight = 0f;
            var curY = 0f;
            var curX = 0f;
            foreach (var item in rolesGroupedTmp)
            {
                var localRoleGroup = item;
                var val = item.First();
                var num3 = item.Sum(item2 => item2.MaxCount);
                var extraInfo = ExtraPawnAssignmentInfo(localRoleGroup);
                var enumerable = item.SelectMany(assignments.AssignedPawns);
                TaggedString taggedString =
                    Find.ActiveLanguageWorker.Pluralize(val.CategoryLabelCap, num3);
                var mp = Event.current.mousePosition;
                var pawns = enumerable.ToList();
                DrawRoleGroup(
                    viewRect,
                    pawns,
                    taggedString,
                    num3,
                    maxPawnsPerRow,
                    ref curX,
                    ref curY,
                    ref rowHeight,
                    (p, dropPos) =>
                    {
                        var pawn2 = (Pawn)DragAndDropWidget.DraggableAt(dragAndDropGroup,
                            mp);
                        if (pawn2 != null)
                        {
                            TryAssignReplace(
                                p,
                                localRoleGroup,
                                pawn2
                            );
                        }
                        else
                        {
                            TryAssign(
                                p,
                                localRoleGroup,
                                true,
                                (Pawn)DragAndDropWidget.GetDraggableAfter(
                                    dragAndDropGroup,
                                    dropPos),
                                true,
                                true
                            );
                        }
                    },
                    item,
                    extraInfo,
                    pawns.Any() && pawns.All(p => assignments.Required(p)),
                    WarningIcon,
                    null,
                    p =>
                    {
                        if (!assignments.Required(p))
                        {
                            assignments.RemoveParticipant(p);
                            SoundDefOf.Click.PlayOneShotOnCamera();
                        }
                    },
                    ShouldGrayOut
                );
            }

            var allCandidatePawns = candidatePool.AllCandidatePawns;
            var spectatorLabel = SpectatorsLabel();
            if (assignments.SpectatorsAllowed)
            {
                var spectatorsForReading = assignments.SpectatorsForReading;
                DrawRoleGroup(
                    viewRect,
                    spectatorsForReading,
                    spectatorLabel,
                    allCandidatePawns.Count,
                    maxPawnsPerRow,
                    ref curX,
                    ref curY,
                    ref rowHeight,
                    (p, dropPos) =>
                    {
                        if (!SendTryAssignMessages(p, null))
                        {
                            assignments.TryAssignSpectate(p,
                                (Pawn)DragAndDropWidget.GetDraggableAfter(
                                    dragAndDropGroup,
                                    dropPos));
                            SoundDefOf.DropElement.PlayOneShotOnCamera();
                        }
                    },
                    DropContextSpectator,
                    null,
                    false,
                    null,
                    p => TryAssignAnyRole(p),
                    p =>
                    {
                        if (assignments.Required(p))
                        {
                            return;
                        }

                        assignments.RemoveParticipant(p);
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    },
                    ShouldGrayOut
                );
            }

            nonParticipatingPawnCandidatesTmp.Clear();
            nonParticipatingPawnCandidatesTmp.AddRange(allCandidatePawns);
            nonParticipatingPawnCandidatesTmp.AddRange(candidatePool.NonAssignablePawns);
            nonParticipatingPawnCandidatesTmp.RemoveDuplicates();

            var assignmentCheck = assignments.RoleGroups()
                .SelectMany(r => r)
                .OfType<TeacherRole>()
                .Cast<RoleType>()
                .Select(r => assignments.FirstAssignedPawn(r))
                .FirstOrDefault();

            var selectedPawns = assignmentCheck == null
                ? TeacherBestFit()
                : StudentBestFit();

            curY += rowHeight;
            rowHeight = 0f;
            curX = 0f;

            DrawRoleGroup(
                viewRect,
                selectedPawns,
                NotParticipatingLabel(),
                nonParticipatingPawnCandidatesTmp.Count,
                maxPawnsPerRow,
                ref curX,
                ref curY,
                ref rowHeight,
                (p, _) =>
                {
                    assignments.RemoveParticipant(p);
                    SoundDefOf.DropElement.PlayOneShotOnCamera();
                },
                DropContextNotParticipating,
                null,
                false,
                null,
                p =>
                {
                    var text5 = CannotAssignReason(p, null, out _,
                        out _);
                    if (text5 != null)
                    {
                        if (!TryAssignAnyRole(p))
                        {
                            Messages.Message(text5, LookTargets.Invalid,
                                MessageTypeDefOf.RejectInput, false);
                        }
                    }
                    else
                    {
                        assignments.TryAssignSpectate(p);
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    }
                },
                p =>
                {
                    var list = new List<FloatMenuOption>();
                    var text4 = CannotAssignReason(
                        p,
                        null,
                        out _,
                        out _
                    );
                    if (assignments.SpectatorsAllowed)
                    {
                        var action = text4 != null
                            ? null
                            : (Action)delegate { assignments.TryAssignSpectate(p); };
                        list.Add(new FloatMenuOption(
                            PostProcessFloatLabel(spectatorLabel,
                                text4),
                            action));
                    }

                    foreach (var item3 in rolesGroupedTmp)
                    {
                        var localRoleGroup2 = item3;
                        var val2 = item3.First();
                        text4 = CannotAssignReason(
                            p,
                            localRoleGroup2,
                            out _,
                            out var mustReplace2,
                            true
                        );
                        var replacing = mustReplace2
                            ? localRoleGroup2.SelectMany(role => assignments.AssignedPawns(role))
                                .Last()
                            : null;
                        var action2 = text4 != null
                            ? null
                            : (Action)delegate
                            {
                                if (mustReplace2)
                                {
                                    TryAssignReplace(p, localRoleGroup2,
                                        replacing);
                                }
                                else
                                {
                                    TryAssign(p, localRoleGroup2);
                                }
                            };
                        list.Add(new FloatMenuOption(
                            PostProcessFloatLabel(val2.LabelCap,
                                text4, replacing), action2));
                    }

                    Find.WindowStack.Add(new FloatMenu(list));
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                },
                ShouldGrayOut
            );
            highlightedRole = null;
            curY += rowHeight + 4f;
            using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft,
                       ColorLibrary.Grey))
            {
                Widgets.Label(
                    new Rect(viewRect.x, curY, viewRect.width, 24f),
                    SteamDeck.IsSteamDeckInNonKeyboardMode
                        ? "DragPawnsToRolesInfoController".Translate()
                        : "DragPawnsToRolesInfo".Translate());
            }

            curY += 20f;
            if (Event.current.type == EventType.Layout)
            {
                listScrollViewHeight = curY;
            }

            foreach (var tmpDelayedGuiCall in tmpDelayedGuiCalls)
            {
                tmpDelayedGuiCall();
            }

            var obj = DragAndDropWidget.CurrentlyDraggedDraggable();
            var pawn =
                (Pawn)DragAndDropWidget.DraggableAt(dragAndDropGroup,
                    Event.current.mousePosition);
            if (obj != null)
            {
                var obj2 = DragAndDropWidget.HoveringDropArea(dragAndDropGroup);
                if (obj2 != null)
                {
                    var rect = DragAndDropWidget.HoveringDropAreaRect(dragAndDropGroup);
                    if (lastHoveredDropArea.HasValue
                        && rect.HasValue
                        && rect != lastHoveredDropArea)
                    {
                        SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    }

                    lastHoveredDropArea = rect;
                }

                if (obj2 != null
                    && obj2 != DropContextNotParticipating)
                {
                    var grouping = obj2 as IGrouping<string, RoleType>;
                    var text2 = CannotAssignReason(
                        (Pawn)obj,
                        grouping,
                        out var firstRole,
                        out _,
                        grouping != null && pawn != null);
                    var text3 = firstRole == null
                        ? null
                        : ExtraPawnAssignmentInfo(grouping, (Pawn)obj);
                    if (!string.IsNullOrWhiteSpace(text2)
                        || !string.IsNullOrWhiteSpace(text3))
                    {
                        var text = string.IsNullOrWhiteSpace(text2) ? text3 : text2;
                        var color = string.IsNullOrWhiteSpace(text2)
                            ? ColorLibrary.Yellow
                            : ColorLibrary.RedReadable;
                        Text.Font = GameFont.Small;
                        var vector = Text.CalcSize(text);
                        var r2 = new Rect(UI.MousePositionOnUI.x - vector.x / 2f,
                                UI.screenHeight - UI.MousePositionOnUI.y - vector.y - 10f,
                                vector.x,
                                vector.y)
                            .ExpandedBy(5f);
                        Find.WindowStack.ImmediateWindow(
                            47839543,
                            r2,
                            WindowLayer.Super,
                            () =>
                            {
                                Text.Font = GameFont.Small;
                                GUI.color = color;
                                Widgets.Label(r2.AtZero().ContractedBy(5f), text);
                                GUI.color = Color.white;
                            }
                        );
                    }
                }
            }
        }
        finally
        {
            tmpDelayedGuiCalls.Clear();
        }

        pawnsListEdgeScrollDirection = 0;
        if (DragAndDropWidget.CurrentlyDraggedDraggable() != null)
        {
            var rect2 = new Rect(viewRect.x, scrollPositionPawns.y, viewRect.width,
                30f);
            var rect3 = new Rect(viewRect.x, scrollPositionPawns.y + (listRect.height - 30f),
                viewRect.width, 30f);
            if (Mouse.IsOver(rect2))
            {
                pawnsListEdgeScrollDirection = -1;
            }
            else if (Mouse.IsOver(rect3))
            {
                pawnsListEdgeScrollDirection = 1;
            }
        }
    }

    private void DrawPawnPortrait(
        Rect rect,
        Pawn pawn,
        TryGetPredicate<Pawn, TaggedString> isGrayedOutPredicate = null,
        Action clickHandler = null,
        Action rightClickHandler = null)
    {
        if (Mouse.IsOver(rect)
            && Event.current.type == EventType.MouseDown
            && Event.current.button == 1)
        {
            rightClickHandler?.Invoke();
        }

        var mp = Event.current.mousePosition;
        if (!assignments.Required(pawn)
            && DragAndDropWidget.Draggable(
                dragAndDropGroup,
                rect,
                pawn,
                clickHandler,
                () =>
                {
                    lastHoveredDropArea =
                        DragAndDropWidget.HoveringDropAreaRect(dragAndDropGroup,
                            mp);
                }))
        {
            rect.position = Event.current.mousePosition;
            tmpDelayedGuiCalls.Add(delegate
            {
                DrawPawnPortraitInternal(rect, pawn, true, 0.9f,
                    isGrayedOutPredicate);
            });
        }
        else
        {
            DrawPawnPortraitInternal(rect, pawn, false, 1f,
                isGrayedOutPredicate);
            Widgets.DrawHighlightIfMouseover(rect);
        }
    }

    private void DrawPawnPortraitInternal(Rect r, Pawn pawn, bool dragging, float scale,
        TryGetPredicate<Pawn, TaggedString> isGrayedOutPredicate = null)
    {
        using (new ProfilerBlock("DrawPawnPortraitInternal"))
        {
            var rect = new Rect(r.x, r.y, r.width * scale, 50f * scale);
            var rect2 = new Rect(r.x, r.y + 50f * scale, r.width * scale,
                20f * scale);
            var result = TaggedString.Empty;
            var flag = isGrayedOutPredicate?.Invoke(pawn, out result) ?? false;
            var material = flag ? TexUI.GrayscaleGUI : null;
            GenUI.DrawTextureWithMaterial(rect, ColonistBar.BGTex,
                material);
            if (ShouldDrawHighlight(highlightedRole, pawn))
            {
                Widgets.DrawHighlight(rect.ContractedBy(3f));
            }

            var texture = PortraitsCache.Get(pawn, new Vector2(100f, 100f),
                Rot4.South,
                new Vector3(0f, 0f, 0.1f),
                1.5f);
            GenUI.DrawTextureWithMaterial(rect, texture, material);
            Widgets.DrawRectFast(rect2, Widgets.WindowBGFillColor);
            var curY = rect.yMax;
            var curX = rect.xMax;
            var iconSize = 20f * scale;
            PawnPortraitIconsDrawer.DrawPawnPortraitIcons(rect, pawn,
                assignments.Required(pawn),
                flag, ref curX,
                ref curY, iconSize, showIdeoIcon,
                out var tooltipActive);
            using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleCenter,
                       flag ? Color.gray : Color.white))
            {
                Widgets.LabelFit(rect2, pawn.LabelShortCap);
                if (!tooltipActive
                    && !dragging)
                {
                    TooltipHandler.TipRegion(
                        new Rect(r.x, r.y, r.width * scale, 70f * scale),
                        PawnTooltip(pawn, result));
                }
            }
        }
    }

    private void DrawRoleGroup(Rect viewRect, IEnumerable<Pawn> selectedPawns, string headline,
        int maxPawns,
        int maxPawnsPerRow,
        ref float curX,
        ref float curY,
        ref float rowHeight,
        Action<Pawn, Vector2> assignAction,
        object dropAreaContext,
        string extraInfo = null,
        bool locked = false,
        Texture2D extraInfoIcon = null,
        Action<Pawn> clickHandler = null,
        Action<Pawn> rightClickHandler = null,
        TryGetPredicate<Pawn, TaggedString> isGrayedOutPredicate = null)
    {
        using (new ProfilerBlock("DrawRoleGroup"))
        {
            tmpSelectedPawns.AddRange(selectedPawns);
            try
            {
                var num = Mathf.Min(maxPawns, tmpSelectedPawns.Count + 1);
                if (num == 0)
                {
                    num = 1;
                }

                var num2 = Mathf.CeilToInt(num / (float)maxPawnsPerRow);
                var num3 = Mathf.Min(maxPawnsPerRow, num);
                var num4 = num3 * 50 + (num3 - 1) * 4;
                var num5 = num2 * 70 + (num2 - 1) * 4;
                var vector = Text.CalcSize(headline);
                var num6 = 60;
                var num7 = Mathf.Max(num4, (int)vector.x + num6 + 10);
                var num8 = Mathf.FloorToInt((viewRect.width - (curX + 26f)) / 50f);
                var flag = (num8 > 0 && num8 < maxPawns) || maxPawns + 2 >= maxPawnsPerRow;
                if (flag)
                {
                    num4 = maxPawnsPerRow * 50 + (maxPawnsPerRow - 1) * 4;
                }

                if (curX + num7 + 26f > viewRect.width || flag)
                {
                    curY += rowHeight;
                    rowHeight = 0f;
                    curX = 0f;
                }

                var num9 = 0f;
                var rect = new Rect(viewRect.x + curX, viewRect.y + num9 + curY,
                    vector.x,
                    vector.y);
                var rect2 = new Rect(rect.xMax + 10f, rect.y + (vector.y - 20f) / 2f,
                    num6, 20f);
                num9 += vector.y + 4f;
                var rect3 = new Rect(viewRect.x + curX, viewRect.y + num9 + curY,
                    num4 + 8,
                    num5 + 8);
                num9 += rect3.height + 10f;
                rowHeight = Mathf.Max(rowHeight, num9);
                curX += num7 + 26;
                GUI.color = locked ? ColorLibrary.Grey : Color.white;
                Widgets.Label(rect, headline);
                GUI.color = Color.white;
                Widgets.DrawRectFast(rect3, Widgets.MenuSectionBGFillColor);
                if (dropAreaContext is IGrouping<string, RoleType> source
                    && Mouse.IsOver(rect3))
                {
                    highlightedRole = source.First();
                }

                if (locked)
                {
                    var rect4 = new Rect(rect2.x, rect2.y, 20f, 20f);
                    rect2.x += rect4.width;
                    rect2.width -= rect4.height;
                    Widgets.DrawTextureFitted(rect4, IdeoUIUtility.LockedTex,
                        1f);
                    TooltipHandler.TipRegion(rect4, () => "Required".Translate(),
                        93457856);
                }

                if (extraInfo != null)
                {
                    var rect5 = new Rect(rect2.x, rect2.y, 20f, 20f);
                    rect2.x += rect5.width;
                    rect2.width -= rect5.height;
                    GUI.color = Mouse.IsOver(rect5)
                        ? Color.white
                        : new Color(0.8f, 0.8f, 0.8f, 1f);
                    Widgets.DrawTextureFitted(rect5, extraInfoIcon ?? WarningIcon,
                        1f);
                    GUI.color = Color.white;
                    TooltipHandler.TipRegion(rect5, () => extraInfo,
                        34899345);
                }

                var rect6 = rect3.ContractedBy(4f);
                DragAndDropWidget.DropArea(dragAndDropGroup, rect6,
                    delegate (object pawn)
                    {
                        assignAction((Pawn)pawn, Event.current.mousePosition);
                    }, dropAreaContext);
                if (Mouse.IsOver(rect6))
                {
                    Widgets.DrawBoxSolidWithOutline(rect3,
                        new Color(0.3f, 0.3f, 0.3f, 1f),
                        new Color(0.5f, 0.5f, 0.5f, 1f),
                        3);
                }

                GenUI.DrawElementStack(
                    rect6,
                    70f,
                    tmpSelectedPawns,
                    (r, p) =>
                    {
                        DrawPawnPortrait(
                            r,
                            p,
                            isGrayedOutPredicate,
                            () => clickHandler?.Invoke(p),
                            () => rightClickHandler?.Invoke(p)
                        );
                    },
                    _ => 50f,
                    4f,
                    4f,
                    false);
            }
            finally
            {
                tmpSelectedPawns.Clear();
            }
        }
    }

    public virtual string ExtraInfoForRole(RoleType role, Pawn pawnToBeAssigned,
        IEnumerable<Pawn> currentlyAssigned)
    {
        return null;
    }

    private string ExtraPawnAssignmentInfo(IEnumerable<RoleType> roleGroup,
        Pawn pawnToBeAssigned = null)
    {
        var lordJobRoles = roleGroup.ToList();
        var val = lordJobRoles.First();
        var enumerable = assignments.AssignedPawns(val);
        if (pawnToBeAssigned != null)
        {
            enumerable = enumerable.Append(pawnToBeAssigned).Distinct();
        }

        var text = ExtraInfoForRole(val, pawnToBeAssigned,
            enumerable);
        if (val.MinCount <= 0
            || pawnToBeAssigned != null
            || assignments.FirstAssignedPawn(val) != null)
        {
            return text;
        }

        var num = lordJobRoles.Sum(item => item.MinCount);

        text = num <= 1
            ? text
              + "MessageLordJobNeedsAtLeastOneRolePawn".Translate(val.Label.Resolve())
            : (string)(text
                       + "MessageLordJobNeedsAtLeastNumRolePawn"
                           .Translate(Find.ActiveLanguageWorker.Pluralize(val.Label),
                               num));

        return text;
    }

    protected virtual string ExtraTipContents(Pawn pawn)
    {
        return null;
    }

    public virtual void Notify_AssignmentsChanged()
    {
    }

    public virtual string NotParticipatingLabel()
    {
        return "NotParticipating".Translate();
    }

    private string PawnTooltip(Pawn pawn, TaggedString cannotAssignReason)
    {
        tipSB.Clear();
        tipSB.AppendLineTagged(pawn.LabelShortCap.AsTipTitle());
        var text = ExtraTipContents(pawn);
        if (!text.NullOrEmpty())
        {
            tipSB.AppendLine(text);
        }

        if (!cannotAssignReason.NullOrEmpty())
        {
            tipSB.AppendLine();
            tipSB.AppendLineTagged(cannotAssignReason.Colorize(ColorLibrary.RedReadable));
        }

        return tipSB.ToString();
    }

    private static string PostProcessFloatLabel(string label, string unavailableReason,
        Pawn replacing = null)
    {
        var text = label;
        if (unavailableReason != null)
        {
            text += " ("
                    + "DisabledLower".Translate().CapitalizeFirst()
                    + ": "
                    + unavailableReason.CapitalizeFirst()
                    + ")";
        }

        if (replacing != null)
        {
            text += " (" + "RitualRoleReplaces".Translate(replacing.Named("PAWN")) + ")";
        }

        return "AssignToRole".Translate(text);
    }

    private bool SendTryAssignMessages(Pawn pawn, IEnumerable<RoleType> roleGroup,
        bool isReplacing = false)
    {
        var text = CannotAssignReason(pawn, roleGroup, out _,
            out _, isReplacing);
        if (text == null)
        {
            return false;
        }

        Messages.Message(text, LookTargets.Invalid,
            MessageTypeDefOf.RejectInput, false);
        return true;
    }

    public virtual bool ShouldDrawHighlight(RoleType role, Pawn pawn)
    {
        return false;
    }

    public virtual bool ShouldGrayOut(Pawn pawn, out TaggedString reason)
    {
        return !assignments.CanParticipate(pawn, out reason);
    }

    public virtual string SpectatorFilterReason(Pawn pawn)
    {
        return null;
    }

    public virtual string SpectatorsLabel()
    {
        return "Spectators".Translate();
    }

    private List<Pawn> StudentBestFit()
    {
        var candidates = nonParticipatingPawnCandidatesTmp
            .Where(p => !assignments.PawnParticipating(p));

        return assignments.RoleGroups()
            .SelectMany(r => r)
            .OfType<StudentRole>()
            .SelectMany(r => candidates.Select(p => new
            {
                Pawn = p,
                Qualified = assignments.CanParticipate(p, out _),
                Score = r.ScoreFor(p),
            }))
            .OrderByDescending(s => s.Qualified)
            .ThenByDescending(s => s.Score)
            .Select(s => s.Pawn)
            .Distinct()
            .ToList();
    }


    private List<Pawn> TeacherBestFit()
    {
        var candidates = nonParticipatingPawnCandidatesTmp
            .Where(p => !assignments.PawnParticipating(p));

        return assignments.RoleGroups()
            .SelectMany(r => r)
            .OfType<TeacherRole>()
            .SelectMany(r => candidates.Select(p => new
            {
                Pawn = p,
                Qualified = assignments.CanParticipate(p, out _),
                Score = r.ScoreFor(p),
            }))
            .OrderByDescending(s => s.Qualified)
            .ThenByDescending(s => s.Score)
            .Select(s => s.Pawn)
            .Distinct()
            .ToList();
    }

    private bool TryAssign(Pawn pawn, IEnumerable<RoleType> roleGroup, bool sendMessage = true,
        Pawn insertBefore = null, bool doSound = true, bool insertLast = false)
    {
        try
        {
            var num = 0;
            var lordJobRoles = roleGroup.ToList();
            foreach (var item in lordJobRoles)
            {
                if (item.MaxCount == 0)
                {
                    num = -1;
                }
                else if (item.MaxCount > 0)
                {
                    num += item.MaxCount;
                }

                tmpAssignedPawns.AddRange(assignments.AssignedPawns(item));
            }

            if ((num > 0 && tmpAssignedPawns.Count == num)
                || (insertBefore != null && !tmpAssignedPawns.Contains(insertBefore)))
            {
                return DoTryAssign(pawn, lordJobRoles, sendMessage,
                    null, doSound);
            }

            foreach (var tmpAssignedPawn in tmpAssignedPawns)
            {
                assignments.TryUnassignAnyRole(tmpAssignedPawn);
            }

            if (insertBefore == null)
            {
                if (insertLast)
                {
                    tmpAssignedPawns.Add(pawn);
                }
                else
                {
                    tmpAssignedPawns.Insert(0, pawn);
                }
            }
            else
            {
                tmpAssignedPawns.Insert(tmpAssignedPawns.IndexOf(insertBefore), pawn);
            }

            var result = false;
            foreach (var tmpAssignedPawn2 in tmpAssignedPawns)
            {
                var flag = DoTryAssign(tmpAssignedPawn2, lordJobRoles,
                    sendMessage && tmpAssignedPawn2 == pawn, null,
                    tmpAssignedPawn2 == pawn);
                if (tmpAssignedPawn2 == pawn)
                {
                    result = flag;
                }
            }

            return result;
        }
        finally
        {
            tmpAssignedPawns.Clear();
        }
    }

    private bool TryAssignAnyRole(Pawn p)
    {
        string text = null;
        var flag = rolesGroupedTmp.Count == 1;
        foreach (var item in rolesGroupedTmp)
        {
            text = CannotAssignReason(p, item, out _,
                out _, true);
            if (text == null
                && TryAssign(p, item, false))
            {
                return true;
            }
        }

        foreach (var item2 in rolesGroupedTmp)
        {
            text = CannotAssignReason(p, item2, out _,
                out var mustReplace2, true);
            if (text == null)
            {
                var replacing = mustReplace2
                    ? item2.SelectMany(role => assignments.AssignedPawns(role)).Last()
                    : null;
                if (TryAssignReplace(p, item2, replacing))
                {
                    return true;
                }
            }
        }

        if (flag && text != null)
        {
            Messages.Message(text, LookTargets.Invalid,
                MessageTypeDefOf.RejectInput, false);
        }

        SoundDefOf.ClickReject.PlayOneShotOnCamera();
        return false;
    }

    private bool TryAssignReplace(Pawn pawn, IEnumerable<RoleType> roleGroup, Pawn replacing)
    {
        var lordJobRoles = roleGroup.ToList();
        if (!SendTryAssignMessages(pawn, lordJobRoles, true))
        {
            var num = assignments.PawnSpectating(pawn);
            var val = assignments.RoleForPawn(pawn);
            var insertBefore = lordJobRoles.SelectMany(r => assignments.AssignedPawns(r))
                .SkipWhile(p => p != replacing)
                .FirstOrDefault();
            assignments.RemoveParticipant(replacing);
            TryAssign(pawn, lordJobRoles, true,
                insertBefore, true, true);
            if (num)
            {
                assignments.TryAssignSpectate(replacing);
            }
            else if (val != null
                     && assignments.TryAssign(replacing, val, out _))
            {
                Notify_AssignmentsChanged();
            }
        }

        return lordJobRoles.Contains(assignments.RoleForPawn(pawn));
    }
}