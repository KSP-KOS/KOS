using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
using UnityEngine;
using System.Collections.Generic;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("Box")]
    public class Box : Widget
    {
        public enum LayoutMode { Stack, Horizontal, Vertical }
        protected LayoutMode Mode { get; private set; }
        protected List<Widget> Widgets { get; private set; }

        public int Count { get { return Widgets.Count; } }

        public UserDelegate UserOnRadioChange { get ; set; }

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
            AddSuffix("ADDLABEL", new OptionalArgsSuffix<Label>(AddLabel, new Structure[] { new StringValue ("") }));
            AddSuffix("ADDTEXTFIELD", new OptionalArgsSuffix<TextField>(AddTextField, new Structure [] { new StringValue ("") }));
            AddSuffix("ADDBUTTON", new OptionalArgsSuffix<Button>(AddButton, new Structure [] { new StringValue ("") }));
            AddSuffix("ADDRADIOBUTTON", new OptionalArgsSuffix<Button>(AddRadioButton, new Structure [] { new StringValue(""), new BooleanValue(false) }));
            AddSuffix("ADDCHECKBOX", new OptionalArgsSuffix<Button>(AddCheckbox, new Structure [] { new StringValue(""), new BooleanValue(false) }));
            AddSuffix("ADDPOPUPMENU", new Suffix<PopupMenu>(AddPopupMenu));
            AddSuffix("ADDHSLIDER", new OptionalArgsSuffix<Slider>(AddHSlider, new Structure [] { new ScalarDoubleValue (0), new ScalarDoubleValue (0), new ScalarDoubleValue (1) }));
            AddSuffix("ADDVSLIDER", new OptionalArgsSuffix<Slider>(AddVSlider, new Structure [] { new ScalarDoubleValue (0), new ScalarDoubleValue (0), new ScalarDoubleValue (1) }));
            AddSuffix("ADDHBOX", new Suffix<Box>(AddHBox));
            AddSuffix("ADDVBOX", new Suffix<Box>(AddVBox));
            AddSuffix("ADDHLAYOUT", new Suffix<Box>(AddHLayout));
            AddSuffix("ADDVLAYOUT", new Suffix<Box>(AddVLayout));
            AddSuffix("ADDSCROLLBOX", new Suffix<ScrollBox>(AddScrollBox));
            AddSuffix("ADDSTACK", new Suffix<Box>(AddStack));
            AddSuffix("ADDSPACING", new OptionalArgsSuffix<Spacing>(AddSpace, new Structure [] { new ScalarIntValue(-1) }));
            AddSuffix("ADDTIPDISPLAY", new OptionalArgsSuffix<Label>(AddTipDisplay, new Structure[] { new StringValue("") }));
            AddSuffix("WIDGETS", new Suffix<ListValue>(() => ListValue.CreateList(Widgets)));
            AddSuffix("RADIOVALUE", new Suffix<StringValue>(() => new StringValue(GetRadioValue())));
            AddSuffix("ONRADIOCHANGE", new SetSuffix<UserDelegate>(() => CallbackGetter(UserOnRadioChange), value => UserOnRadioChange = CallbackSetter(value)));
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

        public void ScheduleOnRadioChange(Button b)
        {
            if (UserOnRadioChange != null)
            {
                if (guiCaused)
                    UserOnRadioChange.TriggerOnFutureUpdate(InterruptPriority.CallbackOnce, b);
                else
                    UserOnRadioChange.TriggerOnNextOpcode(InterruptPriority.NoChange, b);
            }
        }

        /// <summary>
        /// Gets which radio button inside this box has the "on" value, if there
        /// is one.  Returns null if there's no such radio buttons or all are off.
        /// </summary>
        /// <returns>The radio button that is on.</returns>
        public Button WhichRadioButtonOn()
        {
            for (int i = 0; i < Widgets.Count; ++i)
            {
                Button b = Widgets[i] as Button;
                if (b != null && b.IsExclusive == true && b.Pressed)
                    return b;
            }
            return null;
        }

        /// <summary>
        /// Gets the string value of the radio button that's down, or empty string
        /// if all radio buttons are up, or there are no radio buttons.
        /// </summary>
        /// <returns>The down button's radio value</returns>
        public string GetRadioValue()
        {
            Button b = WhichRadioButtonOn();
            if (b != null)
                return b.Text;
            return "";
        }

        public void Clear()
        {
            Widgets.Clear();
            // children who try to Dispose will not be found.
        }

        public Spacing AddSpace(params Structure [] args)
        {
            var w = new Spacing(this, (ScalarValue) args[0]);
            Widgets.Add(w);
            return w;
        }

        public Slider AddHSlider(params Structure [] args)
        {
            var w = new Slider(this, true, (ScalarValue) args[0], (ScalarValue) args[1], (ScalarValue) args[2]);
            Widgets.Add(w);
            return w;
        }

        public Slider AddVSlider(params Structure [] args)
        {
            var w = new Slider(this, false, (ScalarValue) args[0], (ScalarValue) args[1], (ScalarValue) args[2]);
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

        public Label AddLabel(params Structure [] args)
        {
            var w = new Label(this, (StringValue) args[0]);
            Widgets.Add(w);
            return w;
        }

        public Label AddTipDisplay(params Structure [] args)
        {
            var w = new TipDisplay(this, (args.Length == 0 ? (StringValue)"" : (StringValue) args[0]) );
            Widgets.Add(w);
            return w;
        }

        public TextField AddTextField(params Structure [] args)
        {
            var w = new TextField(this, (StringValue) args[0]);
            Widgets.Add(w);
            return w;
        }

        public Button AddButton(params Structure [] args)
        {
            var w = new Button(this, (StringValue) args[0]);
            Widgets.Add(w);
            return w;
        }

        public Button AddCheckbox(params Structure [] args)
        {
            var w = Button.NewCheckbox(this, (StringValue) args[0], (BooleanValue) args [1]);
            Widgets.Add(w);
            return w;
        }

        public Button AddRadioButton(params Structure [] args)
        {
            var w = Button.NewRadioButton(this, (StringValue) args [0], (BooleanValue) args [1]);
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
