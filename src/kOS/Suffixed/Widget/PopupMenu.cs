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
            SetInitialContentImage(GameDatabase.Instance.GetTexture("kOS/GFX/popupmenu", false));
            RegisterInitializer(InitializeSuffixes);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("OPTIONS", new SetSuffix<ListValue>(() => list, value => list = value));
            AddSuffix("ADDOPTION", new OneArgsSuffix<Structure>(AddOption));
            AddSuffix("VALUE", new SetSuffix<Structure>(GetValue, value => Choose(value)));
            AddSuffix("INDEX", new SetSuffix<ScalarIntValue>(() => Index, value => { Index = value; if (Index >= 0 && Index < list.Count()) SetVisibleText(GetItemString(list[Index])); }));
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
            return (Index >= 0 && Index < list.Count()) ? list[Index] : new StringValue("");
        }

        protected virtual void ScheduleChangeCallback()
        {
            if (UserOnChange != null)
            {
                UserOnChange.TriggerNextUpdate();
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
            if (list.Count() == Index)
                SetVisibleText(GetItemString(opt));
            list.Add(opt);
            Communicate(() => changed = true);
        }

        public void Choose(Structure v)
        {
            for (Index = 0; Index < list.Count(); ++Index) {
                if (list[Index] == v) {
                    return;
                }
            }
            string vs = GetItemString(v);
            for (Index = 0; Index < list.Count(); ++Index) {
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
                // This is the max height of the content plus border drawing stuff.
                int visibleRows = list.Count();
                if (maxVisible > 0 && visibleRows > maxVisible)
                    visibleRows = maxVisible;
                
                // I wish there was a better way than re-allocating this new string on every DoGUI(),
                // but because the options list length could change at any time and so could the maxvisible, we have to:
                string testString = new string('\n', visibleRows);

                float itemHeight = popupStyle.ReadOnly.CalcHeight(new GUIContent(testString), popupRect.width);
                popupRect.height = itemHeight;
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
        public void DoPopupGUI()
        {
            rememberScrollSpot = GUILayout.BeginScrollView(rememberScrollSpot, popupStyle.ReadOnly);
            GUILayout.BeginVertical(popupStyle.ReadOnly);
            for (int i=0; i<list.Count(); ++i) {
                if (GUILayout.Button(GetItemString(list[i]), itemStyle.ReadOnly)) {
                    int newindex = i;
                    Communicate(() => Index = newindex);
                    SetVisibleText(GetItemString(list[i]));
                    PopDown();
                    Communicate(() => changed = true);
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
