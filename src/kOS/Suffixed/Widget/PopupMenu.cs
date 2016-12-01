using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("PopupMenu")]
    public class PopupMenu : Button
    {
        private bool changed = false;
        private ListValue list;
        private int index = 0;
        private GUIStyle popupStyle;
        private GUIStyle itemStyle;
        private string optSuffix = "ToString";

        public PopupMenu(Box parent) : base(parent,"")
        {
            SetStyle.alignment = TextAnchor.MiddleLeft;
            IsToggle = true;

            itemStyle = new GUIStyle(Style);
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
            popupStyle.normal.background = Style.onNormal.background;
            popupStyle.border = Style.border;

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
}
