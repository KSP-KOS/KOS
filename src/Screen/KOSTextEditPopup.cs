using System;
using System.Collections.Generic;
using UnityEngine;
using kOS;
using kOS.Persistence;

namespace kOS.Screen
{
    /// <summary>
    /// A Unity window that cotains the text editor for kOS inside it.  It should only be
    /// popped into existence when you feed it a file using the AttachTo call.
    /// </summary>
    public class KOSTextEditPopup : MonoBehaviour
    {
        protected Rect _outerCoords;
        protected Rect _innerCoords;
        protected Rect _saveCoords;
        protected Rect _exitCoords;
        protected Rect _labelCoords;
        protected Rect _reloadCoords;
        protected Rect _resizeButtonCoords;
        protected TermWindow _term = null; // The terminal that this popup is attached to.
        protected string _fName = "";
        protected string _loadingFName = "";
        protected Volume _vol;
        protected Volume _loadingVol;
        protected string _contents = "";
        static protected int _idCount = 100;
        static protected int _frameThickness = 8;
        static protected int _fontHeight = 12;
        protected bool _isOpen = false;
        private Texture2D _resizeImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private bool _resizeMouseDown = false;
        private Vector2 _mouseDownPos; // Position the mouse was at when button went down.
        private Vector2 _mouseUpPos;   // Position the mouse was at when button went up.
        private Vector2 _resizeOldSize; // width and height it had when the mouse button went down on the resize button.
        private bool _isDirty = false; // have any edits occured since last load or save?
        private bool _frozen = false;
        private DelegateDialog _dialog = null;
        private string _exitButtonText = "(E)xit";
        private string _saveButtonText = "(S)ave";
        private string _reloadButtonText = "(R)eload";
        private Vector2 _scrollPosition; // tracks where within the text box it's scrolled to.
        
        public void Freeze(bool newVal)
        {
            _frozen = newVal;
        }
        
        public void Awake()
        {
            GameObject gObj = new GameObject( "texteditConfirm", typeof(DelegateDialog) );
            UnityEngine.Object.DontDestroyOnLoad(gObj);
            _dialog = ((DelegateDialog)gObj.GetComponent(typeof(DelegateDialog)));
            var urlGetter = new WWW( "file://" +
                                    KSPUtil.ApplicationRootPath.Replace("\\", "/") +
                                    "GameData/kOS/GFX/resize-button.png" );
            urlGetter.LoadImageIntoTexture( _resizeImage );
        }

        public void AttachTo( TermWindow term, Volume v, string fName = "" )
        {
            _term = term;
            _outerCoords = new Rect(0,0,0,0); // will be resized and moved in onGUI.
            _frozen = false;
            _loadingVol = v;
            _loadingFName = fName;
            LoadContents(v, fName);
        }
        
        public void Open()
        {
            _isOpen = true;
            _isDirty = false;
            Freeze(false);
        }
        
        public void Close()
        {
            _isOpen = false;
            _isDirty = false;
            Freeze(false);
        }
        
        public void Update()
        {
            // Only stay open as long as the attached terminal window stays open:
            if (_isOpen && _term != null && _term.isOpen && _term.isPowered )
                _isOpen = true;
            else
                _isOpen = false;
        }
        
        public void OnGUI()
        {
            if (_isOpen && !_frozen)
            {
                CalcOuterCoords();
                CalcInnerCoords();

                // Unity wants to constantly see the Window constructor run again on each
                // onGUI call, or else it goes away:
                if (_isOpen)
                    GUI.Window( _idCount, _outerCoords, ProcessWindow, "" );
            }
        }

        protected void ExitEditor()
        {
            if (_isDirty)
                InvokeDirtySaveExitDialog();
            else
                Close();
        }

        
        public void SaveContents()
        {
            ProgramFile file = new ProgramFile(_fName);
            file.Content = _contents;
            
            if (! _vol.SaveFile(file) )
            {
                throw new Exception("[File Save failed]");
            }
            _isDirty = false;
            _term.Print("[Saved changes to " + _fName + "]");
        }
        
        protected void ReloadContents()
        {
            if (_isDirty)
                InvokeReloadConfirmDialog();
            else
                Close();
        }

        public void LoadContents( Volume vol, string fName )
        {
            if (_isDirty)
            {
                Freeze(true);
                InvokeDirtySaveLoadDialog();
                _loadingVol = vol;
                _loadingFName = fName;
            }
            else
            {
                _loadingVol = vol;
                _loadingFName = fName;
                DelegateLoadContents(this);
            }
        }
        
        protected void InvokeDirtySaveExitDialog()
        {
            List<string> choices = new List<string>();
            List<DialogAction> actions = new List<DialogAction>();
            choices.Add( "Yes" );
            actions.Add( DelegateSaveExit );
            choices.Add( "No" );
            actions.Add( DelegateNoSaveExit );
            choices.Add( "Cancel" );
            actions.Add( DelegateCancel );
            
            _dialog.Invoke( this, "\""+_fName+"\" has been edited.  Save it before exiting?", choices, actions );
        }

        protected void InvokeDirtySaveLoadDialog()
        {
            List<string> choices = new List<string>();
            List<DialogAction> actions = new List<DialogAction>();
            choices.Add( "Yes" );
            actions.Add( DelegateSaveThenLoad );
            choices.Add( "No" );
            actions.Add( DelegateLoadContents );
            choices.Add( "Cancel" );
            actions.Add( DelegateCancel );
            
            _dialog.Invoke( this, "\""+_fName+"\" has been edited.  Save before loading \""+_loadingFName+"\"?", choices, actions );
        }

        protected void InvokeReloadConfirmDialog()
        {
            List<string> choices = new List<string>();
            List<DialogAction> actions = new List<DialogAction>();
            choices.Add( "Yes" );
            actions.Add( DelegateLoadContents );
            choices.Add( "No" );
            actions.Add( DelegateCancel );
            
            _dialog.Invoke( this, "\""+_fName+"\" has been edited.  Throw away changes and reload?", choices, actions );
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
            me._vol = me._loadingVol;
            me._fName = me._loadingFName;
            ProgramFile file = me._vol.GetByName( me._fName );
            if ( file == null )
            {
                me._term.Print("[New File]");
                me._contents = "";
            }
            else
            {
                me._contents = file.Content;
            }
            me._isDirty = false;
        }

        protected static void DelegateCancel( KOSTextEditPopup me )
        {
            // do nothing.
        }
        
        protected void CheckKeyboard()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                // If any printed character key was pressed then mark the editor contents dirty,
                // but still allow the keypress to pass through to the TextArea widget unprocessed:
                char ch = Event.current.character;
                
                if (System.Char.IsSymbol(ch) ||
                    System.Char.IsLetterOrDigit(ch) ||
                    System.Char.IsPunctuation(ch) ||
                    System.Char.IsSeparator(ch) )
                {
                    _isDirty = true;
                }

                switch (Event.current.keyCode)
                {
                    case KeyCode.PageUp:
                        DoPageUp();
                        break;
                    case KeyCode.PageDown:
                        DoPageDown();
                        break;
                    case KeyCode.S:
                        if (Event.current.control)
                            SaveContents();
                        break;
                    case KeyCode.E:
                        if (Event.current.control)
                            ExitEditor();
                        break;
                    case KeyCode.R:
                        if (Event.current.control)
                            ReloadContents();
                        break;
                }
            }
        }
        
        protected void DoPageUp()
        {
            // scroll a few pixels less than the whole height:
            _scrollPosition.y -= (_innerCoords.height - 10);
            // If the new position is < 0, Unity will correct that for us.
        }
       
        protected void DoPageDown()
        {
            // scroll a few pixels less than the whole height:
            _scrollPosition.y += (_innerCoords.height - 10);
            // If the new position is > max buffer height, Unity will correct that for us.
        }

        protected void CheckResizeDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Rememeber the fact that this mouseDown started on the resize button:
                if (_resizeButtonCoords.Contains(_mouseDownPos))
                {
                    _resizeMouseDown = true;
                    _resizeOldSize = new Vector2(_outerCoords.width,_outerCoords.height);
                }
            }
            if (Input.GetMouseButtonUp(0)) // mouse button went from Down to Up just now.
            {
                _resizeMouseDown = false;
            }
            if (Input.GetMouseButton(0)) // mouse button is currently held down.
            {
                if (_resizeMouseDown)
                {
                    Vector2 mousePos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);
                    Vector2 dragDelta = mousePos - _mouseDownPos;
                    _outerCoords = new Rect( _outerCoords.xMin,
                                            _outerCoords.yMin,
                                            Math.Max( _resizeOldSize.x + dragDelta.x, 100 ),
                                            Math.Max( _resizeOldSize.y + dragDelta.y, 30 )   );
                    CalcInnerCoords();
                    Event.current.Use();
                }
            }
        }
        
        protected void CheckExitClicked()
        {
            if (Input.GetMouseButtonUp(0))
            {
                // Mouse button went both down and up in the exit box (a click):
                if ( _exitCoords.Contains(_mouseUpPos) &&
                    _exitCoords.Contains(_mouseDownPos) )
                {
                    ExitEditor();
                    Event.current.Use(); // Without this it was quadruple-firing the same event.
                }
            }
        }

        protected void CheckSaveClicked()
        {
            if (Input.GetMouseButtonUp(0))
            {
                // Mouse buton went both down and up in the save box (a click):
                if ( _saveCoords.Contains(_mouseUpPos) &&
                    _saveCoords.Contains(_mouseDownPos) )
                {
                    SaveContents();
                    Event.current.Use(); // Without this it was quadruple-firing the same event.
                }
            }
        }
        
        protected void CheckReloadClicked()
        {
            if (Input.GetMouseButtonUp(0))
            {
                // Mouse button went both down and up in the reload box (a click):
                if ( _reloadCoords.Contains(_mouseUpPos) &&
                    _reloadCoords.Contains(_mouseDownPos) )
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
            if (Input.GetMouseButtonDown(0))
                _mouseDownPos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);
            if (Input.GetMouseButtonUp(0))
                _mouseUpPos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);

            CheckResizeDrag();
            CheckExitClicked();
            CheckSaveClicked();
            CheckReloadClicked();
            
            CalcOuterCoords();
            
            DrawWindow( windowID );
        }
        
        protected void DrawWindow( int windowID/*currently unused argument*/ )
        {
            // These styles don't seem to be having any effect at the moment:
            GUIStyle editStyle = new GUIStyle( GUI.skin.textArea );
            editStyle.fontSize = _fontHeight;
            
            GUI.contentColor = Color.green;

            _scrollPosition = GUI.BeginScrollView( _innerCoords, _scrollPosition, _innerCoords );
            _contents = GUI.TextArea( _contents, editStyle );
            GUI.EndScrollView();            
            
            GUI.Label( _labelCoords, _fName+" on Volume " + _vol.Name );
            GUI.Box( _exitCoords, _exitButtonText );
            GUI.Box( _saveCoords, _saveButtonText );
            GUI.Box( _reloadCoords, _reloadButtonText );
            GUI.Box( _resizeButtonCoords, _resizeImage );
        }

        protected void CalcOuterCoords()
        {
            if ( _isOpen && _term != null)
            {
                Rect tRect = _term.GetRect();
                
                // Glue it to the bottom of the attached term window - move wherever it moves:
                float left = tRect.xMin;
                float top = tRect.yMin + tRect.height;
                
                // If it hasn't been given a size yet, then give it a starting size that matches
                // the attached terminal window size.  Otherwise keep whatever size the user changed it to:
                if (_outerCoords.width == 0)
                    _outerCoords = new Rect( left, top, tRect.width, tRect.height);
                else
                    _outerCoords = new Rect( left, top, _outerCoords.width, _outerCoords.height);
            }
        }

        protected void CalcInnerCoords()
        {
            if (_isOpen)
            {
                _innerCoords = new Rect( _frameThickness,
                                        _frameThickness + _fontHeight,
                                        _outerCoords.width - 2*_frameThickness,
                                        _outerCoords.height - 2*_frameThickness - _fontHeight );

                Vector2 labSize  = GUI.skin.label.CalcSize( new GUIContent(_fName) );
                Vector2 exitSize = GUI.skin.box.CalcSize(   new GUIContent(_exitButtonText) );
                Vector2 saveSize = GUI.skin.box.CalcSize(   new GUIContent(_saveButtonText) );
                Vector2 reloadSize = GUI.skin.box.CalcSize( new GUIContent(_reloadButtonText) );
                
                _labelCoords = new Rect( 5, 1, labSize.x, labSize.y);
                
                float buttonXCounter = _outerCoords.width; // Keep track of the x coord of leftmost button so far.

                buttonXCounter -= (exitSize.x + 5);
                _exitCoords = new Rect( buttonXCounter, 1, exitSize.x, exitSize.y );
                
                buttonXCounter -= (saveSize.x + 2);
                _saveCoords = new Rect( buttonXCounter, 1, saveSize.x, saveSize.y );
                
                buttonXCounter -= (reloadSize.x + 2);
                _reloadCoords = new Rect( buttonXCounter, 1, reloadSize.x, reloadSize.y );

                _resizeButtonCoords = new Rect( _outerCoords.width - _resizeImage.width,
                                               _outerCoords.height - _resizeImage.height,
                                               _resizeImage.width,
                                               _resizeImage.height );
            }
        }
        
        public Rect GetRect()
        {
            return _outerCoords;
        }
        
    }
}
