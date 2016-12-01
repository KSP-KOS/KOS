using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;
using System.Collections.Generic;

namespace kOS.Suffixed
{
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
}
