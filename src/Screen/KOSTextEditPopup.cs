using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Persistence;

namespace kOS.Screen
{
    /// <summary>
    /// A Unity window that contains the text editor for kOS inside it.  It should only be
    /// popped into existence when you feed it a file using the AttachTo call.
    /// </summary>
    public class KOSTextEditPopup : MonoBehaviour
    {
        private const int FRAME_THICKNESS = 8;
        private const int FONT_HEIGHT = 12;
        private const string EXIT_BUTTON_TEXT = "(E)xit";
        private const string SAVE_BUTTON_TEXT = "(S)ave";
        private const string RELOAD_BUTTON_TEXT = "(R)eload";

        private Rect outerCoords;
        private Rect innerCoords;
        private Rect saveCoords;
        private Rect exitCoords;
        private Rect labelCoords;
        private Rect reloadCoords;
        private Rect resizeButtonCoords;
        private TermWindow term; // The terminal that this popup is attached to.
        private string fileName = "";
        private string loadingFileName = "";
        private Volume volume;
        private Volume loadingVolume;
        private string contents = "";
        private bool isOpen;
        private readonly Texture2D resizeImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private bool resizeMouseDown;
        private Vector2 mouseDownPos; // Position the mouse was at when button went down.
        private Vector2 mouseUpPos;   // Position the mouse was at when button went up.
        private Vector2 resizeOldSize; // width and height it had when the mouse button went down on the resize button.
        private bool isDirty; // have any edits occured since last load or save?
        private bool frozen;
        private DelegateDialog dialog;
        private Vector2 scrollPosition; // tracks where within the text box it's scrolled to.

        public KOSTextEditPopup()
        {
            WindowID = 100;
        }

        public int WindowID { get; private set; }

        public void Freeze(bool newVal)
        {
            frozen = newVal;
        }
        
        public void Awake()
        {
            var gObj = new GameObject( "texteditConfirm", typeof(DelegateDialog) );
            DontDestroyOnLoad(gObj);
            dialog = ((DelegateDialog)gObj.GetComponent(typeof(DelegateDialog)));
            var urlGetter = new WWW( string.Format("file://{0}GameData/kOS/GFX/resize-button.png", KSPUtil.ApplicationRootPath.Replace("\\", "/")) );
            urlGetter.LoadImageIntoTexture( resizeImage );
        }

        public void AttachTo( TermWindow termWindow, Volume attachVolume, string attachFileName = "" )
        {
            term = termWindow;
            outerCoords = new Rect(0,0,0,0); // will be resized and moved in onGUI.
            frozen = false;
            loadingVolume = attachVolume;
            loadingFileName = attachFileName;
            LoadContents(attachVolume, attachFileName);
        }
        
        public void Open()
        {
            isOpen = true;
            isDirty = false;
            Freeze(false);
        }
        
        public void Close()
        {
            isOpen = false;
            isDirty = false;
            Freeze(false);
        }
        
        public void Update()
        {
            // Only stay open as long as the attached terminal window stays open:
            if (isOpen && term != null && term.IsOpen && term.IsPowered )
                isOpen = true;
            else
                isOpen = false;
        }
        
        public void OnGUI()
        {
            if (isOpen && !frozen)
            {
                CalcOuterCoords();
                CalcInnerCoords();

                // Unity wants to constantly see the Window constructor run again on each
                // onGUI call, or else it goes away:
                if (isOpen)
                    GUI.Window( WindowID, outerCoords, ProcessWindow, "" );
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
            var file = new ProgramFile(fileName) {Content = contents};

            if (! volume.SaveFile(file) )
            {
                // For some reason the normal trap that prints exeptions on
                // the terminal doesn't work here in this part of the code,
                // thus the two messages:
                term.Print("[File Save failed. Check space on device?]");
                throw new Exception( "File Save Failed from Text Editor.");
            }
            isDirty = false;
            term.Print("[Saved changes to " + fileName + "]");
        }
        
        protected void ReloadContents()
        {
            if (isDirty)
                InvokeReloadConfirmDialog();
            else
                DelegateLoadContents(this);                
        }

        public void LoadContents( Volume vol, string fName )
        {
            if (isDirty)
            {
                Freeze(true);
                InvokeDirtySaveLoadDialog();
                loadingVolume = vol;
                loadingFileName = fName;
            }
            else
            {
                loadingVolume = vol;
                loadingFileName = fName;
                DelegateLoadContents(this);
            }
        }
        
        protected void InvokeDirtySaveExitDialog()
        {
            var choices = new List<string>();
            var actions = new List<DialogAction>();
            choices.Add( "Yes" );
            actions.Add( DelegateSaveExit );
            choices.Add( "No" );
            actions.Add( DelegateNoSaveExit );
            choices.Add( "Cancel" );
            actions.Add( DelegateCancel );
            
            dialog.Invoke( this, "\""+fileName+"\" has been edited.  Save it before exiting?", choices, actions );
        }

        protected void InvokeDirtySaveLoadDialog()
        {
            var choices = new List<string>();
            var actions = new List<DialogAction>();
            choices.Add( "Yes" );
            actions.Add( DelegateSaveThenLoad );
            choices.Add( "No" );
            actions.Add( DelegateLoadContents );
            choices.Add( "Cancel" );
            actions.Add( DelegateCancel );
            
            dialog.Invoke( this, "\""+fileName+"\" has been edited.  Save before loading \""+loadingFileName+"\"?", choices, actions );
        }

        protected void InvokeReloadConfirmDialog()
        {
            var choices = new List<string>();
            var actions = new List<DialogAction>();
            choices.Add( "Yes" );
            actions.Add( DelegateLoadContents );
            choices.Add( "No" );
            actions.Add( DelegateCancel );
            
            dialog.Invoke( this, "\""+fileName+"\" has been edited.  Throw away changes and reload?", choices, actions );
        }
        
        protected static void DelegateSaveExit( KOSTextEditPopup me )
        {
            me.SaveContents();
            me.Freeze(false);
            me.Close();
        }

        protected static void DelegateNoSaveExit( KOSTextEditPopup me )
        {
            me.Freeze(false);
            me.Close();
        }
        
        protected static void DelegateSaveThenLoad( KOSTextEditPopup me )
        {
            me.SaveContents();
            DelegateLoadContents( me );
        }

        protected static void DelegateLoadContents( KOSTextEditPopup me )
        {
            me.volume = me.loadingVolume;
            me.fileName = me.loadingFileName;
            ProgramFile file = me.volume.GetByName( me.fileName );
            if ( file == null )
            {
                me.term.Print("[New File]");
                me.contents = "";
            }
            else
            {
                me.contents = file.Content;
            }
            me.isDirty = false;
        }

        protected static void DelegateCancel( KOSTextEditPopup me )
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
                            ExitEditor();
                        Event.current.Use();
                        break;
                    case KeyCode.S:
                        if (Event.current.control)
                            SaveContents();
                        Event.current.Use();
                        break;
                    case KeyCode.R:
                        if (Event.current.control)
                            ReloadContents();
                        Event.current.Use();
                        break;
                }
            }
        }
        
        protected void DoPageUp()
        {
            UnityEngine.TextEditor editor = GetWidgetController();

            // Seems to be no way to move more than one line at
            // a time - so have to do this:
            int pos = Math.Min( editor.pos, contents.Length - 1);
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
            UnityEngine.TextEditor editor = GetWidgetController();          
            
            // Seems to be no way to move more than one line at
            // a time - so have to do this:
            int pos = Math.Min( editor.pos, contents.Length - 1);
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
                // Rememeber the fact that this mouseDown started on the resize button:
                if (resizeButtonCoords.Contains(mouseDownPos))
                {
                    resizeMouseDown = true;
                    resizeOldSize = new Vector2(outerCoords.width,outerCoords.height);
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
            // see drags extending outside the curent window, while the Input style
            // will.  That's why this looks different from the others.
            if (Input.GetMouseButton(0))
            {
                if (resizeMouseDown)
                {
                    var mousePos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);
                    Vector2 dragDelta = mousePos - mouseDownPos;
                    outerCoords = new Rect( outerCoords.xMin,
                                            outerCoords.yMin,
                                            Math.Max( resizeOldSize.x + dragDelta.x, 100 ),
                                            Math.Max( resizeOldSize.y + dragDelta.y, 30 )   );
                    CalcInnerCoords();
                    Event.current.Use();
                }
            }
        }
        
        protected void CheckExitClicked()
        {
            Event e = Event.current;
            if (e.type == EventType.mouseUp && e.button == 0)
            {
                // Mouse button went both down and up in the exit box (a click):
                if ( exitCoords.Contains(mouseUpPos) &&
                    exitCoords.Contains(mouseDownPos) )
                {
                    ExitEditor();
                    Event.current.Use(); // Without this it was quadruple-firing the same event.
                }
            }
        }

        protected void CheckSaveClicked()
        {
            Event e = Event.current;
            if (e.type == EventType.mouseUp && e.button == 0)
            {
                // Mouse buton went both down and up in the save box (a click):
                if ( saveCoords.Contains(mouseUpPos) &&
                    saveCoords.Contains(mouseDownPos) )
                {
                    SaveContents();
                    Event.current.Use(); // Without this it was quadruple-firing the same event.
                }
            }
        }
        
        protected void CheckReloadClicked()
        {
            Event e = Event.current;
            if (e.type == EventType.mouseUp && e.button == 0)
            {
                // Mouse button went both down and up in the reload box (a click):
                if ( reloadCoords.Contains(mouseUpPos) &&
                    reloadCoords.Contains(mouseDownPos) )
                {
                    ReloadContents();
                    Event.current.Use(); // Without this it was quadruple-firing the same event.
                }
            }
        }
        
        void ProcessWindow( int windowID )
        {

            CheckKeyboard();
            
            // Some mouse global state data used by several of the checks:
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
                mouseDownPos = new Vector2(e.mousePosition.x, e.mousePosition.y);
            if (e.type == EventType.MouseUp && e.button == 0)
                mouseUpPos = new Vector2(e.mousePosition.x, e.mousePosition.y);

            CheckResizeDrag();
            CheckExitClicked();
            CheckSaveClicked();
            CheckReloadClicked();

            CalcOuterCoords();

            DrawWindow( windowID );
            
        }
        
        protected void DrawWindow( int windowID/*currently unused argument*/ )
        {
            GUI.contentColor = Color.green;


            GUILayout.BeginArea( innerCoords );
            scrollPosition = GUILayout.BeginScrollView( scrollPosition );
            int preLength = contents.Length;
            contents = GUILayout.TextArea( contents );
            int postLength = contents.Length;
            GUILayout.EndScrollView();            
            GUILayout.EndArea();
            
            GUI.Label( labelCoords, BuildTitle() );
            GUI.Box( exitCoords, EXIT_BUTTON_TEXT );
            GUI.Box( saveCoords, SAVE_BUTTON_TEXT );
            GUI.Box( reloadCoords, RELOAD_BUTTON_TEXT );
            KeepCursorScrolledInView();            

            GUI.Box( resizeButtonCoords, resizeImage );
            
            if (preLength != postLength)
            {
                isDirty = true;
            }
        }

        protected void CalcOuterCoords()
        {
            if (isOpen && term != null)
            {
                Rect tRect = term.GetRect();
                
                // Glue it to the bottom of the attached term window - move wherever it moves:
                float left = tRect.xMin;
                float top = tRect.yMin + tRect.height;
                
                // If it hasn't been given a size yet, then give it a starting size that matches
                // the attached terminal window size.  Otherwise keep whatever size the user changed it to:
                if (outerCoords.width == 0)
                    outerCoords = new Rect( left, top, tRect.width, tRect.height );
                else
                    outerCoords = new Rect( left, top, outerCoords.width, outerCoords.height );
            }
        }

        protected void CalcInnerCoords()
        {
            if (!isOpen) return;

            innerCoords = new Rect( FRAME_THICKNESS,
                                    FRAME_THICKNESS + 1.5f*FONT_HEIGHT,
                                    outerCoords.width - 2*FRAME_THICKNESS,
                                    outerCoords.height - 2*FRAME_THICKNESS -2*FONT_HEIGHT );

            Vector2 labSize  = GUI.skin.label.CalcSize( new GUIContent(BuildTitle()) );
            Vector2 exitSize = GUI.skin.box.CalcSize(   new GUIContent(EXIT_BUTTON_TEXT) );
            Vector2 saveSize = GUI.skin.box.CalcSize(   new GUIContent(SAVE_BUTTON_TEXT) );
            Vector2 reloadSize = GUI.skin.box.CalcSize( new GUIContent(RELOAD_BUTTON_TEXT) );
                
            labelCoords = new Rect( 5, 1, labSize.x, labSize.y);
                
            float buttonXCounter = outerCoords.width; // Keep track of the x coord of leftmost button so far.

            buttonXCounter -= (exitSize.x + 5);
            exitCoords = new Rect( buttonXCounter, 1, exitSize.x, exitSize.y );
                
            buttonXCounter -= (saveSize.x + 2);
            saveCoords = new Rect( buttonXCounter, 1, saveSize.x, saveSize.y );
                
            buttonXCounter -= (reloadSize.x + 2);
            reloadCoords = new Rect( buttonXCounter, 1, reloadSize.x, reloadSize.y );

            resizeButtonCoords = new Rect( outerCoords.width - resizeImage.width,
                                           outerCoords.height - resizeImage.height,
                                           resizeImage.width,
                                           resizeImage.height );
        }
        
        protected void KeepCursorScrolledInView()
        {
            // It's utterly ridiculous that Unity's TextArea widget doesn't
            // just do this automatically.  It's basic behavior for a scrolling
            // text widget that when the text cursor moves out of the viewport you
            // scroll to keep it in view.  Oh well, have to do it manually:
            //
            // NOTE: This method is what is interferring with the scrollbar's ability
            // to scroll with the mouse - this routine is locking the scrollbar
            // to only be allowed to move as far as the cursor is still in view.
            // Fixing that would take a bit of work.
            //
            
            UnityEngine.TextEditor editor = GetWidgetController();
            Vector2 pos = editor.graphicalCursorPos;
            float usableHeight = innerCoords.height - 2.5f*FONT_HEIGHT;
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
        
        public Rect GetRect()
        {
            return outerCoords;
        }
        
        protected string BuildTitle()
        {
            if (volume.Name.Length > 0)                
                return fileName + " on " + volume.Name;
            return fileName + " on local volume";  // Don't know which number because no link to VolumeManager from this class.
        }
    }
}
