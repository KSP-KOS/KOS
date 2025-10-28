using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;
using kOS.Safe.Exceptions;
using System.Collections.Generic;
using kOS.Safe.Utilities;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("Style")]
    public class WidgetStyle : Structure
    {
        private GUIStyle copyOnWriteStyle;
        private bool copied = false;

        public GUIStyle ReadOnly { get { return copyOnWriteStyle; } }
        public GUIStyle Writable { get {
                if (!copied) {
                    copyOnWriteStyle = new GUIStyle(copyOnWriteStyle);
                    copied = true;
                }
                return copyOnWriteStyle;
            }
        }

        public WidgetStyle(GUIStyle original)
        {
            copyOnWriteStyle = original;
            RegisterInitializer(InitializeSuffixes);
        }

        RgbaColor TextColor
        {
            get
            {
                var c = ReadOnly.normal.textColor;
                return new RgbaColor(c.r, c.g, c.b, c.a);
            }
            set
            {
                Writable.normal.textColor = value.Color;
            }
        }

        WidgetStyleRectOffset margin;
        WidgetStyleRectOffset padding;
        WidgetStyleRectOffset border;
        WidgetStyleRectOffset overflow;
        WidgetStyleState normal;
        WidgetStyleState focused;
        WidgetStyleState active;
        WidgetStyleState hover;
        WidgetStyleState onNormal;
        WidgetStyleState onFocused;
        WidgetStyleState onActive;
        WidgetStyleState onHover;

        WidgetStyleRectOffset CreateWidgetStyleRectOffsetSuffix(RectOffset rectOffset, ref WidgetStyleRectOffset suffix)
        {
            if (suffix == null)
            {
                suffix = new WidgetStyleRectOffset(rectOffset);
            }
            return suffix;
        }

        WidgetStyleState CreateWidgetStyleStateSuffix(GUIStyleState guiStyleState, ref WidgetStyleState suffix)
        {
            if (suffix == null)
            {
                suffix = new WidgetStyleState(guiStyleState);
            }
            return suffix;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("MARGIN", new Suffix<WidgetStyleRectOffset>(() => CreateWidgetStyleRectOffsetSuffix(Writable.margin, ref margin)));
            AddSuffix("PADDING", new Suffix<WidgetStyleRectOffset>(() => CreateWidgetStyleRectOffsetSuffix(Writable.padding, ref padding)));
            AddSuffix("BORDER", new Suffix<WidgetStyleRectOffset>(() => CreateWidgetStyleRectOffsetSuffix(Writable.border, ref border)));
            AddSuffix("OVERFLOW", new Suffix<WidgetStyleRectOffset>(() => CreateWidgetStyleRectOffsetSuffix(Writable.overflow, ref overflow)));

            AddSuffix("WIDTH", new SetSuffix<ScalarValue>(() => ReadOnly.fixedWidth, value => Writable.fixedWidth = value));
            AddSuffix("HEIGHT", new SetSuffix<ScalarValue>(() => ReadOnly.fixedHeight, value => Writable.fixedHeight = value));

            AddSuffix("HSTRETCH", new SetSuffix<BooleanValue>(() => ReadOnly.stretchWidth, value => Writable.stretchWidth = value));
            AddSuffix("VSTRETCH", new SetSuffix<BooleanValue>(() => ReadOnly.stretchHeight, value => Writable.stretchHeight = value));

            AddSuffix("BG", new SetSuffix<StringValue>(() => "", value => Writable.normal.background = Widget.GetTexture(value)));
            AddSuffix("TEXTCOLOR", new SetSuffix<RgbaColor>(() => TextColor, value => TextColor = value));

            AddSuffix("NORMAL", new Suffix<WidgetStyleState>(() => CreateWidgetStyleStateSuffix(Writable.normal, ref normal)));
            AddSuffix("FOCUSED", new Suffix<WidgetStyleState>(() => CreateWidgetStyleStateSuffix(Writable.focused, ref focused)));
            AddSuffix("ACTIVE", new Suffix<WidgetStyleState>(() => CreateWidgetStyleStateSuffix(Writable.active, ref active)));
            AddSuffix("HOVER", new Suffix<WidgetStyleState>(() => CreateWidgetStyleStateSuffix(Writable.hover, ref hover)));
            AddSuffix(new[] { "ON", "NORMAL_ON" }, new Suffix<WidgetStyleState>(() => CreateWidgetStyleStateSuffix(Writable.onNormal, ref onNormal)));
            AddSuffix("FOCUSED_ON", new Suffix<WidgetStyleState>(() => CreateWidgetStyleStateSuffix(Writable.onFocused, ref onFocused)));
            AddSuffix("ACTIVE_ON", new Suffix<WidgetStyleState>(() => CreateWidgetStyleStateSuffix(Writable.onActive, ref onActive)));
            AddSuffix("HOVER_ON", new Suffix<WidgetStyleState>(() => CreateWidgetStyleStateSuffix(Writable.onHover, ref onHover)));

            AddSuffix("FONT", new SetSuffix<StringValue>(GetFont, SetFont));
            AddSuffix("FONTSIZE", new SetSuffix<ScalarIntValue>(() => ReadOnly.fontSize, value => Writable.fontSize = value));
            AddSuffix("RICHTEXT", new SetSuffix<BooleanValue>(() => ReadOnly.richText, value => Writable.richText = value));
            AddSuffix("ALIGN", new SetSuffix<StringValue>(GetAlignment, SetAlignment));

            AddSuffix("WORDWRAP", new SetSuffix<BooleanValue>(() => ReadOnly.wordWrap, value => Writable.wordWrap = value));
        }

        StringValue GetFont()
        {
            if (ReadOnly.font == null) return "";
            return ReadOnly.font.name;
        }

        void SetFont(StringValue name)
        {
            Writable.font = FontNamed(name);
        }

        static Dictionary<string, Font> fontDict = null;

        public static Font FontNamed(string name)
        {
            if (fontDict == null) {
                var allfonts = Resources.FindObjectsOfTypeAll<Font>();
                fontDict = new Dictionary<string, Font>(System.StringComparer.InvariantCultureIgnoreCase);
                SafeHouse.Logger.Log(allfonts.Length + " fonts found");
                foreach (var f in allfonts) {
                    fontDict[f.name] = f;
                    SafeHouse.Logger.Log("  font name: \"" + f.name + "\"");
                }
            }
            if (name == "" || !fontDict.ContainsKey(name)) {
                return null;
            }
            return fontDict[name];
        }

        StringValue GetAlignment()
        {
            if (ReadOnly.alignment == TextAnchor.MiddleCenter) return "CENTER";
            if (ReadOnly.alignment == TextAnchor.MiddleRight) return "RIGHT";
            return "LEFT";
        }

        void SetAlignment(StringValue s)
        {
            s = s.ToLower();
            if (s == "center") Writable.alignment = TextAnchor.MiddleCenter;
            else if (s == "right") Writable.alignment = TextAnchor.MiddleRight;
            else if (s == "left") Writable.alignment = TextAnchor.MiddleLeft;
            else throw new KOSInvalidArgumentException("LABEL", "ALIGNMENT", "expected CENTER, LEFT, or RIGHT, found " + s);
        }

        public override string ToString()
        {
            return "STYLE("+copyOnWriteStyle.name+")";
        }
    }
}
