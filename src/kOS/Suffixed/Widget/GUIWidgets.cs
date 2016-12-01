using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;
using kOS.Safe.Execution;
using kOS.Screen;
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
    [kOS.Safe.Utilities.KOSNomenclature("GUI")]
    public class GUIWidgets : Box, IKOSScopeObserver
    {
        private GUIWindow window;

        public GUIWidgets(int width, int height, SharedObjects shared) : base(null,Box.LayoutMode.Vertical)
        {
            SetStyle.padding.top = Style.padding.bottom; // no title area.
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
            window.Detach(this);
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
