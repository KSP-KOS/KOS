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
using kOS.Suffixed.Widget;
using kOS.Utilities;
using kOS.Communication;
using kOS.Safe;

namespace kOS.Screen
{
    public class GUIWindow : KOSManagedWindow, IUpdateObserver
    {
        private SharedObjects shared;
        public string TitleText {get; set;}
        public bool draggable = true;
        private bool uiGloballyHidden = false;
        private GUIWidgets widgets;
        private GUIStyle style;
        private GUIStyle commDelayStyle;
        private Color noControlColor = new Color(1, 0.5f, 0.5f, 0.4f);
        private Color slowControlColor = new Color(1, 0.95f, 0.95f, 0.9f);
        private Texture2D commDelayedTexture;
        public float extraDelay = 0f;
        public PopupMenu currentPopup;

        public bool ShowCursor { get; set; }

        public bool IsForShared(SharedObjects s)
        {
            return (s == shared);
        }

        public void Awake()
        {

            // Transparent - leave the widget inside it to draw background if it wants to.
            style = new GUIStyle(HighLogic.Skin.window);
            style.normal.background = null;
            style.onNormal.background = null;
            style.focused.background = null;
            style.margin = new RectOffset(0, 0, 0, 0);
            style.padding = new RectOffset(0, 0, 0, 0);

            commDelayStyle = new GUIStyle(HighLogic.Skin.label);
            commDelayStyle.normal.textColor = Color.black;
            commDelayStyle.alignment = TextAnchor.MiddleCenter;
            commDelayStyle.clipping = TextClipping.Overflow;
            commDelayStyle.stretchHeight = true;
            commDelayStyle.fontSize = 10;
            commDelayStyle.fontStyle = FontStyle.Bold;
            commDelayedTexture = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_commDelay", false);
            var solidWhite = new Texture2D(1, 1);
            solidWhite.SetPixel(0, 0, Color.white);
            commDelayStyle.normal.background = solidWhite;

            IsPowered = true;
            WindowRect = new Rect(0, 0, 0, 0); // will get resized later in AttachTo().

            GameEvents.onHideUI.Add (OnHideUI);
			GameEvents.onShowUI.Add (OnShowUI);

            // Fixes #2568 - Unity IMGUI does its own individual input locking per field that needs it,
            // so don't use KSP's more high-level control locking:
            OptOutOfControlLocking = true;
        }

        new public void OnDestroy()
        {
            if (shared != null) shared.RemoveWindow(this);
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);

            base.OnDestroy();
        }

        void OnHideUI()
        {
            uiGloballyHidden = true;
        }
        
        void OnShowUI()
        {
            uiGloballyHidden = false;            
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
            
            if (FlightResultsDialog.isDisplaying) return;
            if (uiGloballyHidden)
            {
                kOS.Safe.Encapsulation.IConfig cfg = kOS.Safe.Utilities.SafeHouse.Config;
                if (cfg == null || cfg.ObeyHideUI)
                    return;
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
            if (widgets == null) return;

            CheckConnectivity();

            widgets.DoGUI();
            if (delayedActions.Count > 0) {
                var c = GUI.color;
                GUI.color = new Color(1,1,1, Mathf.PingPong(Time.realtimeSinceStartup * 0.9f, 0.2f)+0.4f);
                var delay = delayedActions[0].time - shared.UpdateHandler.CurrentTime;
                var textHeight = 30;
                var rect = new Rect((WindowRect.width-commDelayedTexture.width) / 2, (WindowRect.height-commDelayedTexture.height- textHeight) / 2, commDelayedTexture.width, commDelayedTexture.height);
                GUI.DrawTexture(rect, commDelayedTexture);
                rect.y += commDelayedTexture.height;
                rect.x -= 10;
                rect.width += 20;
                rect.height = textHeight;
                GUI.Label(rect, string.Format("{1}\n{0:0.0} seconds", delay,delayedActions[0].reason), commDelayStyle);
                GUI.color = c;
            }

            if (draggable)
                GUI.DragWindow();
        }

        void CheckConnectivity()
        {
            if (!ConnectivityManager.HasConnectionToControl(shared.Vessel)) {
                GUI.color = noControlColor;
                GUI.enabled = false;
            } else {
                var mdelay = ConnectivityManager.GetDelayToControl(shared.Vessel) + extraDelay;
                if (mdelay > 0.1)
                    GUI.color = slowControlColor;
            }
        }

        void PopupGui(int windowId)
        {
            if (currentPopup != null) {
                CheckConnectivity();
                currentPopup.DoPopupGUI();
            }
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

        internal void AttachTo(int width, int height, string title, SharedObjects sharedObj, GUIWidgets w)
        {
            WindowRect = new Rect((UnityEngine.Screen.width-width)/2, (UnityEngine.Screen.height-height)/2, width, height);
            TitleText = title;
            widgets = w;
            shared = sharedObj;
            shared.UpdateHandler.AddObserver(this);
            shared.AddWindow(this);
        }

        internal void Detach(GUIWidgets w)
        {
            if (widgets == w) {
                widgets = null;
                Close();
            }
        }


        class ActionTime
        {
            public ActionTime(Widget w, string r, Action a, double t) { widget = w;  reason = r;  action = a; time = t; }
            public Widget widget { get; private set;}
            public string reason { get; private set;}
            public Action action { get; private set;}
            public double time { get; private set;}
        }
        List<ActionTime> delayedActions = new List<ActionTime>();
        public void Communicate(Widget w, string reason, Action a)
        {
            if (shared == null) return;

            if (ConnectivityManager.HasConnectionToControl(shared.Vessel)) {
                var mdelay = ConnectivityManager.GetDelayToControl(shared.Vessel) + extraDelay;
                if (mdelay > 0.1 || extraDelay > 0) {
                    var newat = new ActionTime(w, reason, a, shared.UpdateHandler.CurrentTime + mdelay);
                    // Linear insert because almost always it will be added at the end
                    // Only if FTL travel or if new pathways come online will it not.
                    int index = delayedActions.Count;
                    while (index-1 >= 0 && delayedActions[index - 1].time > newat.time)
                        index--;
                    delayedActions.Insert(index,newat);
                } else {
                    a();
                }
            }
        }
        public void ClearCommunication(Widget w)
        {
            for (var i=0; i<delayedActions.Count; ) {
                if (delayedActions[i].widget == w)
                    delayedActions.RemoveAt(i);
                else
                    i++;
            }
        }


        public void KOSUpdate(double deltaTime)
        {
            if (shared != null) {
                while (delayedActions.Count > 0) {
                    var next = delayedActions[0];
                    if (next.time > shared.UpdateHandler.CurrentTime)
                        break;
                    next.action();
                    delayedActions.RemoveAt(0);
                }
            }
        }

        public void Dispose()
        {
            if (shared != null)
                shared.UpdateHandler.RemoveObserver(this);
        }
    }
}
