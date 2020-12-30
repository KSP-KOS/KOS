using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;
using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Module;

namespace kOS.Screen
{
    /// <summary>
    /// A Unity window that contains the text editor for kOS inside it.  It should only be
    /// popped into existence when you feed it a file using the AttachTo call.
    /// </summary>
    public class KOSTextEditPopup : KOSManagedWindow
    {
        private const int FRAME_THICKNESS = 8;
        private const string EXIT_BUTTON_TEXT = "(E)xit";
        private const string SAVE_BUTTON_TEXT = "(S)ave";
        private const string RELOAD_BUTTON_TEXT = "(R)eload";

        private int fontHeight = 12;
        private Font font;
        // A list of fonts including the user's choice plus a few fallback options if the user's choice isn't working:
        private string[] tryFontNames =
            new string[] {
               "_User's choice_",
               "Consolas Bold", "Consolas", "Monaco Bold", "Monaco", "Liberation Mono Bold", "Liberation Mono",
               "Courier New Bold", "Courier Bold", "Courier New", "Courier", "Arial" };
        private string prevConfigFontName = "";
        private int prevConfigFontSize = 12;
        private Rect innerCoords;
        private Rect saveCoords;
        private Rect exitCoords;
        private Rect labelCoords;
        private Rect reloadCoords;
        private Rect resizeButtonCoords;
        private TermWindow term; // The terminal that this popup is attached to.
        private GlobalPath loadingPath;
        private Volume volume;
        private Volume loadingVolume;
        private string contents = "";
        private Texture2D resizeImage;
        private bool resizeMouseDown;
        private Vector2 resizeOldSize; // width and height it had when the mouse button went down on the resize button.
        private bool isDirty; // have any edits occurred since last load or save?
        private bool frozen;
        private DelegateDialog dialog;
        private Vector2 scrollPosition; // tracks where within the text box it's scrolled to.
        private bool consumeEvent;
        private GUIStyle textWidgetStyle;

        public KOSTextEditPopup()
        {
            UniqueId = 100; // This is expected to be overridden, but Unity requires that
                            // KosTextEditPopup() be a constructor that takes zero arguments,
                            // so the real WindowId has to be set after construction.
        }

        public void Freeze(bool newVal)
        {
            frozen = newVal;
        }

        public void Awake()
        {
            WindowRect = new Rect(0, 0, 470, 280); // bogus starting value will be changed later when attaching to a terminal.

            // Load dummy textures
            resizeImage = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_resize-button", false);

            dialog = gameObject.AddComponent<DelegateDialog>();

            GetFont();
        }

        private void GetFont()
        {
            fontHeight = GetConfigFontSize();
            tryFontNames[0] = GetConfigFontName();

            font = AssetManager.Instance.GetSystemFontByNameAndSize(tryFontNames, fontHeight, false);

            textWidgetStyle = new GUIStyle(HighLogic.Skin.textArea);
            textWidgetStyle.font = font;

            prevConfigFontSize = fontHeight;
            prevConfigFontName = tryFontNames[0];
        }

        private string GetConfigFontName()
        {
            return SafeHouse.Config.TerminalFontName;
        }

        private int GetConfigFontSize()
        {
            // For a few moments upon first activating the text editor popup, the
            // TermWindow isn't attached yet so the term member is null.  Return a
            // dummy value at first but then correct it later the next time this is
            // queired after the term is attached:
            return (term != null ) ? term.GetFontSize() : 8;
        }

        public void AttachTo(TermWindow termWindow, Volume attachVolume, GlobalPath path)
        {
            term = termWindow;
            WindowRect = new Rect(0, 0, 470, 280); // will be resized and moved in onGUI.
            frozen = false;
            loadingVolume = attachVolume;
            loadingPath = path;
            LoadContents(attachVolume, path);
        }

        public bool Contains(Vector2 posAbs)
        {
            return WindowRect.Contains(posAbs);
        }

        public override void GetFocus()
        {
            Freeze(false);
        }

        public override void LoseFocus()
        {
            Freeze(true);
        }

        public override void Open()
        {
            isDirty = false;
            base.Open();
            BringToFront();
        }

        public override void Close()
        {
            isDirty = false;
            base.Close();
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
            // Only stay open as long as the attached terminal window stays open:
            if (IsOpen && (term == null || !(term.IsPowered)))
                IsOpen = false;
            UpdateLogic();
        }

        public void OnGUI()
        {
            if (!IsOpen) return;

            // If the config options changed the font for me, reload it:
            if ((! prevConfigFontName.Equals(GetConfigFontName())) || prevConfigFontSize != GetConfigFontSize())
                GetFont();

            CalcOuterCoords(); // force windowRect to lock to bottom edge of the parents
            CalcInnerCoords();

            WindowRect = GUI.Window(UniqueId, WindowRect, ProcessWindow, "");
            // Some mouse global state data used by several of the checks:

            if (consumeEvent)
            {
                consumeEvent = false;
                Event.current.Use();
            }
        }

        protected void ExitEditor()
        {
            if (isDirty)
                InvokeDirtySaveExitDialog();
            else
                Close();
        }

        public void SaveContents()
        {
            if (volume.SaveFile(loadingPath, new FileContent(contents)) == null)
            {
                // For some reason the normal trap that prints exceptions on
                // the terminal doesn't work here in this part of the code,
                // thus the two messages:
                term.Print("[File Save failed. Check space on device?]");
                throw new Exception("File Save Failed from Text Editor.");
            }
            isDirty = false;
            term.Print("[Saved changes to " + loadingPath + "]");
        }

        protected void ReloadContents()
        {
            if (isDirty)
                InvokeReloadConfirmDialog();
            else
                DelegateLoadContents(this);
        }

        public void LoadContents(Volume vol, GlobalPath path)
        {
            if (isDirty)
            {
                Freeze(true);
                InvokeDirtySaveLoadDialog();
                loadingVolume = vol;
                loadingPath = path;
            }
            else
            {
                loadingVolume = vol;
                loadingPath = path;
                DelegateLoadContents(this);
            }
        }

        protected void InvokeDirtySaveExitDialog()
        {
            var choices = new List<string>();
            var actions = new List<DialogAction>();
            choices.Add("Yes");
            actions.Add(DelegateSaveExit);
            choices.Add("No");
            actions.Add(DelegateNoSaveExit);
            choices.Add("Cancel");
            actions.Add(DelegateCancel);

            dialog.Invoke(this, "\"" + loadingPath + "\" has been edited.  Save it before exiting?", choices, actions);
        }

        protected void InvokeDirtySaveLoadDialog()
        {
            var choices = new List<string>();
            var actions = new List<DialogAction>();
            choices.Add("Yes");
            actions.Add(DelegateSaveThenLoad);
            choices.Add("No");
            actions.Add(DelegateLoadContents);
            choices.Add("Cancel");
            actions.Add(DelegateCancel);

            dialog.Invoke(this, "\"" + loadingPath + "\" has been edited.  Save before loading \"" + loadingPath.Name + "\"?", choices, actions);
        }

        protected void InvokeReloadConfirmDialog()
        {
            var choices = new List<string>();
            var actions = new List<DialogAction>();
            choices.Add("Yes");
            actions.Add(DelegateLoadContents);
            choices.Add("No");
            actions.Add(DelegateCancel);

            dialog.Invoke(this, "\"" + loadingPath + "\" has been edited.  Throw away changes and reload?", choices, actions);
        }

        protected static void DelegateSaveExit(KOSTextEditPopup me)
        {
            me.SaveContents();
            me.Freeze(false);
            me.Close();
        }

        protected static void DelegateNoSaveExit(KOSTextEditPopup me)
        {
            me.Freeze(false);
            me.Close();
        }

        protected static void DelegateSaveThenLoad(KOSTextEditPopup me)
        {
            me.SaveContents();
            DelegateLoadContents(me);
        }

        protected static void DelegateLoadContents(KOSTextEditPopup me)
        {
            VolumeItem item = me.loadingVolume.Open(me.loadingPath);
            if (item == null)
            {
                me.term.Print("[New File]");
                me.contents = "";
            }
            else if (item is VolumeFile)
            {
                VolumeFile file = item as VolumeFile;
                me.loadingPath = GlobalPath.FromVolumePath(item.Path, me.loadingPath.VolumeId);
                me.contents = file.ReadAll().String;
            }
            else
            {
                throw new KOSPersistenceException("Path '" + me.loadingPath + "' points to a directory");
            }

            me.volume = me.loadingVolume;
            me.isDirty = false;
        }

        protected static void DelegateCancel(KOSTextEditPopup me)
        {
            // do nothing.
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

                    case KeyCode.E:
                        if (Event.current.control)
                        {
                            ExitEditor();
                            Event.current.Use();
                        }
                        break;

                    case KeyCode.S:
                        if (Event.current.control)
                        {
                            SaveContents();
                            Event.current.Use();
                        }
                        break;

                    case KeyCode.R:
                        if (Event.current.control)
                        {
                            ReloadContents();
                            Event.current.Use();
                        }
                        break;
                }
            }
        }

        protected void DoPageUp()
        {
            var editor = GetWidgetController();

            // Seems to be no way to move more than one line at
            // a time - so have to do this:
            int pos = Math.Min(editor.cursorIndex, contents.Length - 1);
            int rows = ((int)innerCoords.height) / fontHeight;
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
            int rows = ((int)innerCoords.height) / fontHeight;
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
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                // Remember the fact that this mouseDown started on the resize button:
                if (resizeButtonCoords.Contains(MouseButtonDownPosRelative))
                {
                    resizeMouseDown = true;
                    resizeOldSize = new Vector2(WindowRect.width, WindowRect.height);
                    Event.current.Use();
                }
            }
            if (e.type == EventType.MouseUp && e.button == 0) // mouse button went from Down to Up just now.
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

        private void ProcessWindow(int windowId)
        {
            if (!frozen)
                CheckKeyboard();

            DrawWindow(windowId);

            CheckResizeDrag();

            CalcOuterCoords();
        }

        protected void DrawWindow(int windowId)
        {
            GUI.contentColor = Color.yellow;

            GUILayout.BeginArea(innerCoords);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            int preLength = contents.Length;
            contents = GUILayout.TextArea(contents, textWidgetStyle);
            int postLength = contents.Length;
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.Label(labelCoords, BuildTitle());
            if (GUI.Button(exitCoords, EXIT_BUTTON_TEXT))
            {
                ExitEditor();
            }
            if (GUI.Button(saveCoords, SAVE_BUTTON_TEXT))
            {
                SaveContents();
            }
            if (GUI.Button(reloadCoords, RELOAD_BUTTON_TEXT))
            {
                ReloadContents();
            }
            KeepCursorScrolledInView();

            GUI.Box(resizeButtonCoords, resizeImage);

            if (preLength != postLength)
            {
                isDirty = true;
            }
        }

        protected void CalcOuterCoords()
        {
            if (IsOpen && term != null)
            {
                Rect tRect = term.GetRect();

                // Glue it to the bottom of the attached term window - move wherever it moves:
                float left = tRect.xMin;
                float top = tRect.yMin + tRect.height;

                // If it hasn't been given a size yet, then give it a starting size that matches
                // the attached terminal window size.  Otherwise keep whatever size the user changed it to:
                if (WindowRect.width == 0)
                    WindowRect = new Rect(left, top, tRect.width, tRect.height);
                else
                    WindowRect = new Rect(left, top, WindowRect.width, WindowRect.height);
            }
        }

        protected void CalcInnerCoords()
        {
            if (!IsOpen) return;

            innerCoords = new Rect(FRAME_THICKNESS,
                                    FRAME_THICKNESS + 1.5f * fontHeight,
                                    WindowRect.width - 2 * FRAME_THICKNESS,
                                    WindowRect.height - 2 * FRAME_THICKNESS - 2 * fontHeight);

            Vector2 labSize = GUI.skin.label.CalcSize(new GUIContent(BuildTitle()));
            Vector2 exitSize = GUI.skin.box.CalcSize(new GUIContent(EXIT_BUTTON_TEXT));
            exitSize = new Vector2(exitSize.x + 4, exitSize.y + 4);
            Vector2 saveSize = GUI.skin.box.CalcSize(new GUIContent(SAVE_BUTTON_TEXT));
            saveSize = new Vector2(saveSize.x + 4, saveSize.y + 4);
            Vector2 reloadSize = GUI.skin.box.CalcSize(new GUIContent(RELOAD_BUTTON_TEXT));
            reloadSize = new Vector2(reloadSize.x + 4, reloadSize.y + 4);

            labelCoords = new Rect(5, 1, labSize.x, labSize.y);

            float buttonXCounter = WindowRect.width; // Keep track of the x coord of leftmost button so far.

            buttonXCounter -= (exitSize.x + 5);
            exitCoords = new Rect(buttonXCounter, 1, exitSize.x, exitSize.y);

            buttonXCounter -= (saveSize.x + 2);
            saveCoords = new Rect(buttonXCounter, 1, saveSize.x, saveSize.y);

            buttonXCounter -= (reloadSize.x + 2);
            reloadCoords = new Rect(buttonXCounter, 1, reloadSize.x, reloadSize.y);

            resizeButtonCoords = new Rect(WindowRect.width - resizeImage.width,
                                           WindowRect.height - resizeImage.height,
                                           resizeImage.width,
                                           resizeImage.height);
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
            float usableHeight = innerCoords.height - 2.5f * fontHeight;
            if (pos.y < scrollPosition.y)
                scrollPosition.y = pos.y;
            else if (pos.y > scrollPosition.y + usableHeight)
                scrollPosition.y = pos.y - usableHeight;
        }

        // Return type needs full namespace path because kOS namespace has a TextEditor class too:
        protected TextEditor GetWidgetController()
        {
            // Whichever TextEdit widget has current focus (should be this one if processing input):
            // There seems to be no way to grab the text edit controller of a Unity Widget by
            // specific ID.
            return (TextEditor)
                GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
        }

        public Rect GetRect()
        {
            return WindowRect;
        }

        protected string BuildTitle()
        {
            return loadingPath.ToString();
        }
    }
}