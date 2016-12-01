using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using UnityEngine;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Label")]
    public class Label : Widget
    {
        private GUIContent content { get; set; }
        private GUIContent content_visible { get; set; }

        public Label(Box parent, string text) : base(parent)
        {
            RegisterInitializer(InitializeSuffixes);
            content = new GUIContent(text);
            content_visible = new GUIContent(text);
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.label;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("TEXT", new SetSuffix<StringValue>(() => content.text, value => { if (content.text != value) { content.text = value; Communicate(() => content_visible.text = value); } }));
            AddSuffix("IMAGE", new SetSuffix<StringValue>(() => "", value => SetContentImage(value)));
            AddSuffix("TOOLTIP", new SetSuffix<StringValue>(() => content.tooltip, value => { if (content.tooltip != value) { content.tooltip = value; Communicate(() => content_visible.tooltip = value); } }));
            AddSuffix("ALIGN", new SetSuffix<StringValue>(GetAlignment, SetAlignment));
            AddSuffix("FONTSIZE", new SetSuffix<ScalarIntValue>(() => Style.fontSize, value => SetStyle.fontSize = value));
            AddSuffix("RICHTEXT", new SetSuffix<BooleanValue>(() => Style.richText, value => SetStyle.richText = value));
            AddSuffix("TEXTCOLOR", new SetSuffix<RgbaColor>(() => GetStyleRgbaColor(), value => SetStyleRgbaColor(value)));
        }

        protected void SetInitialContentImage(Texture2D img)
        {
            content.image = img;
            content_visible.image = img;
        }

        protected void SetContentImage(string img)
        {
            Texture2D tex = GetTexture(img);
            content.image = tex;
            Communicate(() => content_visible.image = tex);
        }

        protected string StoredText()
        {
            return content.text;
        }

        protected string VisibleTooltip()
        {
            return content_visible.tooltip;
        }
        protected string VisibleText()
        {
            return content_visible.text;
        }

        protected void SetVisibleText(string t)
        {
            if (content_visible.text != t) {
                content_visible.text = t;
                Communicate(() => content.text = t);
            }
        }

        protected GUIContent VisibleContent()
        {
            return content_visible;
        }

        StringValue GetAlignment()
        {
            if (Style.alignment == TextAnchor.MiddleCenter) return "CENTER";
            if (Style.alignment == TextAnchor.MiddleRight) return "RIGHT";
            return "LEFT";
        }

        void SetAlignment(StringValue s)
        {
            s = s.ToLower();
            if (s == "center") SetStyle.alignment = TextAnchor.MiddleCenter;
            else if (s == "right") SetStyle.alignment = TextAnchor.MiddleRight;
            else if (s == "left") SetStyle.alignment = TextAnchor.MiddleLeft;
            else throw new KOSInvalidArgumentException("LABEL", "ALIGNMENT", "expected CENTER, LEFT, or RIGHT, found " + s);
        }

        public override void DoGUI()
        {
            GUILayout.Label(content_visible, Style);
        }

        public override string ToString()
        {
            return "LABEL(" + content.text.Ellipsis(10) + ")";
        }
    }
}
