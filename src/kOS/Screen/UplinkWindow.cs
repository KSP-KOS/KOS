using System;
using UnityEngine;
using kOS.Safe.Screen;
using kOS.Communication;
using kOS.Safe.Encapsulation;
using kOS.Safe.Module;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using System.Linq;
using kOS.Module;

namespace kOS.Screen
{
    public class UplinkWindow : KOSManagedWindow
    {
        private const int FRAME_THICKNESS = 8;
        private const int FONT_HEIGHT = 12;
        private const string EXIT_BUTTON_TEXT = "Exit";
        private const string SEND_BUTTON_TEXT = "Send";

        private Vessel vessel;

        private Rect innerCoords;
        private Rect sendCoords;
        private Rect exitCoords;
        private Rect titleLabelCoords;
        private Rect delayLabelCoords;
        private Rect resizeButtonCoords;
        private Texture2D resizeImage;
        private bool resizeMouseDown;
        private Vector2 resizeOldSize; // width and height it had when the mouse button went down on the resize button.
        private Vector2 scrollPosition; // tracks where within the text box it's scrolled to.
        private string contents = "";
        private bool frozen;
        private bool consumeEvent;
        private bool connection;
        private double lastMsgReceiveTime = double.NaN;

        public UplinkWindow()
        {
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Freeze(bool newVal)
        {
            frozen = newVal;
        }

        public void Awake()
        {
            WindowRect = new Rect(100, 60, 470, 180);

            // Load dummy textures
            resizeImage = new Texture2D(0, 0, TextureFormat.DXT1, false);

            var urlGetter = new WWW(string.Format("file://{0}GameData/kOS/GFX/resize-button.png", KSPUtil.ApplicationRootPath.Replace("\\", "/")));
            urlGetter.LoadImageIntoTexture(resizeImage);
        }
        
        public override void GetFocus()
        {
            Freeze(false);
        }

        public override void LoseFocus()
        {
            Freeze(true);
        }

        public void AttachTo(Vessel vessel) {
            if (this.vessel != vessel)
            {
                lastMsgReceiveTime = double.NaN;
                contents = "";
            }
            this.vessel = vessel;
        }

        public override void Open()
        {
            base.Open();
            BringToFront();
        }

        public override void Close()
        {
            base.Close();
            resizeMouseDown = false;
        }

        public int GetUniqueId()
        {
            return UniqueId;
        }

        public void SetUniqueId(int newValue)
        {
            UniqueId = newValue;
        }

        public void Update()
        {
            // if the active vessel changed (or there is none), close the window
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            if (activeVessel == null || activeVessel != vessel)
            {
                Close();
            }
            UpdateLogic();
        }

        public void OnGUI()
        {
            if (!IsOpen) return;

            CalcInnerCoords();

            WindowRect = GUI.Window(UniqueId, WindowRect, ProcessWindow, "");
            // Some mouse global state data used by several of the checks:

            if (consumeEvent)
            {
                consumeEvent = false;
                Event.current.Use();
            }
        }

        protected void CalcInnerCoords()
        {
            if (!IsOpen) return;

            Vector2 titleLabSize = GUI.skin.label.CalcSize(new GUIContent(BuildTitle(false)));
            Vector2 delayLabSize = GUI.skin.label.CalcSize(new GUIContent(BuildDelay(true)));
            Vector2 exitSize = GUI.skin.box.CalcSize(new GUIContent(EXIT_BUTTON_TEXT));
            exitSize = new Vector2(exitSize.x + 4, exitSize.y + 4);
            Vector2 sendSize = GUI.skin.box.CalcSize(new GUIContent(SEND_BUTTON_TEXT));
            sendSize = new Vector2(sendSize.x + 4, sendSize.y + 4);
            
            titleLabelCoords = new Rect(5, 1, titleLabSize.x, titleLabSize.y);
            delayLabelCoords = new Rect(5, 1 + titleLabSize.y, delayLabSize.x, delayLabSize.y);
            innerCoords = new Rect(FRAME_THICKNESS,
                                    delayLabelCoords.y + 1.5f * FONT_HEIGHT,
                                    WindowRect.width - 2 * FRAME_THICKNESS,
                                    WindowRect.height - 2 * FRAME_THICKNESS - 2 * FONT_HEIGHT);

            
            float buttonXCounter = WindowRect.width; // Keep track of the x coord of leftmost button so far.

            buttonXCounter -= (exitSize.x + 5);
            exitCoords = new Rect(buttonXCounter, 1, exitSize.x, exitSize.y);

            buttonXCounter -= (sendSize.x + 2);
            sendCoords = new Rect(buttonXCounter, 1, sendSize.x, sendSize.y);

            resizeButtonCoords = new Rect(WindowRect.width - resizeImage.width,
                                           WindowRect.height - resizeImage.height,
                                           resizeImage.width,
                                           resizeImage.height);
        }

        protected string BuildTitle(bool connected = true)
        {
            if (!connected)
            {
                return "Uplink to: " + vessel.vesselName + " NO CONNECTION";
            }
            return "Uplink to: " + vessel.vesselName;
        }

        protected string BuildDelay(bool forceNoMessage = false)
        {
            double timeLeft;
            timeLeft = forceNoMessage ? double.NaN : lastMsgReceiveTime - Planetarium.GetUniversalTime();
            
            string delay;
            if (timeLeft <= 0)
            {
                delay = "Receive in: RECEIVED";
            }
            else if (timeLeft > 0)
            {
                delay = String.Format("Receive in: {0:0.###}", timeLeft);
            }
            else
            {
                delay = "Receive in: NO MESSAGE SENT";
            }
            return delay;
        }

        private void ProcessWindow(int windowId)
        {
            if (!frozen)
            {
                CheckKeyboard();
            }

            DrawWindow(windowId);

            CheckResizeDrag();
            GUI.DragWindow();
        }

        protected void CheckKeyboard()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.PageUp:
                        DoPageUp();
                        Event.current.Use();
                        break;

                    case KeyCode.PageDown:
                        DoPageDown();
                        Event.current.Use();
                        break;
                    /*
                    case KeyCode.E:
                        if (Event.current.control)
                            Close();
                        Event.current.Use();
                        break;

                    case KeyCode.S:
                        if (Event.current.control)
                            Send();
                        Event.current.Use();
                        break;
                    */
                }
            }
        }

        protected void DoPageUp()
        {
            var editor = GetWidgetController();

            // Seems to be no way to move more than one line at
            // a time - so have to do this:
            int pos = Math.Min(editor.cursorIndex, contents.Length - 1);
            int rows = ((int)innerCoords.height) / FONT_HEIGHT;
            while (rows > 0 && pos >= 0)
            {
                if (contents[pos] == '\n')
                    rows--;
                pos--;
                editor.MoveLeft();  // there is a MoveUp but it doesn't work.
            }
        }

        protected void DoPageDown()
        {
            var editor = GetWidgetController();

            // Seems to be no way to move more than one line at
            // a time - so have to do this:
            int pos = Math.Min(editor.cursorIndex, contents.Length - 1);
            int rows = ((int)innerCoords.height) / FONT_HEIGHT;
            while (rows > 0 && pos < contents.Length)
            {
                if (contents[pos] == '\n')
                    rows--;
                pos++;
                editor.MoveRight(); // there is a MoveDown but it doesn't work.
            }
        }

        protected void CheckResizeDrag()
        {
            Event e = Event.current;
            if (e.type == EventType.mouseDown && e.button == 0)
            {
                if (resizeButtonCoords.Contains(MouseButtonDownPosRelative))
                {
                    // Remember the fact that this mouseDown started on the resize button:
                    resizeMouseDown = true;
                    resizeOldSize = new Vector2(WindowRect.width, WindowRect.height);
                    Event.current.Use();
                }
            }
            if (e.type == EventType.mouseUp && e.button == 0) // mouse button went from Down to Up just now.
            {
                if (resizeMouseDown)
                {
                    resizeMouseDown = false;
                    Event.current.Use();
                }
            }
            // For some reason the Event style of checking won't let you
            // see drags extending outside the current window, while the Input style
            // will.  That's why this looks different from the others.
            if (Input.GetMouseButton(0))
            {
                if (resizeMouseDown)
                {
                    var mousePos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);
                    Vector2 dragDelta = mousePos - MouseButtonDownPosRelative;
                    WindowRect = new Rect(WindowRect.xMin,
                                            WindowRect.yMin,
                                            Math.Max(resizeOldSize.x + dragDelta.x, 100),
                                            Math.Max(resizeOldSize.y + dragDelta.y, 30));
                    CalcInnerCoords();
                    Event.current.Use();
                }
            }
        }

        protected void DrawWindow(int windowId)
        {
            connection = HasConnection();
            GUI.contentColor = Color.green;

            GUILayout.BeginArea(innerCoords);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            int preLength = contents.Length;
            contents = GUILayout.TextArea(contents);
            int postLength = contents.Length;
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.contentColor = connection ? Color.green : Color.red;
            GUI.enabled = connection;
            GUI.Label(titleLabelCoords, BuildTitle(connection));
            GUI.contentColor = Color.green;
            GUI.Label(delayLabelCoords, BuildDelay());
            
            if (GUI.Button(sendCoords, SEND_BUTTON_TEXT))
            {
                Send();
            }
            GUI.enabled = true;
            if (GUI.Button(exitCoords, EXIT_BUTTON_TEXT))
            {
                Close();
            }
            KeepCursorScrolledInView();

            GUI.Box(resizeButtonCoords, resizeImage);
        }

        protected void KeepCursorScrolledInView()
        {
            // It's utterly ridiculous that Unity's TextArea widget doesn't
            // just do this automatically.  It's basic behavior for a scrolling
            // text widget that when the text cursor moves out of the viewport you
            // scroll to keep it in view.  Oh well, have to do it manually:
            //
            // NOTE: This method is what is interfering with the scrollbar's ability
            // to scroll with the mouse - this routine is locking the scrollbar
            // to only be allowed to move as far as the cursor is still in view.
            // Fixing that would take a bit of work.
            //

            var editor = GetWidgetController();
            Vector2 pos = editor.graphicalCursorPos;
            float usableHeight = innerCoords.height - 2.5f * FONT_HEIGHT;
            if (pos.y < scrollPosition.y)
                scrollPosition.y = pos.y;
            else if (pos.y > scrollPosition.y + usableHeight)
                scrollPosition.y = pos.y - usableHeight;
        }

        // Return type needs full namespace path because kOS namespace has a TextEditor class too:
        protected UnityEngine.TextEditor GetWidgetController()
        {
            // Whichever TextEdit widget has current focus (should be this one if processing input):
            // There seems to be no way to grab the text edit controller of a Unity Widget by
            // specific ID.
            return (UnityEngine.TextEditor)
                GUIUtility.GetStateObject(typeof(UnityEngine.TextEditor), GUIUtility.keyboardControl);
        }

        private bool HasConnection()
        {
            return ConnectivityManager.HasConnectionToHome(vessel);
        }

        public void Send()
        {
            if (!HasConnection()) {
                return;
            }
            
            Structure payload = new StringValue(contents);

            MessageQueueStructure queue = InterVesselManager.Instance.GetQueue(vessel, null);

            double delay = ConnectivityManager.GetDelayToHome(vessel) + 15;
            double sentAt = Planetarium.GetUniversalTime();
            double receivedAt = sentAt + delay;
            Message msg = Message.Create(payload, sentAt, receivedAt, KscTarget.Instance, null);
            queue.Push(msg);
            lastMsgReceiveTime = receivedAt;
        }
    }
}
