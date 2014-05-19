using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Suffixed;
using kOS.Execution;

namespace kOS.Utilities
{
    public class VectorRenderer : SpecialValue, IUpdateObserver, KOSScopeObserver
    {
        public Vector3d      vec { get; set; }
        public RgbaColor     rgba { get; set; }
        public Vector3d      start { get; set; }
        public double        scale { get; set; }
        public double        width { get; set; }

        private LineRenderer  _line = null;
        private LineRenderer  _hat = null;
        private bool          _enable = false;
        private UpdateHandler _uHandler = null;
        private GameObject    _lineObj = null;
        private GameObject    _hatObj  = null;
        private GameObject    _labelObj  = null;
        private GUIText       _label = null;
        private string        _labelStr = "";
        private Vector3       _labelLocation;

        // These could probably be moved somewhere where they are updated
        // more globally just once per Update() rather than once per
        // VecterRenderer object per Update().  In future if we start
        // allowing more types of drawing primitives like this, then
        // it might be worth the work to move these, and their associated
        // updater methods, to a new class with one global instance for the whole
        // mod.  Until then it's not that much of an extra cost:
        private Vector3       _shipCenterCoords;
        private Vector3       _camPos;         // camera coordinates.
        private Vector3       _prevCamPos;
        private Vector3       _camLookVec;     // vector from camera to ship positon.
        private Vector3       _prevCamLookVec;
        private Quaternion    _camRot = new Quaternion();
        private Quaternion    _prevCamRot = new Quaternion();
        private bool          _isOnMap = false; // true = Map view, false = Flight view.
        private bool          _prevIsOnMap = false;
        private int           _mapLayer = 10;   // found through trial-and-error
        private int           _flightLayer = 0; // found through trial-and-error
        
        public VectorRenderer( UpdateHandler uHand )
        {
            vec     = new Vector3d(0,0,0);
            rgba    = new RgbaColor(1,1,1,1);
            start   = new Vector3d(0,0,0);
            scale   = 1.0;
            width   = 0;
            
            _uHandler    = uHand;
        }

        // Implementation of KOSSCopeObserver interface:
        // ---------------------------------------------
        public int linkCount { get; set; }
        public void ScopeLost()
        {
            // When no kos script variables can still access me,
            // tell Unity to make me disappear, and also
            // tell UpdateHandler to take me out of its list
            // (Note that if I didn't do this,
            // then as far as C# thinks, I wouldn't be orphaned because
            // UpdateHandler is holding a reference to me.)
            SetShow(false);
        }

        
        public void Update( double deltaTime )
        {
            /// <summary>
            /// Move the origin point of the vector drawings to move with the
            /// current ship, whichever ship that happens to be at the moment,
            /// and move to wherever that ship is within its local XYZ world (which
            /// isn't always at (0,0,0), as it turns out.):
            /// </summary>
            

            if (_line != null && _hat != null)
            {
                if (_enable)
                {
                    GetCamData();
                    GetShipCenterCoords();
                    PutAtShipRelativeCoords();
                    
                    if (_isOnMap)
                        SetLayer( _mapLayer );
                    else
                        SetLayer( _flightLayer );
                    
                    if ( _isOnMap != _prevIsOnMap || _prevCamLookVec.magnitude != _camLookVec.magnitude )
                    {
                        RenderPointCoords();
                        LabelPlacement();
                    }
                    else if (_prevCamRot != _camRot)
                    {
                        LabelPlacement();
                    }
                }
            }
        }
        
        private void GetShipCenterCoords()
        {
            /// <summary>
            /// Update _shipCenterCoords, abstracting the different ways to do
            /// it depending on view mode:
            /// </summary>
            if (_isOnMap)
                _shipCenterCoords = ScaledSpace.LocalToScaledSpace(
                    FlightGlobals.ActiveVessel.GetWorldPos3D() );
            else
                _shipCenterCoords = FlightGlobals.ActiveVessel.findWorldCenterOfMass();
        }
        
        private void GetCamData()
        {
            /// <summary>
            /// Update camera data, abstracting the different ways KSP does it
            /// depending on view mode:
            /// </summary>

            _prevIsOnMap    = _isOnMap;
            _prevCamLookVec = _camLookVec;
            _prevCamPos     = _camPos;
            _prevCamRot     = _camRot;
            
            _isOnMap = MapView.MapIsEnabled;

            if (_isOnMap)
            {
                PlanetariumCamera pc = MapView.MapCamera;
                _camPos = pc.transform.localPosition;
                // the Distance coming from from MapView.MapCamera.Distance
                // doesn't seem to work - calculating it myself below:
                // _camdist = pc.Distance();
                _camRot = MapView.MapCamera.GetCameraTransform().rotation;
            }
            else
            {
                FlightCamera fc = FlightCamera.fetch;
                _camPos = fc.transform.localPosition;
                // the Distance coming from from FlightCamera.Distance
                // doesn't seem to work - calculating it myself below:
                // _camdist = fc.Distance();
                _camRot = FlightCamera.fetch.GetCameraTransform().rotation;
            }
            _camLookVec = _camPos - _shipCenterCoords;
        }
        
        private Vector3 GetViewportPosFor( Vector3 v )
        {
            /// <summary>
            /// Get the position in screen coordinates that the 3d world coordinates
            /// project onto, abstracting the two different ways KSP has to access
            /// the camera depending on view mode.
            /// Returned coords are in a system where the screen viewport goes from
            /// (0,0) to (1,1) and the Z coord is how far from the screen it is
            /// (-Z means behind you).
            /// </summary>
            Camera cam;
            if (_isOnMap)
                cam = MapView.MapCamera.camera;
            else
                cam = FlightCamera.fetch.mainCamera;
            return cam.WorldToViewportPoint( v );
        }
        
        private void PutAtShipRelativeCoords()
        {
            /// <summary>
            /// Position the origins of the objects that make up the arrow
            /// such that they anchor relatove to current ship position.
            /// </summary>
            _line.transform.localPosition  = _shipCenterCoords;
            _hat.transform.localPosition   = _shipCenterCoords;
        }
        
        public bool GetShow()
        {
            return _enable;
        }
        
        public void SetShow( bool newShowVal )
        {
            if (newShowVal)
            {
                if (_line == null || _hat == null )
                {
                    _lineObj     = new GameObject("vecdrawLine");
                    _hatObj      = new GameObject("vecdrawHat");
                    _labelObj    = new GameObject("vecdrawLabel", typeof(GUIText) );

                    _line  = _lineObj.AddComponent<LineRenderer>();
                    _hat   = _hatObj.AddComponent<LineRenderer>();
                    _label = _labelObj.guiText;

                    _line.useWorldSpace      = false;
                    _hat.useWorldSpace       = false;

                    GetShipCenterCoords();

                    _line.material           = new Material(Shader.Find("Particles/Additive"));
                    _hat.material            = new Material(Shader.Find("Particles/Additive"));

                    // This is how font loading would work if other fonts were available in KSP:
                    // Font lblFont = (Font)Resources.Load( "Arial", typeof(Font) );
                    // Debug.Log( "lblFont is " + (lblFont == null ? "null" : "not null") );
                    // _label.font = lblFont;
                    
                    _label.fontSize = 12;
                    _label.text = _labelStr;
                    _label.anchor = TextAnchor.MiddleCenter;

                    PutAtShipRelativeCoords();
                    RenderValues();
                }
                _uHandler.AddObserver( this );
                _line.enabled  = true;
                _hat.enabled   = true;
                _label.enabled = true;
            }
            else
            {
                _uHandler.RemoveObserver( this );
                if (_label != null)
                {
                    _label.enabled = false;
                    _label = null;
                }
                if (_hat != null)
                {
                    _hat.enabled   = false;
                    _hat = null;
                }
                if (_line != null)
                {
                    _line.enabled  = false;
                    _line = null;
                }
                _labelObj = null;
                _hatObj   = null;
                _lineObj  = null;
            }

            _enable = newShowVal;
        }
        
        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "VEC":
                case "VECTOR":
                    return  new Vector(vec);
                case "SHOW":
                    return _enable;
                case "COLOR":
                case "COLOUR":
                    return rgba ;
                case "START":
                    return  new Vector(start);
                case "SCALE":
                    return scale ;
                case "LABEL":
                    return _labelStr;
                case "WIDTH":
                    return width;
            }

            return base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            double dblValue = 0.0;
            bool boolValue = false;
            string strValue = "";
            RgbaColor rgbaValue = new RgbaColor(1,1,1,1);
            Vector vectorValue = new Vector(0,0,0);
            
            // When the wrong type of value is given, attempt
            // to make at least SOME value out of it that won't crash
            // the system.  This was added because now the value can
            // be a wide variety of different types and this check
            // used to deny any of them being usable other than doubles.
            // This is getting a bit silly looking and something else
            // needs to be done, I think.
            if (value is double || value is int || value is float ||value is Int32 || value is Int64)
            {
                dblValue = Convert.ToDouble(value);
                boolValue = (Convert.ToBoolean(value) ? true : false);
            }
            else if (value is bool)
            {
                boolValue = (bool)value;
                dblValue = (double) ( ((bool)value) ? 1.0 : 0.0 );
            }
            else if (value is RgbaColor)
            {
                rgbaValue = (RgbaColor) value;
            }
            else if (value is Vector)
            {
                vectorValue = (Vector) value;
            }
            else if (value is String)
            {
                strValue = value.ToString() ;
            }
            else if (!double.TryParse(value.ToString(), out dblValue))
            {
                return false;
            }

            switch (suffixName)
            {
                case "VEC":
                case "VECTOR":
                    vec = vectorValue.ToVector3D();
                    RenderPointCoords();
                    return true;
                case "SHOW":
                    SetShow( boolValue );
                    return true;
                case "COLOR":
                case "COLOUR":
                    rgba = rgbaValue;
                    RenderColor();
                    return true;
                case "START":
                    start = vectorValue.ToVector3D();
                    RenderPointCoords();
                    return true;
                case "SCALE":
                    scale = dblValue;
                    RenderPointCoords();
                    return true;
                case "LABEL":
                    SetLabel( strValue );
                    return true;
                case "WIDTH":
                    width = dblValue;
                    RenderPointCoords();
                    return true;
            }

            return base.SetSuffix(suffixName, value);
        }
        public void SetLayer( int newVal )
        {
            if (_lineObj  != null) _lineObj.layer  = newVal;
            if (_hatObj   != null) _hatObj.layer   = newVal;
            if (_labelObj != null) _labelObj.layer = newVal;
        }
        
        public void SetLabel( string newVal )
        {
            _labelStr = newVal;
            if (_label != null) _label.text = _labelStr;
        }
        
        public void RenderValues()
        {
            RenderPointCoords();
            RenderColor();
            GetCamData();
            LabelPlacement();
        }

        public void RenderPointCoords()
        {
            /// <summary>
            /// Assign the arrow and label's positions in space.  Call
            /// whenever :VEC, :START, or :SCALE change, or when the
            /// game switches between flight view and map view, as they
            /// don't use the same size scale.
            /// </summary>

            if (_line != null && _hat != null)
            {
                double mag = vec.magnitude;
                double mapLengthMult = 1.0; // for scaling when on map view.
                double mapWidthMult  = 1.0; // for scaling when on map view.
                float useWidth = 1.0f;

                if (_isOnMap)
                {
                    mapLengthMult = ScaledSpace.InverseScaleFactor;
                    mapWidthMult = Math.Max( _camLookVec.magnitude, 100.0f ) / 100.0f;
                }
                
                Vector3d point1 = mapLengthMult * scale * start;
                Vector3d point2 = mapLengthMult * scale * (start+0.95*vec);
                Vector3d point3 = mapLengthMult * scale * (start+vec);
                
                if (width <= 0) // User didn't pick a valid width. Use dynamic calculation.
                {
                    useWidth = (float) (0.2*mapWidthMult);
                }
                else // User did pick a width to override the dynamic calculations.
                {
                    useWidth = (float)width;
                }

                // Position the arrow line:
                _line.SetVertexCount( 2 );
                _line.SetWidth( useWidth , useWidth );
                _line.SetPosition( 0, point1 );
                _line.SetPosition( 1, point2 );

                // Position the arrow hat:
                _hat.SetVertexCount( 2 );
                _hat.SetWidth( useWidth * 3.5f, 0.0F );
                _hat.SetPosition( 0, point2 );
                _hat.SetPosition( 1, point3 );

                // Put the label at the midpoint of the arrow:
                _labelLocation = (point1 + point3) / 2;

                PutAtShipRelativeCoords();

            }
        }

        public void RenderColor()
        {
            /// <summary>
            /// Calculates colors and applies transparency fade effect.
            /// Only needs to be called when the color changes.
            /// </summary>

            Color c1 = rgba.color();
            Color c2 = rgba.color();
            c1.a = c1.a * (float)0.25;
            Color lCol = Color.Lerp( c2, Color.white, 0.7f ); // "whiten" the label color a lot.

            if (_line != null && _hat != null)
            {
                _line.SetColors( c1, c2 ); // The line has the fade effect
                _hat.SetColors( c2, c2 );  // The hat does not have the fade effect.
                _label.color = lCol;     // The label does not have the fade effect.
            }
        }
        
        private void LabelPlacement()
        {
            /// <summary>
            /// Place the 2D label at the correct projected spot on
            /// the screen from its location in 3D space:
            /// </summary>
            
            Vector3 screenPos = GetViewportPosFor( _shipCenterCoords + _labelLocation );
            
            // If the projected location is onscreen:
            if ( screenPos.z > 0
                 && screenPos.x >= 0 && screenPos.x <= 1
                 && screenPos.y >= 0 && screenPos.y <= 1 )
            {
                _label.enabled = true;
                _label.transform.position = screenPos;
            }
            else
            {
                _label.enabled = false;
            }
        }

    }
}
