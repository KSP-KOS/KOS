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
        private Vector2 _mouseDownPos; // Position the mouse was at when button went down - for measuring drags.
        private Vector2 _resizeOldSize; // width and height it had when the mouse button went down on the resize button.
        private bool _isDirty = false; // have any edits occured since last load or save?
        private bool _frozen = false;
        private DelegateDialog _dialog = null;
        
        
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
                    GUI.Window( _idCount, _outerCoords, DrawWindow, "" );
            }
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
        
        static void DelegateSaveExit( KOSTextEditPopup me )
        {
            me.SaveContents();
            me.Freeze(false);
            me.Close();
        }

        static void DelegateNoSaveExit( KOSTextEditPopup me )
        {
            me.Freeze(false);
            me.Close();
        }
        
        static void DelegateSaveThenLoad( KOSTextEditPopup me )
        {
            me.SaveContents();
            DelegateLoadContents( me );
        }

        static void DelegateLoadContents( KOSTextEditPopup me )
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

        static void DelegateCancel( KOSTextEditPopup me )
        {
            // do nothing.
        }
        
        void DrawWindow( int windowID )
        {
            if (Event.current.type == EventType.KeyDown)
            {
                char c = Event.current.character;
                if (System.Char.IsSymbol(c) ||
                    System.Char.IsLetterOrDigit(c) ||
                    System.Char.IsPunctuation(c) ||
                    System.Char.IsSeparator(c) )
                {
                    _isDirty = true;
                }
            }
            
            if (Input.GetMouseButtonDown(0)) // mouse button went from Up to Down just now.
            {
                _mouseDownPos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);

                // Rememeber the fact that this mouseDown started on the resize button:
                if (_resizeButtonCoords.Contains(_mouseDownPos))
                {
                    _resizeMouseDown = true;
                    _resizeOldSize = new Vector2(_outerCoords.width,_outerCoords.height);
                }
            }
            
            if (Input.GetMouseButtonUp(0)) // mouse button went from Down to Up just now.
            {
                Vector2 mouseUpPos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);

                _resizeMouseDown = false;


                // Mouse buton went both down and up in the save box (a click):
                if ( _saveCoords.Contains(mouseUpPos) &&
                    _saveCoords.Contains(_mouseDownPos) )
                {
                    SaveContents();
                    Event.current.Use(); // Without this it was quadruple-firing the same event.
                    _term.Print("[Saved changes to " + _fName + "]");
                }
                // Mouse button went both down and up in the exit box (a click):
                else if ( _exitCoords.Contains(mouseUpPos) &&
                         _exitCoords.Contains(_mouseDownPos) )
                {
                    if (_isDirty)
                        InvokeDirtySaveExitDialog();
                    else
                        Close();
                    
                    Event.current.Use(); // Without this it was quadruple-firing the same event.
                }
                // Mouse button went both down and up in the reload box (a click):
                else if ( _reloadCoords.Contains(mouseUpPos) &&
                         _reloadCoords.Contains(_mouseDownPos) )
                {
                    if (_isDirty)
                        InvokeReloadConfirmDialog();
                    else
                        Close();
                    
                    Event.current.Use(); // Without this it was quadruple-firing the same event.
                }
                
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

            CalcOuterCoords();

            // These styles don't seem to be having any effect at the moment:
            GUIStyle editStyle = new GUIStyle( GUI.skin.textArea );
            editStyle.fontSize = _fontHeight;
            
            GUI.contentColor = Color.green;
            
            // Must BOTH pass contents in and assign return val to contents or else Unity
            // makes the widget un-editable.  It doesn't edit the contents string in-place:
            _contents = GUI.TextArea( _innerCoords, _contents, editStyle );
            
            GUI.Label( _labelCoords, _fName+" on Volume " + _vol.Name );
            GUI.Box( _reloadCoords, "Reload" );
            GUI.Box( _exitCoords, "Exit" );
            GUI.Box( _saveCoords, "Save" );
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
                Vector2 saveSize = GUI.skin.box.CalcSize(   new GUIContent("Save") );
                Vector2 exitSize = GUI.skin.box.CalcSize(   new GUIContent("Exit") );
                Vector2 reloadSize = GUI.skin.box.CalcSize( new GUIContent("Reload") );
                
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
