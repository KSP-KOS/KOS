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

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("GUI")]
    public class GUIWidgets : Box, IKOSScopeObserver
    {
        private GUIWindow window;
        public WidgetSkin Skin { get; private set; }

        /// <summary>
        /// All instances of this widget which have ever been constructed.
        /// Weak references used so this won't prevent them from being GC'ed.
        /// </summary>
        private static List<WeakReference> instances = new List<WeakReference>();

        public string RecentToolTip { get; private set; }

        public GUIWidgets(int width, int height, SharedObjects shared) : base(Box.LayoutMode.Vertical, new WidgetStyle(new GUIStyle(HighLogic.Skin.window)))
        {
            instances.Add(new WeakReference(this));

            RecentToolTip = "";
            var gskin = UnityEngine.Object.Instantiate(HighLogic.Skin);

            // Use Arial as that's what used in other KSP GUIs
            gskin.font = WidgetStyle.FontNamed("Arial");

            // Undo KSP weirdness with toggle style
            gskin.toggle.clipping = TextClipping.Clip;
            gskin.toggle.contentOffset = Vector2.zero;
            gskin.toggle.fixedWidth = 0;
            gskin.toggle.overflow = new RectOffset(8, -45, 10, -1);
            gskin.toggle.padding = new RectOffset(27, 0, 3, 0);
            gskin.toggle.margin = new RectOffset(4, 4, 4, 4);
            gskin.toggle.border = new RectOffset(40, 0, 40, 0);
            gskin.toggle.normal.background = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_toggle_bg_normal", false);
            gskin.toggle.onNormal.background = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_toggle_bg_onnormal", false);
            gskin.toggle.active.background = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_toggle_bg_onactive", false);
            gskin.toggle.onActive.background = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_toggle_bg_active", false);
            gskin.toggle.hover.background = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_toggle_bg_hover", false);
            gskin.toggle.onHover.background = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_toggle_bg_onhover", false);

            // Get back the style we made in the base initializer.
            gskin.window = ReadOnlyStyle;
            // no title area.
            gskin.window.padding.top = gskin.window.padding.bottom;

            // Stretch labels, otherwise ALIGN is confusing.
            gskin.label.stretchWidth = true;

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
            popupMenuItem.hover.background = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_popupmenu_bg_hover", false);
            popupMenuItem.hover.textColor = Color.black;
            popupMenuItem.active.background = popupMenuItem.hover.background;
            popupMenuItem.stretchWidth = true;
            styles.Add(popupMenuItem);

            var emptyHintStyle = new GUIStyle(gskin.label);
            emptyHintStyle.name = "emptyHintStyle";
            emptyHintStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            styles.Add(emptyHintStyle);

            var tipDisplayLabel = new GUIStyle(gskin.label);
            tipDisplayLabel.name = "tipDisplay";
            tipDisplayLabel.normal.textColor = new Color(1f, 1f, 1f, 1f);
            styles.Add(tipDisplayLabel);

            gskin.customStyles = styles.ToArray();

            Skin = new WidgetSkin(gskin);

            var go = new GameObject("kOSGUIWindow");
            window = go.AddComponent<GUIWindow>();
            window.AttachTo(width, height, "", shared, this);
            InitializeSuffixes();
        }

        // Remove me from the list of instances, if I'm in them:
        ~GUIWidgets()
        {
            // iterating in inverse order because we're deleting from
            // the list as we go, and doing it this way doesn't break
            // the index iteration as things get deleted
            for (int i = instances.Count - 1; i >= 0; --i)
            {
                if (instances[i].Target == this)
                {
                    instances.RemoveAt(i);
                }
            }
        }

        public override void DoGUI()
        {
            var prevSkin = GUI.skin;
            GUI.skin = Skin.Skin;
            base.DoGUI();
            GUI.skin = prevSkin;

            // Unity3d calls the OnGui() hook in multiple passes, not all of which do all the work.
            // The tooltip stuff only gets populated correctly in the "Repaint" pass of OnGUI().  On
            // the other passes, don't trust what it says.  This technique means the tooltip will
            // be populated one "pass" out of date (In the pass where the mouse is hovering over the
            // widget, RecentToolTip will populate, and in the NEXT OnGUI Repaint it will actually
            // be displayed.  But nobody should notice that delay.)
            if (Event.current.type == EventType.Repaint && GUI.tooltip != null)
                RecentToolTip = GUI.tooltip;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("X", new SetSuffix<ScalarValue>(() => window.GetRect().x, value => window.SetX(value)));
            AddSuffix("Y", new SetSuffix<ScalarValue>(() => window.GetRect().y, value => window.SetY(value)));
            AddSuffix("DRAGGABLE", new SetSuffix<BooleanValue>(() => window.draggable, value => window.draggable = value));
            AddSuffix("EXTRADELAY", new SetSuffix<ScalarValue>(() => window.extraDelay, value => window.extraDelay = value));
            AddSuffix("SKIN", new SetSuffix<WidgetSkin>(() => Skin, value => Skin = value));
            AddSuffix("TOOLTIP", new Suffix<StringValue>(() => RecentToolTip));
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

        // Find all instances of me and wipe them all out:
        public static void ClearAll(SharedObjects shared)
        {
            // Iterating in inverse order so the deletions
            // tend to happen in nested order from the
            // creations:
            for (int i = instances.Count - 1; i >= 0; --i)
            {
                if (instances[i].IsAlive)
                {
                    GUIWidgets g = instances[i].Target as GUIWidgets;
                    if (g.window.IsForShared(shared))
                    {
                        if (g != null) // should always be true
                        {
                            g.Hide();
                            g.Dispose();
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            return "GUI";
        }
    }
}
