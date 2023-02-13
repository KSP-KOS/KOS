using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("PopupMenu")]
    public class PopupMenu : Button
    {
        private bool changed = false;
        private ListValue list;
        private int index = 0;
        public int Index
        {
            get
            {
                return index;
            }
            private set
            {
                int oldIndex = index;
                index = value;
                if (oldIndex != index)
                    ScheduleChangeCallback();
            }
        }
        private int maxVisible = 15;
        private WidgetStyle popupStyle;
        private WidgetStyle itemStyle;
        private string optSuffix = "ToString";
        public UserDelegate UserOnChange { get ; set; }

        public PopupMenu(Box parent) : base(parent,"", parent.FindStyle("popupMenu"))
        {
            IsToggle = true;

            itemStyle = FindStyle("popupMenuItem");
            popupStyle = FindStyle("popupWindow");

            list = new ListValue();
            SetInitialContentImage(Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_popupmenu", false));
            RegisterInitializer(InitializeSuffixes);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("OPTIONS", new SetSuffix<ListValue>(() => list, value => list = value));
            AddSuffix("ADDOPTION", new OneArgsSuffix<Structure>(AddOption));
            AddSuffix("VALUE", new SetSuffix<Structure>(GetValue, value => Choose(value)));
            AddSuffix("INDEX", new SetSuffix<ScalarIntValue>(() => Index, value => { Index = value; if (Index >= 0 && Index < list.Count) SetVisibleText(GetItemString(list[Index])); }));
            AddSuffix("CLEAR", new NoArgsVoidSuffix(Clear));
            AddSuffix("CHANGED", new SetSuffix<BooleanValue>(() => TakeChange(), value => changed = value));
            AddSuffix("MAXVISIBLE", new SetSuffix<ScalarIntValue>(() => maxVisible, value => maxVisible = value));
            AddSuffix("ONCHANGE", new SetSuffix<UserDelegate>(() => CallbackGetter(UserOnChange), value => UserOnChange = CallbackSetter(value)));
            AddSuffix("OPTIONSUFFIX", new SetSuffix<StringValue>(() => optSuffix, value => optSuffix = value));
        }

        public void Clear()
        {
            list.Clear();
            SetVisibleText("");
            Communicate(() => changed = true);
            // Note: we leave the index alone so things can be set in any order.
        }

        public Structure GetValue()
        {
            return (Index >= 0 && Index < list.Count) ? list[Index] : new StringValue("");
        }

        protected virtual void ScheduleChangeCallback()
        {
            if (UserOnChange != null)
            {
                if (guiCaused)
                    UserOnChange.TriggerOnFutureUpdate(InterruptPriority.CallbackOnce, GetValue());
                else
                    UserOnChange.TriggerOnNextOpcode(InterruptPriority.NoChange, GetValue());
                changed = false;
            }
        }

        public bool TakeChange()
        {
            bool r = changed;
            changed = false;
            return r;
        }

        string GetItemString(Structure item)
        {
            if (item.HasSuffix(optSuffix)) {
                ISuffixResult v = item.GetSuffix(optSuffix);
                if (v.HasValue) return v.Value.ToString();
            }
            return item.ToString();
        }

        public void AddOption(Structure opt)
        {
            if (list.Count == Index)
                SetVisibleText(GetItemString(opt));
            list.Add(opt);
            Communicate(() => changed = true);
        }

        public void Choose(Structure v)
        {
            for (Index = 0; Index < list.Count; ++Index) {
                if (list[Index] == v) {
                    return;
                }
            }
            string vs = GetItemString(v);
            for (Index = 0; Index < list.Count; ++Index) {
                if (GetItemString(list[Index]) == vs) {
                    return;
                }
            }
            Index = -1;
        }

        public override void DoGUI()
        {
            bool was = PressedVisible;
            base.DoGUI();
            if (Event.current.type == EventType.Repaint) {
                Rect r = GUILayoutUtility.GetLastRect();
                popupRect.position = GUIUtility.GUIToScreenPoint(r.position) + new Vector2(0, r.height);
                popupRect.width = r.width;

                popupRect.height = CalcPopupViewHeight();
            }
            if (was != PressedVisible) {
                GUIWidgets gui = FindGUI();
                if (gui != null) {
                    if (PressedVisible)
                        gui.SetCurrentPopup(this);
                    else
                        gui.UnsetCurrentPopup(this);
                }
            }
        }

        /// <summary>
        /// Calculates the total height in pixels of the visible window of
        /// the popup scrolling pane.
        /// </summary>
        /// <returns>The total height.</returns>
        private float CalcPopupViewHeight()
        {
            int visibleRows = list.Count;
            bool extendsPastBottom = (maxVisible > 0 && visibleRows > maxVisible);
            if (extendsPastBottom)
                visibleRows = maxVisible;

            float itemHeight = itemStyle.ReadOnly.CalcHeight(new GUIContent("XX"), popupRect.width);
            RectOffset innerPadding = popupStyle.ReadOnly.padding;

            float innerPadHeight = innerPadding.top + innerPadding.bottom;
            RectOffset windowPadding = parent.FindStyle("window").ReadOnly.padding;

            // Scrollbars still seem to exist when the window fits the content.
            // The frame has to be bigger than (not just equal to) the size of the content to suppress the scrollbars, it seems.
            // Thus this little bit of extra pixels to add on when we are trying to show all the content, unscrolled:
            float fudgeExtraToPreventScrollbar = 5f;

            float framingPadHeight = windowPadding.top + (extendsPastBottom ? 0f : windowPadding.bottom + fudgeExtraToPreventScrollbar);
            return visibleRows * itemHeight + innerPadHeight + framingPadHeight;
        }

        public void PopDown()
        {
            SetPressedVisible(false);
            GUIWidgets gui = FindGUI();
            if (gui != null)
                gui.UnsetCurrentPopup(this);
        }

        override public void Dispose()
        {
            PopDown();
            base.Dispose();
        }

        public Rect popupRect;
        private Vector2 rememberScrollSpot = new Vector2();
        private string nameFmt = "{0}_{1}";
        private string indexedName;
        public void DoPopupGUI()
        {
            rememberScrollSpot = GUILayout.BeginScrollView(rememberScrollSpot, popupStyle.ReadOnly);
            GUILayout.BeginVertical(popupStyle.ReadOnly);
            for (int i=0; i<list.Count; ++i) {

                // Might be some time savings by storing these Id names and not recalculating them each GUI pass,
                // but for now this is good enough to see if this idea works:
                myId = GUIUtility.GetControlID(FocusType.Passive);
                GUI.SetNextControlName(myId.ToString());

                if (GUILayout.Button(GetItemString(list[i]), itemStyle.ReadOnly)) {
                    int newindex = i;
                    Communicate(() => Index = newindex);
                    SetVisibleText(GetItemString(list[i]));
                    PopDown();
                    Communicate(() => changed = true);
                    GUI.FocusControl(indexedName);
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        public override string ToString()
        {
            return "POPUP(" + StoredText().Ellipsis(10) + ")";
        }
    }
}
