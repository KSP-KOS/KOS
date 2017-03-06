using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("Label")]
    public class Label : Widget
    {
        private GUIContent content { get; set; }
        private GUIContent content_visible { get; set; }

        public string Text { get { return content.text; } }

        public Label(Box parent, string text, WidgetStyle style) : base(parent, style)
        {
            RegisterInitializer(InitializeSuffixes);
            content = new GUIContent(text);
            content_visible = new GUIContent(text);
        }

        public Label(Box parent, string text) : this(parent, text, parent.FindStyle("label"))
        {
        }

        private void InitializeSuffixes()
        {
            AddSuffix("TEXT", new SetSuffix<StringValue>(() => content.text, value => { if (content.text != value) { content.text = value; Communicate(() => content_visible.text = value); } }));
            AddSuffix("IMAGE", new SetSuffix<StringValue>(() => "", value => SetContentImage(value)));
            AddSuffix("TOOLTIP", new SetSuffix<StringValue>(() => content.tooltip, value => { if (content.tooltip != value) { content.tooltip = value; Communicate(() => content_visible.tooltip = value); } }));
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

        public override void DoGUI()
        {
            GUILayout.Label(content_visible, ReadOnlyStyle);
        }

        public override string ToString()
        {
            return "LABEL(" + content.text.Ellipsis(10) + ")";
        }
    }
}
