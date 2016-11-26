using System;
using System.Linq;
using System.Collections.Generic;
using kOS.Safe.Persistence;
using UnityEngine;
using kOS.Safe.Screen;
using kOS.Module;
using kOS.UserIO;
using kOS.Safe.UserIO;
using KSP.UI.Dialogs;
using kOS.Safe.Encapsulation;
using kOS.Suffixed;
using kOS.Utilities;

namespace kOS.Screen
{
    public class GUIWindow : KOSManagedWindow
    {
        private SharedObjects shared;
        public string TitleText {get; set;}
        public bool draggable = true;
        private bool uiGloballyHidden = false;
        private GUIWidgets widgets;
        private GUIStyle style;

        public GUIWindow()
        {
            // Transparent - leave the widget inside it to draw background if it wants to.
            GUISkin theSkin = Utils.GetSkinCopy(HighLogic.Skin);
            style = new GUIStyle(theSkin.window);
            style.normal.background = null;
            style.onNormal.background = null;
            style.focused.background = null;
            style.margin = new RectOffset(0, 0, 0, 0);
            style.padding = new RectOffset(0, 0, 0, 0);

            IsPowered = true;
            WindowRect = new Rect(0, 0, 0, 0); // will get resized later in AttachTo().
        }

        public bool ShowCursor { get; set; }

        public void Awake()
        {
            GameEvents.onHideUI.Add (OnHideUI);
			GameEvents.onShowUI.Add (OnShowUI);
        }

        public void OnDestroy()
        {
            if (shared != null) shared.RemoveWindow(this);
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
        }
        
        void OnHideUI()
        {
            uiGloballyHidden = true;
        }
        
        void OnShowUI()
        {
            uiGloballyHidden = false;            
        }
        
        public override void GetFocus()
        {
        }
        
        public override void LoseFocus()
        {
        }

        public override void Open()
        {
            base.Open();
            BringToFront();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        void OnGUI()
        {
            if (!IsOpen) return;
            
            try
            {
                if (FlightResultsDialog.isDisplaying) return;
                if (uiGloballyHidden && kOS.Safe.Utilities.SafeHouse.Config.ObeyHideUI) return;
            }
            catch(NullReferenceException)
            {
            }
            
            GUI.skin = HighLogic.Skin;

            WindowRect = GUILayout.Window(UniqueId, WindowRect, WidgetGui, TitleText, style);

            if (currentPopup != null) {
                var r = RectExtensions.EnsureCompletelyVisible(currentPopup.popupRect);
                if (Event.current.type == EventType.MouseDown && !r.Contains(Event.current.mousePosition)) {
                    currentPopup.PopDown();
                } else {
                    GUI.BringWindowToFront(UniqueId + 1);
                    currentPopup.popupRect = GUILayout.Window(UniqueId + 1, r, PopupGui, "", style);
                }
            }
        }

        void Update()
        {
            if (!IsPowered) {
                Close();
                Destroy(gameObject);
                return;
            }

            if (shared == null || shared.Vessel == null || shared.Vessel.parts.Count == 0)
            {
                // Holding onto a vessel instance that no longer exists?
                Close();
            }

            if (!IsOpen ) return;
         
            UpdateLogic();
        }
        
        void WidgetGui(int windowId)
        {
            widgets.DoGUI();
            if (draggable)
                GUI.DragWindow();
        }

        void PopupGui(int windowId)
        {
            if (currentPopup != null)
                currentPopup.DoPopupGUI();
        }
        
        public Rect GetRect()
        {
            return WindowRect;
        }

        public void SetX(float x)
        {
            var r = WindowRect;
            if (x < 0) r.x = UnityEngine.Screen.width + x - r.width + 1; // X11-geometry-style negative position
            else r.x = x;
            WindowRect = r;
        }

        public void SetY(float y)
        {
            var r = WindowRect;
            if (y < 0) r.y = UnityEngine.Screen.height + y - r.height + 1;
            else r.y = y;
            WindowRect = r;
        }

        public PopupMenu currentPopup;

        internal void AttachTo(int width, int height, string title, SharedObjects sharedObj, GUIWidgets w)
        {
            WindowRect = new Rect((UnityEngine.Screen.width-width)/2, (UnityEngine.Screen.height-height)/2, width, height);
            TitleText = title;
            widgets = w;
            shared = sharedObj;
            shared.AddWindow(this);
        }
    }
}
