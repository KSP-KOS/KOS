using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("PopupMenu")]
    public class PopupMenu : Button
    {
        private bool changed = false;
        private ListValue list;
        private int index = 0;
        private WidgetStyle popupStyle;
        private WidgetStyle itemStyle;
        private string optSuffix = "ToString";

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
            string vs = GetItemString(v);
            for (index = 0; index < list.Count(); ++index) {
                if (GetItemString(list[index]) == vs) {
                    return;
                }
            }
            index = -1;
        }

        public override void DoGUI()
        {
            bool was = PressedVisible;
            base.DoGUI();
            if (Event.current.type == EventType.Repaint) {
                Rect r = GUILayoutUtility.GetLastRect();
                popupRect.position = GUIUtility.GUIToScreenPoint(r.position) + new Vector2(0, r.height);
                popupRect.width = r.width;
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
        public void DoPopupGUI()
        {
            GUILayout.BeginVertical(popupStyle.ReadOnly);
            for (int i=0; i<list.Count(); ++i) {
                if (GUILayout.Button(GetItemString(list[i]), itemStyle.ReadOnly)) {
                    int newindex = i;
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
}
