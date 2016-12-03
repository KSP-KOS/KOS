using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;
using kOS.Safe.Execution;
using kOS.Screen;
using System;
using kOS.Utilities;
using System.Collections.Generic;

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
        public WidgetSkin Skin { get; private set; }

        public GUIWidgets(int width, int height, SharedObjects shared) : base(Box.LayoutMode.Vertical,new WidgetStyle(new GUIStyle(HighLogic.Skin.window)))
        {
            var gskin = Utils.GetSkinCopy(HighLogic.Skin);

            // Get back the style we made in the base initializer.
            gskin.window = ReadOnlyStyle;
            // no title area.
            gskin.window.padding.top = gskin.window.padding.bottom;

            // align better with labels.
            gskin.horizontalSlider.margin.top = 8;
            gskin.horizontalSlider.margin.bottom = 8;

            List<GUIStyle> styles = new List<GUIStyle>(gskin.customStyles);

            var flatLayout = new GUIStyle(gskin.box);
            flatLayout.name = "flatLayout";
            flatLayout.margin = new RectOffset(0, 0, 0, 0);
            flatLayout.padding = new RectOffset(0, 0, 0, 0);
            flatLayout.normal.background = null;
            styles.Add(flatLayout);

            var popupWindow = new GUIStyle(gskin.window);
            popupWindow.name = "popupWindow";
            popupWindow.padding.left = 0;
            popupWindow.padding.right = 0;
            popupWindow.margin = new RectOffset(0, 0, 0, 0);
            popupWindow.normal.background = gskin.button.onNormal.background;
            popupWindow.border = gskin.button.border;

            styles.Add(popupWindow);

            var popupMenu = new GUIStyle(gskin.button);
            popupMenu.name = "popupMenu";
            popupMenu.alignment = TextAnchor.MiddleLeft;
            styles.Add(popupMenu);

            var popupMenuItem = new GUIStyle(gskin.label);
            popupMenuItem.name = "popupMenuItem";
            popupMenuItem.margin.top = 0;
            popupMenuItem.margin.bottom = 0;
            popupMenuItem.normal.background = null;
            popupMenuItem.hover.background = GameDatabase.Instance.GetTexture("kOS/GFX/popupmenu_bg_hover", false);
            popupMenuItem.hover.textColor = Color.black;
            popupMenuItem.active.background = popupMenuItem.hover.background;
            popupMenuItem.stretchWidth = true;
            styles.Add(popupMenuItem);

            var labelTipOverlay = new GUIStyle(gskin.label);
            labelTipOverlay.name = "labelTipOverlay";
            labelTipOverlay.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 0.2f);
            styles.Add(labelTipOverlay);

            gskin.customStyles = styles.ToArray();

            Skin = new WidgetSkin(gskin);

            var go = new GameObject("kOSGUIWindow");
            window = go.AddComponent<GUIWindow>();
            window.AttachTo(width,height,"",shared,this);
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("X", new SetSuffix<ScalarValue>(() => window.GetRect().x, value => window.SetX(value)));
            AddSuffix("Y", new SetSuffix<ScalarValue>(() => window.GetRect().y, value => window.SetY(value)));
            AddSuffix("DRAGGABLE", new SetSuffix<BooleanValue>(() => window.draggable, value => window.draggable = value));
            AddSuffix("EXTRADELAY", new SetSuffix<ScalarValue>(() => window.extraDelay, value => window.extraDelay = value));
            AddSuffix("SKIN", new SetSuffix<WidgetSkin>(() => Skin, value => Skin = value));
        }

        public int LinkCount { get; set; }

        public void ScopeLost()
        {
            Dispose();
        }

        override public void Show()
        {
            base.Show();
            window.Open();
        }
        override public void Hide()
        {
            base.Hide();
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
