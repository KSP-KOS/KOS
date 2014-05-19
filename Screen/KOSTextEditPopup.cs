using System;
using UnityEngine;
using kOS;
using kOS.Persistence;

namespace kOS.Screen
{
    /// <summary>
    /// Description of Class1.
    /// </summary>
    public class KOSTextEditPopup : MonoBehaviour
    {
        protected Rect _outerCoords;
        protected Rect _innerCoords;
        protected Rect _saveCoords;
        protected Rect _exitCoords;
        protected Rect _labelCoords;
        protected Rect _resizeButtonCoords;
        protected TermWindow _term = null; // The terminal that this popup is attached to.
        protected string _fName = "";
        protected string _contents = "";
        protected Volume _vol;
        static protected int _idCount = 100;
        static protected int _frameThickness = 8;
        static protected int _fontHeight = 12;
        protected bool _isOpen = false;
        private Texture2D _resizeImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private bool _resizeMouseDown = false;
        private Vector2 _mouseDownPos; // Position the mouse was at when button went down - for measuring drags.
        private Vector2 _resizeOldSize; // width and height it had when the mouse button went down on the resize button.

        
        public void AttachTo( TermWindow term, Volume v, string fName = "", string initStr = "" )
        {
            _idCount++;
            _term = term;
            _fName = fName;
            _vol = v;
            _contents = initStr;
            _isOpen = true;
            _outerCoords = new Rect(0,0,0,0); // will be resized and moved in onGUI.
        }
        
        public void Awake()
        {
            var urlGetter = new WWW( "file://" +
                                     KSPUtil.ApplicationRootPath.Replace("\\", "/") +
                                     "GameData/kOS/GFX/resize-button.png" );
            urlGetter.LoadImageIntoTexture( _resizeImage );
        }
        
        public void Open()
        {
            _isOpen = true; // This probably never gets called because this object is thrown away on closing.
        }
        
        public void Close()
        {
            _isOpen = false;
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
            CalcOuterCoords();
            CalcInnerCoords();

            // Unity wants to constantly see the Window constructor run again on each
            // onGUI call, or else it goes away:
            if (_isOpen)
                GUI.Window( _idCount, _outerCoords, DrawWindow, "" );
        }
        
        public void SaveContents()
        {
            ProgramFile file = new ProgramFile(_fName);
            file.Content = _contents;
            
            if (! _vol.SaveFile(file) )
            {
                throw new Exception("[File Save failed]");
            }
        }
        
        void DrawWindow( int wID )
        {
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
                    Close();
                    Event.current.Use(); // Without this it was quadruple-firing the same event.
                    _term.Print("[Exited editor.]");
                }
                
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
                    Event.current.Use();
                    CalcInnerCoords();
                }
            }

            // These styles don't seem to be having any effect at the moment:
            GUIStyle editStyle = new GUIStyle( GUI.skin.textArea );
            editStyle.fontSize = _fontHeight;
            editStyle.normal.textColor   = Color.grey;
            editStyle.onActive.textColor = Color.green;

            // Must BOTH pass contents in and assign return val to contents or else Unity
            // makes the widget un-editable.  It doesn't edit the contents string in-place:
            _contents = GUI.TextArea( _innerCoords, _contents, editStyle );

            Vector2 labSize  = GUI.skin.label.CalcSize( new GUIContent(_fName) );
            Vector2 saveSize = GUI.skin.box.CalcSize(   new GUIContent("Save") );
            Vector2 exitSize = GUI.skin.box.CalcSize(   new GUIContent("Exit") );
            
            GUI.Label( _labelCoords, _fName );
            GUI.Box( _exitCoords, "Exit" );
            GUI.Box( _saveCoords, "Save" );
            GUI.Box( _resizeButtonCoords, _resizeImage );
            
            CalcOuterCoords(); // freeze it to be glued to the terminal window.
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
                
                _labelCoords = new Rect( 5, 1, labSize.x, labSize.y);
                
                _exitCoords = new Rect( _outerCoords.width - exitSize.x - 1,
                                       1,
                                       exitSize.x,
                                       exitSize.y );
                
                _saveCoords = new Rect( _outerCoords.width - exitSize.x - saveSize.x - 2,
                                       1,
                                       saveSize.x,
                                       saveSize.y );
                
                _resizeButtonCoords = new Rect( _outerCoords.width - _resizeImage.width,
                                                _outerCoords.height - _resizeImage.height,
                                                _resizeImage.width,
                                                _resizeImage.height );
            }
        }
        
    }
}
