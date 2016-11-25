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
using System;

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
    abstract public class Widget : Structure, IKOSScopeObserver
    {
        abstract protected GUIStyle BaseStyle();
        private Box parent;
        private GUIStyle _style;
        public GUIStyle style { get { return _style == null ? BaseStyle() : _style; } }
        public GUIStyle setstyle { get { if (_style == null) { _style = new GUIStyle(BaseStyle()); } return _style; } }

        public bool enabled { get; protected set; }
        public bool shown { get; protected set; }

        public int LinkCount { get; set; }

        public Widget(Box parent)
        {
            this.parent = parent;
            enabled = true;
            shown = true;
            RegisterInitializer(InitializeSuffixes);
        }

        protected RgbaColor GetStyleRgbaColor()
        {
            var c = style.normal.textColor;
            return new RgbaColor(c.r, c.g, c.b, c.a);
        }
        
        protected void SetStyleRgbaColor(RgbaColor rgba)
        {
            setstyle.normal.textColor = rgba.Color;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("HMARGIN", new SetSuffix<ScalarIntValue>(() => style.margin.left, value => { setstyle.margin.left = value; setstyle.margin.right = value; }));
            AddSuffix("HPADDING", new SetSuffix<ScalarIntValue>(() => style.padding.left, value => { setstyle.padding.left = value; setstyle.padding.right = value; }));
            AddSuffix("VMARGIN", new SetSuffix<ScalarIntValue>(() => style.margin.top, value => { setstyle.margin.top = value; setstyle.margin.bottom = value; }));
            AddSuffix("VPADDING", new SetSuffix<ScalarIntValue>(() => style.padding.top, value => { setstyle.padding.top = value; setstyle.padding.bottom = value; }));

            AddSuffix("WIDTH", new SetSuffix<ScalarValue>(() => style.fixedWidth, value => setstyle.fixedWidth = value));
            AddSuffix("HEIGHT", new SetSuffix<ScalarValue>(() => style.fixedHeight, value => setstyle.fixedHeight = value));
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => enabled, value => enabled = value));
            AddSuffix("SHOW", new NoArgsVoidSuffix(Show));
            AddSuffix("HIDE", new NoArgsVoidSuffix(Hide));

            AddSuffix("HSTRETCH", new SetSuffix<BooleanValue>(() => style.stretchWidth, value => setstyle.stretchWidth = value));
            AddSuffix("VSTRETCH", new SetSuffix<BooleanValue>(() => style.stretchHeight, value => setstyle.stretchHeight = value));

            AddSuffix("HBORDER", new SetSuffix<ScalarIntValue>(() => style.border.left, value => { setstyle.border.left = value; setstyle.border.right = value; }));
            AddSuffix("VBORDER", new SetSuffix<ScalarIntValue>(() => style.border.top, value => { setstyle.border.top = value; setstyle.border.bottom = value; }));

            AddSuffix("BG", new SetSuffix<StringValue>(() => "", value => setstyle.normal.background = GetTexture(value)));
            AddSuffix("BG_FOCUSED", new SetSuffix<StringValue>(() => "", value => setstyle.focused.background = GetTexture(value)));
            AddSuffix("BG_ACTIVE", new SetSuffix<StringValue>(() => "", value => setstyle.active.background = GetTexture(value)));
            AddSuffix("BG_HOVER", new SetSuffix<StringValue>(() => "", value => setstyle.hover.background = GetTexture(value)));
            AddSuffix("BG_ON", new SetSuffix<StringValue>(() => "", value => setstyle.onNormal.background = GetTexture(value)));
            AddSuffix("BG_FOCUSED_ON", new SetSuffix<StringValue>(() => "", value => setstyle.onFocused.background = GetTexture(value)));
            AddSuffix("BG_ACTIVE_ON", new SetSuffix<StringValue>(() => "", value => setstyle.onActive.background = GetTexture(value)));
            AddSuffix("BG_HOVER_ON", new SetSuffix<StringValue>(() => "", value => setstyle.onHover.background = GetTexture(value)));
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

        virtual public void ScopeLost()
        {
            UnityEngine.Debug.Log("Scope lost on " + this + (parent==null ? " (no parent)" : "(parent is "+parent+")"));
            if (parent != null) parent.Remove(this);
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

        public Slider(Box parent, bool h_not_v, float v, float from, float to) : base(parent)
        {
            RegisterInitializer(InitializeSuffixes);
            horizontal = h_not_v;
            value = v;
            min = from;
            max = to;
            if (horizontal) { setstyle.margin.top = 8; setstyle.margin.bottom = 8; } // align better with labels.
            thumbStyle = new GUIStyle(horizontal ? HighLogic.Skin.horizontalSliderThumb : HighLogic.Skin.verticalSliderThumb);
        }

        protected override GUIStyle BaseStyle() { return horizontal ? HighLogic.Skin.horizontalSlider : HighLogic.Skin.verticalSlider; }

        private void InitializeSuffixes()
        {
            AddSuffix("VALUE", new SetSuffix<ScalarValue>(() => value, v => value = v));
            AddSuffix("MIN", new SetSuffix<ScalarValue>(() => min, v => min = v));
            AddSuffix("MAX", new SetSuffix<ScalarValue>(() => max, v => max = v));
        }

        public override void DoGUI()
        {
            if (horizontal)
                value = GUILayout.HorizontalSlider(value, min, max, style, thumbStyle);
            else
                value = GUILayout.VerticalSlider(value, min, max, style, thumbStyle);
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

        public Spacing(Box parent, float v) : base(parent)
        {
            RegisterInitializer(InitializeSuffixes);
            amount = v;
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.label; // not that changing a spacing style makes any sense.
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

        public Label(Box parent, string text) : base(parent)
        {
            RegisterInitializer(InitializeSuffixes);
            content = new GUIContent(text);
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.label;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("TEXT", new SetSuffix<StringValue>(() => content.text, value => content.text = value));
            AddSuffix("IMAGE", new SetSuffix<StringValue>(() => "", value => content.image = GetTexture(value)));
            AddSuffix("TOOLTIP", new SetSuffix<StringValue>(() => content.tooltip, value => content.tooltip = value));
            AddSuffix("ALIGN", new SetSuffix<StringValue>(GetAlignment, SetAlignment));
            AddSuffix("FONTSIZE", new SetSuffix<ScalarIntValue>(() => style.fontSize, value => setstyle.fontSize = value));
            AddSuffix("RICHTEXT", new SetSuffix<BooleanValue>(() => style.richText, value => setstyle.richText = value));
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
            if (s == "center") setstyle.alignment = TextAnchor.MiddleCenter;
            else if (s == "right") setstyle.alignment = TextAnchor.MiddleRight;
            else if (s == "left") setstyle.alignment = TextAnchor.MiddleLeft;
            else throw new KOSInvalidArgumentException("LABEL", "ALIGNMENT", "expected CENTER, LEFT, or RIGHT, found " + s);
        }

        public override void DoGUI()
        {
            GUILayout.Label(content, style);
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

        public TextField(Box parent, string text) : base(parent,text)
        {
            if (toolTipStyle == null) {
                toolTipStyle = new GUIStyle(HighLogic.Skin.label);
                toolTipStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 0.2f);
            }
            RegisterInitializer(InitializeSuffixes);
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.textField;
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
        public bool isToggle { get; set; }

        public Button(Box parent, string text) : base(parent,text)
        {
            isToggle = false;
            RegisterInitializer(InitializeSuffixes);
        }

        protected override GUIStyle BaseStyle()
        {
            return isToggle ? HighLogic.Skin.toggle : HighLogic.Skin.button;
        }

        public static Button NewCheckbox(Box parent, string text)
        {
            var r = new Button(parent,text);
            r.SetToggleMode(true);
            return r;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("PRESSED", new SetSuffix<BooleanValue>(() => TakePress(), value => Pressed = value));
            AddSuffix("SETTOGGLE", new OneArgsSuffix<BooleanValue>(SetToggleMode));
        }

        public void SetToggleMode(BooleanValue on)
        {
            if (isToggle != on)
                isToggle = on;
        }

        public bool TakePress()
        {
            var r = Pressed;
            if (!isToggle) Pressed = false;
            return r;
        }

        public override void DoGUI()
        {
            if (isToggle) {
                Pressed = GUILayout.Toggle(Pressed, content, style);
            } else {
                if (GUILayout.Toggle(Pressed, content, style)) Pressed = true;
            }
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

        public Box(Box parent, LayoutMode mode) : base(parent)
        {
            RegisterInitializer(InitializeSuffixes);
            layout = mode;
            widgets = new List<Widget>();
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.box;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("ADDLABEL", new OneArgsSuffix<Label, StringValue>(AddLabel));
            AddSuffix("ADDTEXTFIELD", new OneArgsSuffix<TextField, StringValue>(AddTextField));
            AddSuffix("ADDBUTTON", new OneArgsSuffix<Button, StringValue>(AddButton));
            AddSuffix("ADDCHECKBOX", new TwoArgsSuffix<Button, StringValue, BooleanValue>(AddCheckbox));
            AddSuffix("ADDHSLIDER", new TwoArgsSuffix<Slider, ScalarValue, ScalarValue>(AddHSlider));
            AddSuffix("ADDVSLIDER", new TwoArgsSuffix<Slider, ScalarValue, ScalarValue>(AddVSlider));
            AddSuffix("ADDHBOX", new Suffix<Box>(AddHBox));
            AddSuffix("ADDVBOX", new Suffix<Box>(AddVBox));
            AddSuffix("ADDHLAYOUT", new Suffix<Box>(AddHLayout));
            AddSuffix("ADDVLAYOUT", new Suffix<Box>(AddVLayout));
            AddSuffix("ADDSTACK", new Suffix<Box>(AddStack));
            AddSuffix("ADDSPACING", new OneArgsSuffix<Spacing, ScalarValue>(AddSpace));
            AddSuffix("WIDGETS", new Suffix<ListValue>(() => ListValue.CreateList(widgets)));
            AddSuffix("SHOWONLY", new OneArgsSuffix<Widget>(value=>ShowOnly(value)));
        }

        public void ShowOnly(Widget toshow)
        {
            for (var i=0; i<widgets.Count; ++i) {
                var w = widgets[i];
                if (w == toshow) w.Show();
                else w.Hide();
            }
        }

        public Spacing AddSpace(ScalarValue amount)
        {
            var w = new Spacing(this, amount);
            widgets.Add(w);
            return w;
        }

        public Slider AddHSlider(ScalarValue min, ScalarValue max)
        {
            var w = new Slider(this, true, min, min, max);
            widgets.Add(w);
            return w;
        }

        public Slider AddVSlider(ScalarValue min, ScalarValue max)
        {
            var w = new Slider(this, false, min, min, max);
            widgets.Add(w);
            return w;
        }

        public Box AddStack()
        {
            var w = new Box(this, Box.LayoutMode.Stack);
            widgets.Add(w);
            return w;
        }

        public Box AddHBox()
        {
            var w = new Box(this, Box.LayoutMode.Horizontal);
            widgets.Add(w);
            return w;
        }

        public Box AddVBox()
        {
            var w = new Box(this,Box.LayoutMode.Vertical);
            widgets.Add(w);
            return w;
        }

        void MakeFlat()
        {
            setstyle.margin = new RectOffset(0, 0, 0, 0);
            setstyle.padding = new RectOffset(0, 0, 0, 0);
            setstyle.normal.background = null;
        }

        public void Remove(Widget child)
        {
            widgets.Remove(child);
        }

        public Box AddHLayout()
        {
            var w = new Box(this, Box.LayoutMode.Horizontal);
            w.MakeFlat();
            widgets.Add(w);
            return w;
        }

        public Box AddVLayout()
        {
            var w = new Box(this, Box.LayoutMode.Vertical);
            w.MakeFlat();
            widgets.Add(w);
            return w;
        }

        public Label AddLabel(StringValue text)
        {
            var w = new Label(this, text);
            widgets.Add(w);
            return w;
        }

        public TextField AddTextField(StringValue text)
        {
            var w = new TextField(this, text);
            widgets.Add(w);
            return w;
        }

        public Button AddButton(StringValue text)
        {
            var w = new Button(this, text);
            widgets.Add(w);
            return w;
        }

        public Button AddCheckbox(StringValue text, BooleanValue on)
        {
            var w = Button.NewCheckbox(this, text);
            w.Pressed = on;
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
    public class GUIWidgets : Box
    {
        private GUIWindow window;

        public GUIWidgets(int width, int height, SharedObjects shared) : base(null,Box.LayoutMode.Vertical)
        {
            setstyle.padding.top = style.padding.bottom; // no title area.
            var go = new GameObject("kOSGUIWindow");
            window = go.AddComponent<GUIWindow>();
            window.AttachTo(width,height,"",shared,this);
            InitializeSuffixes();
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.window;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("X", new SetSuffix<ScalarValue>(() => window.GetRect().x, value => window.SetX(value)));
            AddSuffix("Y", new SetSuffix<ScalarValue>(() => window.GetRect().y, value => window.SetY(value)));
        }

        override public void ScopeLost()
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
