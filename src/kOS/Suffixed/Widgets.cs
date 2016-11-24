using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using UnityEngine;
using System.Collections.Generic;
using kOS.Safe.Utilities;
using kOS.Safe.Execution;
using kOS.Utilities;
using kOS.Screen;
using System.IO;

/* Usage:
 * 
 *   SET ui TO GUI(200,200).
 *   ui:ADDLABEL("Hello world").
 *   SET button TO ui:ADDBUTTON("OK").
 *   WHEN button:PRESSED THEN ui:HIDE().
 *   
 *   See docs for more.
 */

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Widget")]
    abstract public class Widget : Structure
    {
        protected GUIStyle style;
        public bool enabled { get; protected set; }
        public bool shown { get; protected set; }

        public Widget()
        {
            enabled = true;
            shown = true;
            RegisterInitializer(InitializeSuffixes);
            GUISkin theSkin = Utils.GetSkinCopy(HighLogic.Skin);
            style = new GUIStyle(theSkin.box);
        }

        protected RgbaColor GetStyleRgbaColor()
        {
            var c = style.normal.textColor;
            return new RgbaColor(c.r, c.g, c.b, c.a);
        }
        
        protected void SetStyleRgbaColor(RgbaColor rgba)
        {
            style.normal.textColor = rgba.Color;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("MARGIN", new SetSuffix<ScalarIntValue>(() => style.margin.left, value => style.margin = new RectOffset(value, value, value, value)));
            AddSuffix("PADDING", new SetSuffix<ScalarIntValue>(() => style.padding.left, value => style.padding = new RectOffset(value, value, value, value)));
            AddSuffix("WIDTH", new SetSuffix<ScalarValue>(() => style.fixedWidth, value => style.fixedWidth = value));
            AddSuffix("HEIGHT", new SetSuffix<ScalarValue>(() => style.fixedHeight, value => style.fixedHeight = value));
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => enabled, value => enabled = value));
            AddSuffix("SHOW", new NoArgsVoidSuffix(Show));
            AddSuffix("HIDE", new NoArgsVoidSuffix(Hide));

            AddSuffix("HSTRETCH", new SetSuffix<BooleanValue>(() => style.stretchWidth, value => style.stretchWidth = value));
            AddSuffix("VSTRETCH", new SetSuffix<BooleanValue>(() => style.stretchHeight, value => style.stretchHeight = value));
            AddSuffix("BG", new SetSuffix<StringValue>(() => "", value => style.normal.background = GetTexture(value)));
            AddSuffix("BG_FOCUSED", new SetSuffix<StringValue>(() => "", value => style.focused.background = GetTexture(value)));
            AddSuffix("BG_ACTIVE", new SetSuffix<StringValue>(() => "", value => style.active.background = GetTexture(value)));
            AddSuffix("BG_HOVER", new SetSuffix<StringValue>(() => "", value => style.hover.background = GetTexture(value)));
            AddSuffix("BG_ON", new SetSuffix<StringValue>(() => "", value => style.onNormal.background = GetTexture(value)));
            AddSuffix("BG_FOCUSED_ON", new SetSuffix<StringValue>(() => "", value => style.onFocused.background = GetTexture(value)));
            AddSuffix("BG_ACTIVE_ON", new SetSuffix<StringValue>(() => "", value => style.onActive.background = GetTexture(value)));
            AddSuffix("BG_HOVER_ON", new SetSuffix<StringValue>(() => "", value => style.onHover.background = GetTexture(value)));
        }

        virtual public void Show()
        {
            shown = true;
        }

        virtual public void Hide()
        {
            shown = false;
        }

        abstract public void DoGUI();

        class TexFileInfo
        {
            public Texture2D texture;
            public FileInfo info;
        }
        static Dictionary<string, TexFileInfo> textureCache;

        public static Texture2D GetTexture(string relativePath)
        {
            if (relativePath == "") return null;
            if (textureCache == null)
                textureCache = new Dictionary<string, TexFileInfo>();
            TexFileInfo t;
            if (textureCache.TryGetValue(relativePath, out t)) {
                if (t.texture != null) {
                    var curInfo = new FileInfo(t.info.FullName);
                    if (curInfo.LastWriteTime == t.info.LastWriteTime)
                        return t.texture;
                }
            } else {
                t = new TexFileInfo();
                textureCache.Add(relativePath, t);
            }
            var path = Path.Combine(SafeHouse.ArchiveFolder, relativePath);
            var r = new Texture2D(0, 0, TextureFormat.ARGB32, false);
            string[] exts = { ".png", "" };
            foreach (var ext in exts) {
                var filename = path + ext;
                if (File.Exists(filename)) {
                    r.LoadImage(File.ReadAllBytes(filename));
                    t.texture = r;
                    t.info = new FileInfo(filename);
                    break;
                }
            }
            return r;
        }

        public override string ToString()
        {
            return "WIDGET";
        }

        public override bool Equals(object obj)
        {
            return false;
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Slider")]
    public class Slider : Widget
    {
        public bool horizontal { get; set; }
        public float value { get; set; }
        public float min { get; set; }
        public float max { get; set; }
        protected GUIStyle thumbStyle;

        public Slider(bool h_not_v, float v, float from, float to)
        {
            RegisterInitializer(InitializeSuffixes);
            horizontal = h_not_v;
            value = v;
            min = from;
            max = to;
            GUISkin theSkin = Utils.GetSkinCopy(HighLogic.Skin);
            style = new GUIStyle(horizontal ? theSkin.horizontalSlider : theSkin.verticalSlider);
            thumbStyle = new GUIStyle(horizontal ? theSkin.horizontalSliderThumb : theSkin.verticalSliderThumb);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("VALUE", new SetSuffix<ScalarValue>(() => value, v => value = v));
            AddSuffix("MIN", new SetSuffix<ScalarValue>(() => min, v => min = v));
            AddSuffix("MAX", new SetSuffix<ScalarValue>(() => max, v => max = v));
        }

        public override void DoGUI()
        {
            if (horizontal)
                value = GUILayout.HorizontalSlider(value, min, max, style, thumbStyle, GUILayout.ExpandWidth(true));
            else
                value = GUILayout.VerticalSlider(value, min, max, style, thumbStyle, GUILayout.ExpandWidth(true));
        }

        public override string ToString()
        {
            return "SLIDER(" + value + ")";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Spacing")]
    public class Spacing : Widget
    {
        public float amount { get; set; }

        public Spacing(float v)
        {
            RegisterInitializer(InitializeSuffixes);
            amount = v;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("AMOUNT", new SetSuffix<ScalarValue>(() => amount, v => amount = v));
        }

        public override void DoGUI()
        {
            if (amount < 0)
                GUILayout.FlexibleSpace();
            else
                GUILayout.Space(amount);
        }

        public override string ToString()
        {
            return "SPACING(" + amount + ")";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Label")]
    public class Label : Widget
    {
        public GUIContent content { get; set; }

        public Label(string text)
        {
            RegisterInitializer(InitializeSuffixes);
            content = new GUIContent(text);
            GUISkin theSkin = Utils.GetSkinCopy(HighLogic.Skin);
            style = new GUIStyle(theSkin.label);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("TEXT", new SetSuffix<StringValue>(() => content.text, value => content.text = value));
            AddSuffix("IMAGE", new SetSuffix<StringValue>(() => "", value => content.image = GetTexture(value)));
            AddSuffix("TOOLTIP", new SetSuffix<StringValue>(() => content.tooltip, value => content.tooltip = value));
            AddSuffix("ALIGN", new SetSuffix<StringValue>(GetAlignment, SetAlignment));
            AddSuffix("FONTSIZE", new SetSuffix<ScalarIntValue>(() => style.fontSize, value => style.fontSize = value));
            AddSuffix("RICHTEXT", new SetSuffix<BooleanValue>(() => style.richText, value => style.richText = value));
            AddSuffix("TEXTCOLOR", new SetSuffix<RgbaColor>(() => GetStyleRgbaColor(), value => SetStyleRgbaColor(value)));
        }

        StringValue GetAlignment()
        {
            if (style.alignment == TextAnchor.MiddleCenter) return "CENTER";
            if (style.alignment == TextAnchor.MiddleRight) return "RIGHT";
            return "LEFT";
        }

        void SetAlignment(StringValue s)
        {
            s = s.ToLower();
            if (s == "center") style.alignment = TextAnchor.MiddleCenter;
            else if (s == "right") style.alignment = TextAnchor.MiddleRight;
            else if (s == "left") style.alignment = TextAnchor.MiddleLeft;
            else throw new KOSInvalidArgumentException("LABEL", "ALIGNMENT", "expected CENTER, LEFT, or RIGHT, found " + s);
        }

        public override void DoGUI()
        {
            GUILayout.Label(content, style, GUILayout.ExpandWidth(true));
        }

        public override string ToString()
        {
            return "LABEL(" + content.text + ")";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("TextField")]
    public class TextField : Label
    {
        public bool Changed { get; set; }
        public bool Confirmed { get; set; }

        static GUIStyle toolTipStyle = null;

        public TextField(string text) : base(text)
        {
            GUISkin theSkin = Utils.GetSkinCopy(HighLogic.Skin);
            style = new GUIStyle(theSkin.textField);
            if (toolTipStyle == null) {
                toolTipStyle = new GUIStyle(theSkin.label);
                toolTipStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 0.2f);
            }
            RegisterInitializer(InitializeSuffixes);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("CHANGED", new SetSuffix<BooleanValue>(() => TakeChange(), value => Changed = value));
            AddSuffix("CONFIRMED", new SetSuffix<BooleanValue>(() => TakeConfirm(), value => Confirmed = value));
        }

        public bool TakeChange()
        {
            var r = Changed;
            Changed = false;
            return r;
        }

        public bool TakeConfirm()
        {
            var r = Confirmed;
            Confirmed = false;
            return r;
        }

        int uiID = -1;

        public override void DoGUI()
        {
            if (Event.current.keyCode == KeyCode.Return && GUIUtility.keyboardControl == uiID) {
                Confirmed = true;
                GUIUtility.keyboardControl = -1;
            }
            uiID = GUIUtility.GetControlID(FocusType.Passive) + 1; // Dirty kludge.
            var newtext = GUILayout.TextField(content.text, style);
            if (newtext != content.text) {
                content.text = newtext;
                Changed = true;
            }
            if (newtext == "") {
                GUI.Label(GUILayoutUtility.GetLastRect(), content.tooltip, toolTipStyle);
            }
        }

        public override string ToString()
        {
            return "TEXTFIELD(" + content.text + ")";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Button")]
    public class Button : Label
    {
        public bool Pressed { get; set; }

        public Button(string text) : base(text)
        {
            GUISkin theSkin = Utils.GetSkinCopy(HighLogic.Skin);
            style = new GUIStyle(theSkin.button);
            RegisterInitializer(InitializeSuffixes);
            UnityEngine.Debug.Log("BUTTON BORDERS:"+style.border);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("PRESSED", new SetSuffix<BooleanValue>(() => TakePress(), value => Pressed = value));
        }

        public bool TakePress()
        {
            var r = Pressed;
            Pressed = false;
            return r;
        }

        public override void DoGUI()
        {
            if (GUILayout.Toggle(Pressed, content, style)) Pressed = true;
        }

        public override string ToString()
        {
            return "BUTTON(" + content.text + ")" + (Pressed ? " is pressed" : "");
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Box")]
    public class Box : Widget
    {
        public enum LayoutMode { Stack, Horizontal, Vertical }
        LayoutMode layout;
        List<Widget> widgets;

        public int Count { get { return widgets.Count; } }

        public Box(LayoutMode mode)
        {
            RegisterInitializer(InitializeSuffixes);
            layout = mode;
            widgets = new List<Widget>();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("ADDLABEL", new OneArgsSuffix<Label, StringValue>(AddLabel));
            AddSuffix("ADDTEXTFIELD", new OneArgsSuffix<TextField, StringValue>(AddTextField));
            AddSuffix("ADDBUTTON", new OneArgsSuffix<Button, StringValue>(AddButton));
            AddSuffix("ADDHSLIDER", new TwoArgsSuffix<Slider, ScalarValue, ScalarValue>(AddHSlider));
            AddSuffix("ADDVSLIDER", new TwoArgsSuffix<Slider, ScalarValue, ScalarValue>(AddVSlider));
            AddSuffix("ADDHBOX", new Suffix<Box>(AddHBox));
            AddSuffix("ADDVBOX", new Suffix<Box>(AddVBox));
            AddSuffix("ADDSTACK", new Suffix<Box>(AddStack));
            AddSuffix("ADDSPACING", new OneArgsSuffix<Spacing, ScalarValue>(AddSpace));
            AddSuffix("WIDGETS", new Suffix<ListValue>(() => ListValue.CreateList(widgets)));
            AddSuffix("SHOWONLY", new OneArgsSuffix<Widget>(value=>ShowOnly(value)));
        }

        public void ShowOnly(Widget toshow)
        {
            foreach (var w in widgets) {
                if (w == toshow) w.Show();
                else w.Hide();
            }
        }

        public Spacing AddSpace(ScalarValue amount)
        {
            var w = new Spacing(amount);
            widgets.Add(w);
            return w;
        }

        public Slider AddHSlider(ScalarValue min, ScalarValue max)
        {
            var w = new Slider(true, min, min, max);
            widgets.Add(w);
            return w;
        }

        public Slider AddVSlider(ScalarValue min, ScalarValue max)
        {
            var w = new Slider(false, min, min, max);
            widgets.Add(w);
            return w;
        }

        public Box AddStack()
        {
            var w = new Box(Box.LayoutMode.Stack);
            widgets.Add(w);
            return w;
        }

        public Box AddHBox()
        {
            var w = new Box(Box.LayoutMode.Horizontal);
            widgets.Add(w);
            return w;
        }

        public Box AddVBox()
        {
            var w = new Box(Box.LayoutMode.Vertical);
            widgets.Add(w);
            return w;
        }

        public Label AddLabel(StringValue text)
        {
            var w = new Label(text);
            widgets.Add(w);
            return w;
        }

        public TextField AddTextField(StringValue text)
        {
            var w = new TextField(text);
            widgets.Add(w);
            return w;
        }

        public Button AddButton(StringValue text)
        {
            var w = new Button(text);
            widgets.Add(w);
            return w;
        }

        public override void DoGUI()
        {
            if (!shown) return;
            if (!enabled) GUI.enabled = false;
            if (layout == LayoutMode.Horizontal) GUILayout.BeginHorizontal(style);
            else if (layout == LayoutMode.Vertical) GUILayout.BeginVertical(style);
            for (var i = 0; i < widgets.Count; ++i) {
                if (widgets[i].shown) {
                    var ge = GUI.enabled;
                    if (ge && !widgets[i].enabled) GUI.enabled = false;
                    widgets[i].DoGUI();
                    if (ge) GUI.enabled = true;
                    if (layout == LayoutMode.Stack)
                        break;
                }
            }
            if (layout == LayoutMode.Horizontal) GUILayout.EndHorizontal();
            else if (layout == LayoutMode.Vertical) GUILayout.EndVertical();
        }

        public override string ToString()
        {
            return layout+"BOX";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("GUI")]
    public class GUIWidgets : Box, IKOSScopeObserver
    {
        private GUIWindow window;

        public GUIWidgets(int width, int height, SharedObjects shared) : base(Box.LayoutMode.Vertical)
        {
            GUISkin theSkin = Utils.GetSkinCopy(HighLogic.Skin);
            style = new GUIStyle(theSkin.window);
            style.padding.top = style.padding.bottom; // no title area.
            var go = new GameObject();
            window = go.AddComponent<GUIWindow>();
            window.AttachTo(width,height,"",shared,this);
            InitializeSuffixes();
        }


        private void InitializeSuffixes()
        {
            AddSuffix("X", new SetSuffix<ScalarValue>(() => window.GetRect().x, value => window.SetX(value)));
            AddSuffix("Y", new SetSuffix<ScalarValue>(() => window.GetRect().y, value => window.SetY(value)));
        }

        // Implementation of KOSSCopeObserver interface:
        // ---------------------------------------------
        public int LinkCount { get; set; }

        public void ScopeLost()
        {
            // Hardly ever seems to be called :-(
            SetShow(false);
            window.Close();
        }

        override public void Show()
        {
            window.Open();
        }
        override public void Hide()
        {
            window.Close();
        }

        public bool GetShow()
        {
            return window.enabled;
        }

        public void SetShow(BooleanValue newShowVal)
        {
            window.gameObject.SetActive(newShowVal);
        }

        public override string ToString()
        {
            return "GUI";
        }
    }
}
