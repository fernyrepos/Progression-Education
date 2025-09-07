using RimWorld;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Verse;

namespace ProgressionEducation
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }
    [HotSwappable]
    public class Window_ColorPicker : Window
    {
        private Color color;
        private Color oldColor;
        private readonly Action<Color> onColorSelected;

        private bool hsvColorWheelDragging;
        private bool colorTemperatureDragging;

        private string[] textfieldBuffers = new string[6];
        private Color textfieldColorBuffer;
        private string previousFocusedControlName;

        public static Widgets.ColorComponents visibleColorTextfields = Widgets.ColorComponents.Hue | Widgets.ColorComponents.Sat | Widgets.ColorComponents.Value;
        public static Widgets.ColorComponents editableColorTextfields = Widgets.ColorComponents.Hue | Widgets.ColorComponents.Sat | Widgets.ColorComponents.Value;

        private static readonly Vector2 ButSize = new(150f, 38f);
        public override Vector2 InitialSize => new(600f, 450f);

        private static readonly List<string> focusableControlNames = ["title", "colorTextfields_0", "colorTextfields_1", "colorTextfields_2", "colorTextfields_3", "colorTextfields_4"];


        public Window_ColorPicker(Color initialColor, Action<Color> onColorSelected)
        {
            doCloseX = true;
            color = initialColor;
            oldColor = initialColor;
            this.onColorSelected = onColorSelected;

            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            closeOnAccept = false;
        }

        private static void HeaderRow(ref RectDivider layout)
        {
            using (new TextBlock(GameFont.Medium))
            {
                var taggedString = "ChooseAColor".Translate().CapitalizeFirst();
                var rectDivider = layout.NewRow(Text.CalcHeight(taggedString, layout.Rect.width));
                GUI.SetNextControlName(focusableControlNames[0]);
                var rect = rectDivider.Rect;
                rect.y -= 5f;
                Widgets.Label(rect, taggedString);
            }
        }

        private void BottomButtons(ref RectDivider layout)
        {
            var rectDivider = layout.NewRow(ButSize.y, VerticalJustification.Bottom);
            if (Widgets.ButtonText(rectDivider.NewCol(ButSize.x), "Cancel".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(rectDivider.NewCol(ButSize.x, HorizontalJustification.Right), "Accept".Translate()))
            {
                onColorSelected?.Invoke(color);
                Close();
            }
        }

        private void ColorTextfields(ref RectDivider layout, out Vector2 size)
        {
            RectAggregator aggregator = new(new Rect(layout.Rect.position, new Vector2(125f, 0f)), 195906069);
            Widgets.ColorTextfields(ref aggregator, ref color, ref textfieldBuffers, ref textfieldColorBuffer, previousFocusedControlName, "colorTextfields", editableColorTextfields, visibleColorTextfields);
            size = aggregator.Rect.size;

            Rect hexRect = new(aggregator.Rect.x, aggregator.Rect.yMax + 4, 125, 32);
            if (Widgets.ButtonText(hexRect, "PE_PasteHex".Translate()))
            {
                if (TryGetColorFromHex(GUIUtility.systemCopyBuffer, out var tempColor))
                {
                    color = tempColor;
                }
            }
        }

        public static bool TryGetColorFromHex(string hex, out Color color)
        {
            color = Color.white;
            if (hex.StartsWith("#"))
            {
                hex = hex[1..];
            }

            if (hex.Length is not 6 and not 8)
            {
                return false;
            }

            try
            {
                int r = int.Parse(hex[..2], NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                int a = 255;
                if (hex.Length == 8)
                {
                    a = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                }
                color = GenColor.FromBytes(r, g, b, a);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ColorReadback(Rect rect, Color color, Color oldColor)
        {
            rect.SplitVertically((rect.width - 26f) / 2f, out var left, out var right);
            RectDivider rectDivider = new(left, 195906069);
            var label = "CurrentColor".Translate().CapitalizeFirst();
            var label2 = "OldColor".Translate().CapitalizeFirst();
            float width = Mathf.Max(100f, label.GetWidthCached(), label2.GetWidthCached());
            var rectDivider2 = rectDivider.NewRow(Text.LineHeight);
            Widgets.Label(rectDivider2.NewCol(width), label);
            Widgets.DrawBoxSolid(rectDivider2, color);
            var rectDivider3 = rectDivider.NewRow(Text.LineHeight);
            Widgets.Label(rectDivider3.NewCol(width), label2);
            Widgets.DrawBoxSolid(rectDivider3, oldColor);
            RectDivider rectDivider4 = new(right, 195906069);
            rectDivider4.NewCol(26f);
            if (DarklightUtility.IsDarklight(color))
            {
                Widgets.Label(rectDivider4, "Darklight".Translate().CapitalizeFirst());
            }
            else
            {
                Widgets.Label(rectDivider4, "NotDarklight".Translate().CapitalizeFirst());
            }
        }

        private static void TabControl()
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab)
            {
                bool forward = !Event.current.shift;
                Event.current.Use();
                string currentFocus = GUI.GetNameOfFocusedControl();
                if (currentFocus.NullOrEmpty())
                {
                    currentFocus = focusableControlNames[0];
                }
                int currentIndex = focusableControlNames.IndexOf(currentFocus);
                if (currentIndex < 0)
                {
                    currentIndex = focusableControlNames.Count;
                }
                currentIndex = forward ? (currentIndex + 1) : (currentIndex - 1);
                if (currentIndex >= focusableControlNames.Count)
                {
                    currentIndex = 0;
                }
                else if (currentIndex < 0)
                {
                    currentIndex = focusableControlNames.Count - 1;
                }
                GUI.FocusControl(focusableControlNames[currentIndex]);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            using (TextBlock.Default())
            {
                RectDivider layout = new(inRect, 195906069);
                HeaderRow(ref layout);
                layout.NewRow(0f);
                BottomButtons(ref layout);
                layout.NewRow(0f, VerticalJustification.Bottom);

                var defaultColor = color;
                defaultColor.a = 1f;

                ColorPalette(ref layout, ref color, defaultColor, true, out float paletteHeight);
                ColorTextfields(ref layout, out var size);

                float height = Mathf.Max(paletteHeight, 128f, size.y);
                var rectDivider = layout.NewRow(height);
                rectDivider.NewCol(size.x);
                rectDivider.NewCol(250f, HorizontalJustification.Right);
                Widgets.HSVColorWheel(rectDivider.Rect.ContractedBy((rectDivider.Rect.width - 128f) / 2f, (rectDivider.Rect.height - 128f) / 2f), ref color, ref hsvColorWheelDragging, 1f);

                layout.NewRow(10f);
                Rect rect = layout.NewRow(34f);
                Widgets.ColorTemperatureBar(rect, ref color, ref colorTemperatureDragging, 1f);

                layout.NewRow(26f);
                ColorReadback(layout, color, oldColor);

                TabControl();
                if (Event.current.type == EventType.Layout)
                {
                    previousFocusedControlName = GUI.GetNameOfFocusedControl();
                }
            }
        }

        private void ColorPalette(ref RectDivider layout, ref Color color, Color defaultColor, bool showDarklight, out float paletteHeight)
        {
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                var rectDivider = layout;
                var rectDivider2 = rectDivider.NewCol(250f, HorizontalJustification.Right);
                int boxSize = 26;
                var rectDivider3 = rectDivider2.NewRow(boxSize);
                int num2 = 4;
                rectDivider3.Rect.SplitVertically(num2 * (boxSize + 2), out var left, out var right);
                RectDivider rectDivider4 = new(left, 195906069, new Vector2(10f, 2f));
                Widgets.ColorBox(rectDivider4.NewCol(boxSize), ref color, defaultColor);
                Widgets.Label(rectDivider4, "Default".Translate().CapitalizeFirst());
                RectDivider rectDivider5 = new(right, 195906069, new Vector2(10f, 2f));
                var defaultDarklight = DarklightUtility.DefaultDarklight;
                Rect rect = rectDivider5.NewCol(boxSize);
                if (showDarklight)
                {
                    Widgets.ColorBox(rect, ref color, defaultDarklight);
                    Widgets.Label(rectDivider5, "Darklight".Translate().CapitalizeFirst());
                }
                Widgets.ColorSelector(rectDivider2, ref color, Dialog_GlowerColorPicker.colors, out paletteHeight);
                paletteHeight += boxSize + 2;
            }
        }
    }
}
