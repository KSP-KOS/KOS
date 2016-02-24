using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Utilities;
using UnityEngine;
using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using System.Collections.Generic;

namespace kOS.Suffixed
{
    public class VectorRenderer : Structure, IUpdateObserver, IKOSScopeObserver
    {
        public Vector3d      Vector { get; set; }
        public RgbaColor     Color { get; set; }
        public Vector3d      Start { get; set; }
        public double        Scale { get; set; }
        public double        Width { get; set; }

        private LineRenderer  line;
        private LineRenderer  hat;
        private bool          enable;
        private readonly UpdateHandler updateHandler;
        private readonly SharedObjects shared;
        private GameObject    lineObj;
        private GameObject    hatObj;
        private GameObject    labelObj;
        private GUIText       label;
        private string        labelStr = "";
        private Vector3       labelLocation;

        // These could probably be moved somewhere where they are updated
        // more globally just once per Update() rather than once per
        // VecterRenderer object per Update().  In future if we start
        // allowing more types of drawing primitives like this, then
        // it might be worth the work to move these, and their associated
        // updater methods, to a new class with one global instance for the whole
        // mod.  Until then it's not that much of an extra cost:
        private Vector3       shipCenterCoords;
        private Vector3       camPos;         // camera coordinates.
        private Vector3       camLookVec;     // vector from camera to ship position.
        private Vector3       prevCamLookVec;
        private Quaternion    camRot;
        private Quaternion    prevCamRot;
        private bool          isOnMap; // true = Map view, false = Flight view.
        private bool          prevIsOnMap;
        private const int     MAP_LAYER = 10; // found through trial-and-error
        private const int     FLIGHT_LAYER = 15; // Supposedly the layer for UI effects in flight camera.

        public VectorRenderer( UpdateHandler updateHand, SharedObjects shared )
        {
            Vector  = new Vector3d(0,0,0);
            Color   = new RgbaColor(1,1,1);
            Start   = new Vector3d(0,0,0);
            Scale   = 1.0;
            Width   = 0;
            
            updateHandler = updateHand;
            this.shared = shared;
            InitializeSuffixes();
        }

        // Implementation of KOSSCopeObserver interface:
        // ---------------------------------------------
        public int LinkCount { get; set; }
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

        /// <summary>Make all vector renderers invisible everywhere in the kOS module.</summary>
        static public void ClearAll(UpdateHandler handler)
        {
            // Take a copy of the list because the items will be deleted from the update handler
            // as SetShow() gets called, and .NET won't let you iterate over the collection
            // directly while you do that:
            List<VectorRenderer> allOfMe = new List<VectorRenderer>();
            foreach(VectorRenderer item in handler.GetAllUpdatersOfType(typeof(VectorRenderer)))
                allOfMe.Add(item);

            // Now actually turn them all off:
            foreach(VectorRenderer vecRend in allOfMe)
                vecRend.SetShow(false);
        }
        
        /// <summary>
        /// Move the origin point of the vector drawings to move with the
        /// current ship, whichever ship that happens to be at the moment,
        /// and move to wherever that ship is within its local XYZ world (which
        /// isn't always at (0,0,0), as it turns out.):
        /// </summary>
        public void KOSUpdate( double deltaTime )
        {
            if (line == null || hat == null) return;
            if (!enable) return;

            GetCamData();
            GetShipCenterCoords();
            PutAtShipRelativeCoords();

            SetLayer(isOnMap ? MAP_LAYER : FLIGHT_LAYER);

            var mapChange = isOnMap != prevIsOnMap;
            var magnitudeChange = prevCamLookVec.magnitude != camLookVec.magnitude; 
            if ( mapChange || magnitudeChange )
            {
                RenderPointCoords();
                LabelPlacement();
            }
            else if (prevCamRot != camRot)
            {
                LabelPlacement();
            }
        }

        private void InitializeSuffixes()
        {
            AddSuffix(new[]{"VEC", "VECTOR"}, new SetSuffix<Vector>(() => new Vector(Vector), value =>
            {
                Vector = value;
                RenderPointCoords();
            }));
            AddSuffix(new[]{"COLOR", "COLOUR"}, new SetSuffix<RgbaColor>(() => Color, value =>
            {
                Color = value;
                RenderColor();
            }));
            AddSuffix("SHOW", new SetSuffix<bool>(() => enable, SetShow));
            AddSuffix("START", new SetSuffix<Vector>(() => new Vector(Start), value =>
            {
                Start = value;
                RenderPointCoords();
            }));
            AddSuffix("SCALE", new SetSuffix<double>(() => Scale, value =>
            {
                Scale = value;
                RenderPointCoords();
            }));

            AddSuffix("LABEL", new SetSuffix<string>(() => labelStr,SetLabel));
            AddSuffix("WIDTH", new SetSuffix<double>(() => Width, value =>
            {
                Width = value;
                RenderPointCoords();
            }));
        }

        /// <summary>
        /// Update _shipCenterCoords, abstracting the different ways to do
        /// it depending on view mode:
        /// </summary>
        private void GetShipCenterCoords()
        {
            if (isOnMap)
                shipCenterCoords = ScaledSpace.LocalToScaledSpace(
                     shared.Vessel.findWorldCenterOfMass() );
            else
                shipCenterCoords = shared.Vessel.findWorldCenterOfMass();
        }
        
        /// <summary>
        /// Update camera data, abstracting the different ways KSP does it
        /// depending on view mode:
        /// </summary>
        private void GetCamData()
        {

            prevIsOnMap    = isOnMap;
            prevCamLookVec = camLookVec;
            prevCamRot     = camRot;
            
            isOnMap = MapView.MapIsEnabled;

            var cam = Utils.GetCurrentCamera();
            camPos = cam.transform.localPosition;

            // the Distance coming from MapView.MapCamera.Distance
            // doesn't seem to work - calculating it myself below:
            // _camdist = pc.Distance();
            // camRot = cam.GetCameraTransform().rotation;
            camRot = cam.transform.rotation;

            camLookVec = camPos - shipCenterCoords;
        }
        
        /// <summary>
        /// Get the position in screen coordinates that the 3d world coordinates
        /// project onto, abstracting the two different ways KSP has to access
        /// the camera depending on view mode.
        /// Returned coords are in a system where the screen viewport goes from
        /// (0,0) to (1,1) and the Z coord is how far from the screen it is
        /// (-Z means behind you).
        /// </summary>
        private Vector3 GetViewportPosFor( Vector3 v )
        {
            var cam = Utils.GetCurrentCamera();
            return cam.WorldToViewportPoint( v );
        }

        /// <summary>
        /// Position the origins of the objects that make up the arrow
        /// such that they anchor relative to current ship position.
        /// </summary>
        private void PutAtShipRelativeCoords()
        {
            line.transform.localPosition  = shipCenterCoords;
            hat.transform.localPosition   = shipCenterCoords;
        }
        
        public bool GetShow()
        {
            return enable;
        }
        
        public void SetShow( bool newShowVal )
        {
            if (newShowVal)
            {
                if (line == null || hat == null )
                {
                    lineObj     = new GameObject("vecdrawLine");
                    hatObj      = new GameObject("vecdrawHat");
                    labelObj    = new GameObject("vecdrawLabel", typeof(GUIText) );

                    line  = lineObj.AddComponent<LineRenderer>();
                    hat   = hatObj.AddComponent<LineRenderer>();
                    label = labelObj.guiText;

                    line.useWorldSpace      = false;
                    hat.useWorldSpace       = false;

                    GetShipCenterCoords();

                    line.material           = new Material(Shader.Find("Particles/Additive"));
                    hat.material            = new Material(Shader.Find("Particles/Additive"));

                    // This is how font loading would work if other fonts were available in KSP:
                    // Font lblFont = (Font)Resources.Load( "Arial", typeof(Font) );
                    // SafeHouse.Logger.Log( "lblFont is " + (lblFont == null ? "null" : "not null") );
                    // _label.font = lblFont;

                    label.text = labelStr;
                    label.anchor = TextAnchor.MiddleCenter;

                    PutAtShipRelativeCoords();
                    RenderValues();
                }
                updateHandler.AddObserver( this );
                line.enabled  = true;
                hat.enabled   = true;
                label.enabled = true;
            }
            else
            {
                updateHandler.RemoveObserver( this );
                if (label != null)
                {
                    label.enabled = false;
                    label = null;
                }
                if (hat != null)
                {
                    hat.enabled   = false;
                    hat = null;
                }
                if (line != null)
                {
                    line.enabled  = false;
                    line = null;
                }
                labelObj = null;
                hatObj   = null;
                lineObj  = null;
            }

            enable = newShowVal;
        }

        public void SetLayer( int newVal )
        {
            if (lineObj  != null) lineObj.layer  = newVal;
            if (hatObj   != null) hatObj.layer   = newVal;
            if (labelObj != null) labelObj.layer = newVal;
        }
        
        public void SetLabel( string newVal )
        {
            labelStr = newVal;
            if (label != null) label.text = labelStr;
            RenderPointCoords();
        }
        
        public void RenderValues()
        {
            RenderPointCoords();
            RenderColor();
            GetCamData();
            LabelPlacement();
        }

        /// <summary>
        /// Assign the arrow and label's positions in space.  Call
        /// whenever :VEC, :START, or :SCALE change, or when the
        /// game switches between flight view and map view, as they
        /// don't use the same size scale.
        /// </summary>
        public void RenderPointCoords()
        {

            if (line != null && hat != null)
            {
                double mapLengthMult = 1.0; // for scaling when on map view.
                double mapWidthMult  = 1.0; // for scaling when on map view.
                float useWidth;

                if (isOnMap)
                {
                    mapLengthMult = ScaledSpace.InverseScaleFactor;
                    mapWidthMult = Math.Max( camLookVec.magnitude, 100.0f ) / 100.0f;
                }
                
                Vector3d point1 = mapLengthMult * Start;
                Vector3d point2 = mapLengthMult * (Start + (Scale * 0.95 * Vector));
                Vector3d point3 = mapLengthMult * (Start + (Scale * Vector));
                
                label.fontSize = (int) (12.0 * (Width/0.2) * Scale);

                useWidth = (float) (Width * Scale * mapWidthMult);


                // Position the arrow line:
                line.SetVertexCount( 2 );
                line.SetWidth( useWidth , useWidth );
                line.SetPosition( 0, point1 );
                line.SetPosition( 1, point2 );

                // Position the arrow hat:
                hat.SetVertexCount( 2 );
                hat.SetWidth( useWidth * 3.5f, 0.0F );
                hat.SetPosition( 0, point2 );
                hat.SetPosition( 1, point3 );

                // Put the label at the midpoint of the arrow:
                labelLocation = (point1 + point3) / 2;

                PutAtShipRelativeCoords();

            }
        }

        /// <summary>
        /// Calculates colors and applies transparency fade effect.
        /// Only needs to be called when the color changes.
        /// </summary>
        public void RenderColor()
        {

            Color c1 = Color.Color;
            Color c2 = Color.Color;
            c1.a = c1.a * (float)0.25;
            Color lCol = UnityEngine.Color.Lerp( c2, UnityEngine.Color.white, 0.7f ); // "whiten" the label color a lot.

            if (line != null && hat != null)
            {
                line.SetColors( c1, c2 ); // The line has the fade effect
                hat.SetColors( c2, c2 );  // The hat does not have the fade effect.
                label.color = lCol;     // The label does not have the fade effect.
            }
        }
        
        /// <summary>
        /// Place the 2D label at the correct projected spot on
        /// the screen from its location in 3D space:
        /// </summary>
        private void LabelPlacement()
        {
            
            Vector3 screenPos = GetViewportPosFor( shipCenterCoords + labelLocation );
            
            // If the projected location is on-screen:
            if ( screenPos.z > 0
                 && screenPos.x >= 0 && screenPos.x <= 1
                 && screenPos.y >= 0 && screenPos.y <= 1 )
            {
                label.enabled = true;
                label.transform.position = screenPos;
            }
            else
            {
                label.enabled = false;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} VectorRenderer", base.ToString());
        }

        public void Dispose()
        {
           updateHandler.RemoveObserver(this);
        }
    }
}
