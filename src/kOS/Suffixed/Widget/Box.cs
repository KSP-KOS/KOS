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
        protected LayoutMode Mode { get; private set; }
        protected List<Widget> Widgets { get; private set; }

        public int Count { get { return Widgets.Count; } }

        public Box(Box parent, LayoutMode mode) : this(parent, mode, parent.FindStyle("box"))
        {
        }

        public Box(LayoutMode mode, WidgetStyle style) : this(null, mode, style)
        {
        }

        public Box(Box parent, LayoutMode mode, WidgetStyle style) : base(parent, style)
        {
            RegisterInitializer(InitializeSuffixes);
            Mode = mode;
            Widgets = new List<Widget>();
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
            AddSuffix("WIDGETS", new Suffix<ListValue>(() => ListValue.CreateList(Widgets)));
            AddSuffix("SHOWONLY", new OneArgsSuffix<Widget>(value => ShowOnly(value)));
            AddSuffix("CLEAR", new NoArgsVoidSuffix(Clear));
        }

        public void ShowOnly(Widget toshow)
        {
            for (var i = 0; i < Widgets.Count; ++i) {
                var w = Widgets[i];
                if (w == toshow) w.Show();
                else w.Hide();
            }
        }

        public void UnpressVisibleAllBut(Widget leave)
        {
            for (var i = 0; i < Widgets.Count; ++i) {
                var w = Widgets[i] as Button;
                if (w != null && w != leave) { w.SetPressedVisible(false); }
            }
        }

        public void Clear()
        {
            Widgets.Clear();
            // children who try to Dispose will not be found.
        }

        public Spacing AddSpace(ScalarValue amount)
        {
            var w = new Spacing(this, amount);
            Widgets.Add(w);
            return w;
        }

        public Slider AddHSlider(ScalarValue min, ScalarValue max)
        {
            var w = new Slider(this, true, min, min, max);
            Widgets.Add(w);
            return w;
        }

        public Slider AddVSlider(ScalarValue min, ScalarValue max)
        {
            var w = new Slider(this, false, min, min, max);
            Widgets.Add(w);
            return w;
        }

        public Box AddStack()
        {
            var w = new Box(this, Box.LayoutMode.Stack);
            Widgets.Add(w);
            return w;
        }

        public Box AddHBox()
        {
            var w = new Box(this, Box.LayoutMode.Horizontal);
            Widgets.Add(w);
            return w;
        }

        public Box AddVBox()
        {
            var w = new Box(this, Box.LayoutMode.Vertical);
            Widgets.Add(w);
            return w;
        }

        public ScrollBox AddScrollBox()
        {
            var w = new ScrollBox(this);
            Widgets.Add(w);
            return w;
        }

        public void Remove(Widget child)
        {
            Widgets.Remove(child);
        }

        public Box AddHLayout()
        {
            var w = new Box(this, Box.LayoutMode.Horizontal, FindStyle("flatLayout"));
            Widgets.Add(w);
            return w;
        }

        public Box AddVLayout()
        {
            var w = new Box(this, Box.LayoutMode.Vertical, FindStyle("flatLayout"));
            Widgets.Add(w);
            return w;
        }

        public Label AddLabel(StringValue text)
        {
            var w = new Label(this, text);
            Widgets.Add(w);
            return w;
        }

        public TextField AddTextField(StringValue text)
        {
            var w = new TextField(this, text);
            Widgets.Add(w);
            return w;
        }

        public Button AddButton(StringValue text)
        {
            var w = new Button(this, text);
            Widgets.Add(w);
            return w;
        }

        public Button AddCheckbox(StringValue text, BooleanValue on)
        {
            var w = Button.NewCheckbox(this, text, on);
            Widgets.Add(w);
            return w;
        }

        public Button AddRadioButton(StringValue text, BooleanValue on)
        {
            var w = Button.NewRadioButton(this, text, on);
            Widgets.Add(w);
            return w;
        }

        public PopupMenu AddPopupMenu()
        {
            var w = new PopupMenu(this);
            Widgets.Add(w);
            return w;
        }

        public void DoChildGUIs()
        {
            for (var i = 0; i < Widgets.Count; ++i) {
                if (Widgets[i].Shown) {
                    var ge = GUI.enabled;
                    if (ge && !Widgets[i].Enabled) GUI.enabled = false;
                    Widgets[i].DoGUI();
                    if (ge) GUI.enabled = true;
                    if (Mode == LayoutMode.Stack)
                        break;
                }
            }
        }

        public override void DoGUI()
        {
            if (!Shown) return;
            if (!Enabled) GUI.enabled = false;
            if (Mode == LayoutMode.Horizontal) GUILayout.BeginHorizontal(ReadOnlyStyle);
            else if (Mode == LayoutMode.Vertical) GUILayout.BeginVertical(ReadOnlyStyle);
            DoChildGUIs();
            if (Mode == LayoutMode.Horizontal) GUILayout.EndHorizontal();
            else if (Mode == LayoutMode.Vertical) GUILayout.EndVertical();
        }

        public override string ToString()
        {
            return Mode.ToString()[0] + "BOX";
        }
    }
}
