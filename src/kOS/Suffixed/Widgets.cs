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
    static class StringExt
    {
        public static string Ellipsis(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Widget")]
    abstract public class Widget : Structure
    {
        abstract protected GUIStyle BaseStyle();
        protected Box parent;
        private GUIStyle _style;
        public GUIStyle style { get { return _style == null ? BaseStyle() : _style; } }
        public GUIStyle setstyle { get { if (_style == null) { _style = new GUIStyle(BaseStyle()); } return _style; } }

        public bool enabled { get; protected set; }
        public bool shown { get; protected set; }

        public Widget(Box parent)
        {
            this.parent = parent;
            enabled = true;
            shown = true;
            RegisterInitializer(InitializeSuffixes);
        }

        protected GUIWidgets FindGUI()
        {
            var c = this;
            while (c.parent != null) {
                c = c.parent;
            }
            return c as GUIWidgets;
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
            AddSuffix("DISPOSE", new NoArgsVoidSuffix(Dispose));

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

        virtual public void Dispose()
        {
            shown = false;
            if (parent != null) {
                parent.Remove(this);
                var gui = FindGUI();
                if (gui != null)
                    gui.ClearCommunication(this);
            }
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

        virtual protected void Communicate(Action a)
        {
            var gui = FindGUI();
            if (gui != null)
                gui.Communicate(this,ToString(),a);
            else
                a();
        }

        public override string ToString()
        {
            return "WIDGET";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Slider")]
    public class Slider : Widget
    {
        public bool horizontal { get; set; }
        private float value { get; set; }
        private float value_visible { get; set; }
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
            AddSuffix("VALUE", new SetSuffix<ScalarValue>(() => value, v => { if (value != v) { value = v; Communicate(() => value_visible = v); } }));
            AddSuffix("MIN", new SetSuffix<ScalarValue>(() => min, v => min = v));
            AddSuffix("MAX", new SetSuffix<ScalarValue>(() => max, v => max = v));
        }

        public override void DoGUI()
        {
            float newvalue;
            if (horizontal)
                newvalue = GUILayout.HorizontalSlider(value_visible, min, max, style, thumbStyle);
            else
                newvalue = GUILayout.VerticalSlider(value_visible, min, max, style, thumbStyle);
            if (newvalue != value_visible) {
                value_visible = newvalue;
                Communicate(() => value = newvalue);
            }
        }

        public override string ToString()
        {
            return string.Format("SLIDER({0:0.00})",value);
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
            AddSuffix("FONTSIZE", new SetSuffix<ScalarIntValue>(() => style.fontSize, value => setstyle.fontSize = value));
            AddSuffix("RICHTEXT", new SetSuffix<BooleanValue>(() => style.richText, value => setstyle.richText = value));
            AddSuffix("TEXTCOLOR", new SetSuffix<RgbaColor>(() => GetStyleRgbaColor(), value => SetStyleRgbaColor(value)));
        }

        protected void SetInitialContentImage(Texture2D img)
        {
            content.image = img;
            content_visible.image = img;
        }

        protected void SetContentImage(string img)
        {
            var tex = GetTexture(img);
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
            GUILayout.Label(content_visible, style);
        }

        public override string ToString()
        {
            return "LABEL(" + content.text.Ellipsis(10) + ")";
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
                Communicate(() => Confirmed = true);
                GUIUtility.keyboardControl = -1;
            }
            uiID = GUIUtility.GetControlID(FocusType.Passive) + 1; // Dirty kludge.
            var newtext = GUILayout.TextField(VisibleText(), style);
            if (newtext != VisibleText()) {
                SetVisibleText(newtext);
                Changed = true;
            }
            if (newtext == "") {
                GUI.Label(GUILayoutUtility.GetLastRect(), VisibleTooltip(), toolTipStyle);
            }
        }

        public override string ToString()
        {
            return "TEXTFIELD(" + StoredText().Ellipsis(10) + ")";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Button")]
    public class Button : Label
    {
        public bool Pressed { get; private set; }
        public bool PressedVisible { get; private set; }
        public bool isToggle { get; set; }
        public bool isExclusive { get; set; }

        public Button(Box parent, string text) : base(parent, text)
        {
            isToggle = false;
            RegisterInitializer(InitializeSuffixes);
        }

        protected override GUIStyle BaseStyle()
        {
            return isToggle ? HighLogic.Skin.toggle : HighLogic.Skin.button;
        }

        public static Button NewCheckbox(Box parent, string text, bool on)
        {
            var r = new Button(parent, text);
            r.Pressed = on;
            r.PressedVisible = on;
            r.SetToggleMode(true);
            return r;
        }

        public static Button NewRadioButton(Box parent, string text, bool on)
        {
            var r = new Button(parent, text);
            r.Pressed = on;
            r.PressedVisible = on;
            r.SetToggleMode(true);
            r.isExclusive = true;
            return r;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("PRESSED", new SetSuffix<BooleanValue>(() => TakePress(), value => { Pressed = value; Communicate(() => PressedVisible = value); }));
            AddSuffix("SETTOGGLE", new OneArgsSuffix<BooleanValue>(SetToggleMode));
            AddSuffix("EXCLUSIVE", new SetSuffix<BooleanValue>(() => isExclusive, value => isExclusive = value));
        }

        public void SetToggleMode(BooleanValue on)
        {
            if (isToggle != on)
                isToggle = on;
        }

        public bool TakePress()
        {
            var r = Pressed;
            if (!isToggle && Pressed) {
                Pressed = false;
                Communicate(() => PressedVisible = false);
            }
            return r;
        }

        public void SetPressedVisible(bool on)
        {
            PressedVisible = on;
            if (PressedVisible != on)
                Communicate(() => Pressed = on);
        }

        public override void DoGUI()
        {
            if (isToggle) {
                var newpressed = GUILayout.Toggle(PressedVisible, VisibleContent(), style);
                PressedVisible = newpressed;
                if (isExclusive && newpressed && parent != null) {
                    parent.UnpressVisibleAllBut(this);
                }
                if (Pressed != newpressed)
                    Communicate(() => Pressed = newpressed);
            } else {
                if (GUILayout.Toggle(PressedVisible, VisibleContent(), style)) {
                    if (!PressedVisible) {
                        PressedVisible = true;
                        Communicate(() => Pressed = true);
                    }
                }
            }
        }

        public override string ToString()
        {
            return "BUTTON(" + VisibleText().Ellipsis(10) + ")";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("PopupMenu")]
    public class PopupMenu : Button
    {
        private bool changed = false;
        private ListValue list;
        private int index = 0;
        GUIStyle popupStyle;
        GUIStyle itemStyle;
        string optSuffix = "ToString";

        public PopupMenu(Box parent) : base(parent,"")
        {
            setstyle.alignment = TextAnchor.MiddleLeft;
            isToggle = true;

            itemStyle = new GUIStyle(style);
            itemStyle.margin.top = 0;
            itemStyle.margin.bottom = 0;
            itemStyle.normal.background = null;
            itemStyle.hover.background = GameDatabase.Instance.GetTexture("kOS/GFX/popupmenu_bg_hover", false);
            itemStyle.hover.textColor = Color.black;
            itemStyle.active.background = itemStyle.hover.background;
            itemStyle.stretchWidth = true;

            popupStyle = new GUIStyle(HighLogic.Skin.window);
            popupStyle.padding.top = popupStyle.padding.bottom; // no title area
            popupStyle.padding.left = 0;
            popupStyle.padding.right = 0;
            popupStyle.margin = new RectOffset(0, 0, 0, 0);

            list = new ListValue();
            SetInitialContentImage(GameDatabase.Instance.GetTexture("kOS/GFX/popupmenu", false));
            RegisterInitializer(InitializeSuffixes);
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.button;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("OPTIONS", new SetSuffix<ListValue>(() => list, value => list = value));
            AddSuffix("ADDOPTION", new OneArgsSuffix<Structure>(AddOption));
            AddSuffix("VALUE", new SetSuffix<Structure>(() => (index >= 0 && index < list.Count()) ? list[index] : new StringValue(""), value => Choose(value)));
            AddSuffix("INDEX", new SetSuffix<ScalarIntValue>(() => index, value => { index = value; if (index >= 0 && index < list.Count()) SetVisibleText(GetItemString(list[index])); }));
            AddSuffix("CLEAR", new NoArgsVoidSuffix(Clear));
            AddSuffix("CHANGED", new SetSuffix<BooleanValue>(() => TakeChange(), value => changed = value));
            AddSuffix("OPTIONSUFFIX", new SetSuffix<StringValue>(() => optSuffix, value => optSuffix = value));
        }

        public void Clear()
        {
            list.Clear();
            SetVisibleText("");
            Communicate(() => changed = true);
            // Note: we leave the index alone so things can be set in any order.
        }

        public bool TakeChange()
        {
            var r = changed;
            changed = false;
            return r;
        }

        string GetItemString(Structure item)
        {
            if (item.HasSuffix(optSuffix)) {
                var v = item.GetSuffix(optSuffix);
                if (v.HasValue) return v.Value.ToString();
            }
            return item.ToString();
        }

        public void AddOption(Structure opt)
        {
            if (list.Count() == index)
                SetVisibleText(GetItemString(opt));
            list.Add(opt);
            Communicate(() => changed = true);
        }

        public void Choose(Structure v)
        {
            for (index = 0; index < list.Count(); ++index) {
                if (list[index] == v) {
                    return;
                }
            }
            var vs = GetItemString(v);
            for (index = 0; index < list.Count(); ++index) {
                if (GetItemString(list[index]) == vs) {
                    return;
                }
            }
            index = -1;
        }

        public override void DoGUI()
        {
            var was = PressedVisible;
            base.DoGUI();
            if (Event.current.type == EventType.Repaint) {
                var r = GUILayoutUtility.GetLastRect();
                popupRect.position = GUIUtility.GUIToScreenPoint(r.position) + new Vector2(0, r.height);
                popupRect.width = r.width;
            }
            if (was != PressedVisible) {
                var gui = FindGUI();
                if (gui != null) {
                    if (PressedVisible)
                        gui.SetCurrentPopup(this);
                    else
                        gui.UnsetCurrentPopup(this);
                }
            }
        }

        public void PopDown()
        {
            SetPressedVisible(false);
            var gui = FindGUI();
            if (gui != null)
                gui.UnsetCurrentPopup(this);
        }

        override public void Dispose()
        {
            PopDown();
            base.Dispose();
        }

        public Rect popupRect;
        public void DoPopupGUI()
        {
            // Use onNormal as popup style (seems to suit it).
            popupStyle.normal.background = style.onNormal.background;
            popupStyle.border = style.border;

            GUILayout.BeginVertical(popupStyle);
            for (int i=0; i<list.Count(); ++i) {
                if (GUILayout.Button(GetItemString(list[i]), itemStyle)) {
                    var newindex = i;
                    Communicate(() => index = newindex);
                    SetVisibleText(GetItemString(list[i]));
                    PopDown();
                    Communicate(() => changed = true);
                }
            }
            GUILayout.EndVertical();
        }

        public override string ToString()
        {
            return "POPUP(" + StoredText().Ellipsis(10) + ")";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("Box")]
    public class Box : Widget
    {
        public enum LayoutMode { Stack, Horizontal, Vertical }
        protected LayoutMode layout;
        protected List<Widget> widgets;

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
            AddSuffix("ADDRADIOBUTTON", new TwoArgsSuffix<Button, StringValue, BooleanValue>(AddRadioButton));
            AddSuffix("ADDCHECKBOX", new TwoArgsSuffix<Button, StringValue, BooleanValue>(AddCheckbox));
            AddSuffix("ADDPOPUPMENU", new Suffix<PopupMenu>(AddPopupMenu));
            AddSuffix("ADDHSLIDER", new TwoArgsSuffix<Slider, ScalarValue, ScalarValue>(AddHSlider));
            AddSuffix("ADDVSLIDER", new TwoArgsSuffix<Slider, ScalarValue, ScalarValue>(AddVSlider));
            AddSuffix("ADDHBOX", new Suffix<Box>(AddHBox));
            AddSuffix("ADDVBOX", new Suffix<Box>(AddVBox));
            AddSuffix("ADDHLAYOUT", new Suffix<Box>(AddHLayout));
            AddSuffix("ADDVLAYOUT", new Suffix<Box>(AddVLayout));
            AddSuffix("ADDSCROLLBOX", new Suffix<ScrollBox>(AddScrollBox));
            AddSuffix("ADDSTACK", new Suffix<Box>(AddStack));
            AddSuffix("ADDSPACING", new OneArgsSuffix<Spacing, ScalarValue>(AddSpace));
            AddSuffix("WIDGETS", new Suffix<ListValue>(() => ListValue.CreateList(widgets)));
            AddSuffix("SHOWONLY", new OneArgsSuffix<Widget>(value => ShowOnly(value)));
            AddSuffix("CLEAR", new NoArgsVoidSuffix(Clear));
        }

        public void ShowOnly(Widget toshow)
        {
            for (var i = 0; i < widgets.Count; ++i) {
                var w = widgets[i];
                if (w == toshow) w.Show();
                else w.Hide();
            }
        }

        public void UnpressVisibleAllBut(Widget leave)
        {
            for (var i = 0; i < widgets.Count; ++i) {
                var w = widgets[i] as Button;
                if (w != null && w != leave) { w.SetPressedVisible(false); }
            }
        }

        public void Clear()
        {
            widgets.Clear();
            // children who try to Dispose will not be found.
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
            var w = new Box(this, Box.LayoutMode.Vertical);
            widgets.Add(w);
            return w;
        }

        public ScrollBox AddScrollBox()
        {
            var w = new ScrollBox(this);
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
            var w = Button.NewCheckbox(this, text, on);
            widgets.Add(w);
            return w;
        }

        public Button AddRadioButton(StringValue text, BooleanValue on)
        {
            var w = Button.NewRadioButton(this, text, on);
            widgets.Add(w);
            return w;
        }

        public PopupMenu AddPopupMenu()
        {
            var w = new PopupMenu(this);
            widgets.Add(w);
            return w;
        }

        public void DoChildGUIs()
        {
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
        }

        public override void DoGUI()
        {
            if (!shown) return;
            if (!enabled) GUI.enabled = false;
            if (layout == LayoutMode.Horizontal) GUILayout.BeginHorizontal(style);
            else if (layout == LayoutMode.Vertical) GUILayout.BeginVertical(style);
            DoChildGUIs();
            if (layout == LayoutMode.Horizontal) GUILayout.EndHorizontal();
            else if (layout == LayoutMode.Vertical) GUILayout.EndVertical();
        }

        public override string ToString()
        {
            return layout.ToString()[0] + "BOX";
        }
    }
    [kOS.Safe.Utilities.KOSNomenclature("ScrollBox")]
    public class ScrollBox : Box
    {
        bool hscrollalways = false;
        bool vscrollalways = false;
        Vector2 position;

        public ScrollBox(Box parent) : base(parent, LayoutMode.Vertical)
        {
            RegisterInitializer(InitializeSuffixes);
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.scrollView;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("HALWAYS", new SetSuffix<BooleanValue>(() => hscrollalways, value => hscrollalways = value));
            AddSuffix("VALWAYS", new SetSuffix<BooleanValue>(() => vscrollalways, value => vscrollalways = value));
            AddSuffix("POSITION", new SetSuffix<Vector>(() => new Vector(position.x,position.y,0), value => { position.x = (float)value.X; position.y = (float)value.Y; }));
        }

        public override void DoGUI()
        {
            if (!shown) return;
            var was = GUI.enabled;
            GUI.enabled = true; // always allow scrolling
            position = GUILayout.BeginScrollView(position,hscrollalways,vscrollalways,HighLogic.Skin.horizontalScrollbar,HighLogic.Skin.verticalScrollbar,style);
            if (layout == LayoutMode.Horizontal) GUILayout.BeginHorizontal();
            if (!enabled || !was) GUI.enabled = false;
            DoChildGUIs();
            if (layout == LayoutMode.Horizontal) GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.EndScrollView();
            GUI.enabled = was;
        }

        public override string ToString()
        {
            return "SCROLLBOX";
        }
    }

    [kOS.Safe.Utilities.KOSNomenclature("GUI")]
    public class GUIWidgets : Box, IKOSScopeObserver
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
            AddSuffix("DRAGGABLE", new SetSuffix<BooleanValue>(() => window.draggable, value => window.draggable = value));
            AddSuffix("EXTRADELAY", new SetSuffix<ScalarValue>(() => window.extraDelay, value => window.extraDelay = value));
        }

        public int LinkCount { get; set; }

        public void ScopeLost()
        {
            Dispose();
        }

        override public void Show()
        {
            window.Open();
        }
        override public void Hide()
        {
            window.Close();
        }
        override public void Dispose()
        {
            GameObject.Destroy(window.gameObject);
        }

        public bool GetShow()
        {
            return window.enabled;
        }

        public void SetShow(BooleanValue newShowVal)
        {
            window.gameObject.SetActive(newShowVal);
        }

        public void SetCurrentPopup(PopupMenu pop)
        {
            if (window.currentPopup != null) {
                if (window.currentPopup == pop) return;
                window.currentPopup.PopDown();
            }
            window.currentPopup = pop;
        }

        public void UnsetCurrentPopup(PopupMenu pop)
        {
            if (window.currentPopup == pop)
                window.currentPopup = null;
        }

        public void Communicate(Widget w, string reason, Action a)
        {
            window.Communicate(w, reason, a);
        }

        public void ClearCommunication(Widget w)
        {
            window.ClearCommunication(w);
        }

        public override string ToString()
        {
            return "GUI";
        }
    }
}
