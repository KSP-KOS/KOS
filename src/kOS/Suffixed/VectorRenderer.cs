using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
using kOS.Safe.Exceptions;
using kOS.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Vecdraw")]
    public class VectorRenderer : Structure, IUpdateObserver, IKOSScopeObserver
    {
        public Vector3d Vector { get; set; }
        public RgbaColor Color { get; set; }
        private RgbaColor prevColor;
        public Vector3d Start { get; set; }
        public double Scale { get; set; }
        public double Width { get; set; }
        public bool Pointy { get; set; }
        public bool Wiping { get; set; }

        private LineRenderer line;
        private LineRenderer hat;
        private bool enable;
        private bool scopeLost = false;
        private readonly UpdateHandler updateHandler;
        private readonly SharedObjects shared;
        private GameObject lineObj;
        private GameObject hatObj;
        private GameObject labelCanvasObj;
        private RectTransform labelTransform;
        private Text label;
        private string labelStr = "";
        private Vector3 labelLocation;

        // These could probably be moved somewhere where they are updated
        // more globally just once per Update() rather than once per
        // VecterRenderer object per Update().  In future if we start
        // allowing more types of drawing primitives like this, then
        // it might be worth the work to move these, and their associated
        // updater methods, to a new class with one global instance for the whole
        // mod.  Until then it's not that much of an extra cost:
        private Vector3 shipCenterCoords;

        private Vector3 camPos;         // camera coordinates.
        private Vector3 camLookVec;     // vector from camera to ship position.
        private Vector3 camLookUp;      // vector upward along camera's top direction.
        private Vector3 prevCamLookVec;
        private Quaternion camRot;
        private Quaternion prevCamRot;
        private bool isOnMap; // true = Map view, false = Flight view.
        private bool prevIsOnMap;
        private const int MAP_LAYER  =10; // found through trial-and-error
        private const int FLIGHT_LAYER = 15; // Supposedly the layer for UI effects in flight camera.
        
        private TriggerInfo StartTrigger = null;
        private TriggerInfo VectorTrigger = null;
        private TriggerInfo ColorTrigger = null;
        private UserDelegate StartDelegate = null;
        private UserDelegate VectorDelegate = null;
        private UserDelegate ColorDelegate = null;

        public VectorRenderer(UpdateHandler updateHand, SharedObjects shared)
        {
            Vector = new Vector3d(0, 0, 0);
            Color = new RgbaColor(1, 1, 1);
            Start = new Vector3d(0, 0, 0);
            Scale = 1.0;
            Width = 0;
            Pointy = false;
            Wiping = false;

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

            // Store the information about lost scope to be checked in the KOSUpdate method
            // because Unity complains if we try to set visual element visibility from
            // outside of the main thread.  That isn't a problem most of the time, but the
            // finalizer method called by the GC (see Variable.cs ~Variable method) is
            // apparently called from another thread, or at least Unity thinks it is.
            scopeLost = true;
        }

        /// <summary>Make all vector renderers invisible everywhere in the kOS module.</summary>
        static public void ClearAll(UpdateHandler handler)
        {
            // Take a copy of the list because the items will be deleted from the update handler
            // as SetShow() gets called, and .NET won't let you iterate over the collection
            // directly while you do that:
            List<VectorRenderer> allOfMe = new List<VectorRenderer>();
            foreach (VectorRenderer item in handler.GetAllUpdatersOfType(typeof(VectorRenderer)))
                allOfMe.Add(item);

            // Now actually turn them all off:
            foreach (VectorRenderer vecRend in allOfMe)
                vecRend.SetShow(false);
        }

        /// <summary>
        /// Move the origin point of the vector drawings to move with the
        /// current ship, whichever ship that happens to be at the moment,
        /// and move to wherever that ship is within its local XYZ world (which
        /// isn't always at (0,0,0), as it turns out.):
        /// </summary>
        public void KOSUpdate(double deltaTime)
        {
            if (line == null || hat == null) return;
            if (scopeLost) // TODO: When collection scope tracking is fixed, we can simply check the link count instead
            {
                SetShow(false);
                scopeLost = false; // Clear the flag just in case something still has a reference to this object
            }
            if (!enable) return;

            HandleDelegateUpdates();

            GetCamData();
            GetShipCenterCoords();
            PutAtShipRelativeCoords();

            SetLayer(isOnMap ? MAP_LAYER : FLIGHT_LAYER);

            var mapChange = isOnMap != prevIsOnMap;
            var magnitudeChange = prevCamLookVec.magnitude != camLookVec.magnitude;
            if (mapChange || magnitudeChange)
            {
                RenderPointCoords();
                LabelPlacement();
            }
            else if (prevCamRot != camRot)
            {
                LabelPlacement();
            }
        }
        
        private void HandleDelegateUpdates()
        {
            // If a UserDelegate went away, throw away any previous trigger handles we may have been waiting to finish:
            // --------------------------------------------------------------------------------------------------------
            if (StartDelegate == null) // Note: if the user assigns the NoDelegate, we re-map that to null (See how the SetSuffixes of this class are set up).
                StartTrigger = null;
            if (VectorDelegate == null) // Note: if the user assigns the NoDelegate, we re-map that to null (See how the SetSuffixes of this class are set up).
                VectorTrigger = null;
            if (ColorDelegate == null) // Note: if the user assigns the NoDelegate, we re-map that to null (See how the SetSuffixes of this class are set up).
                ColorTrigger = null;

            // For any trigger handles for delegate calls that were in progress already, if they're now done
            // then update the value to what they returned:
            // ---------------------------------------------------------------------------------------------
            bool needToRender = false; // track when to call RenderPointCoords
            if (StartTrigger != null && StartTrigger.CallbackFinished)
            {
                if (StartTrigger.ReturnValue is Vector)
                {
                    Start = StartTrigger.ReturnValue as Vector;
                    needToRender = true;
                }
                else
                    throw new KOSInvalidDelegateType("VECDRAW:STARTDELEGATE", "Vector", StartTrigger.ReturnValue.KOSName);
            }
            if (VectorTrigger != null && VectorTrigger.CallbackFinished)
            {
                if (VectorTrigger.ReturnValue is Vector)
                {
                    Vector = VectorTrigger.ReturnValue as Vector;
                    needToRender = true;
                }
                else
                    throw new KOSInvalidDelegateType("VECDRAW:VECTORDELEGATE", "Vector", VectorTrigger.ReturnValue.KOSName);
            }
            if (needToRender)
                RenderPointCoords();  // save a little execution time by only rendering once if both start and vec are updated

            if (ColorTrigger != null && ColorTrigger.CallbackFinished)
            {
                if (ColorTrigger.ReturnValue is RgbaColor)
                {
                    Color = ColorTrigger.ReturnValue as RgbaColor;
                    RenderColor();
                }
                else
                    throw new KOSInvalidDelegateType("VECDRAW:COLORDELEGATE", "Vector", ColorTrigger.ReturnValue.KOSName);
            }

            // For those UserDelegates that have been assigned, if there isn't a current UserDelegate call in progress, start a new one:
            // -------------------------------------------------------------------------------------------------------------------------
            if (StartDelegate != null && (StartTrigger == null || StartTrigger.CallbackFinished))
            {
                StartTrigger = StartDelegate.TriggerOnFutureUpdate(InterruptPriority.Recurring);
                if (StartTrigger == null) // Delegate must be from a stale ProgramContext.  Stop trying to call it.
                    StartDelegate = null;
            }
            if (VectorDelegate != null && (VectorTrigger == null || VectorTrigger.CallbackFinished))
            {
                VectorTrigger = VectorDelegate.TriggerOnFutureUpdate(InterruptPriority.Recurring);
                if (VectorTrigger == null) // Delegate must be from a stale ProgramContext.  Stop trying to call it.
                    VectorDelegate = null;
            }
            if (ColorDelegate != null && (ColorTrigger == null || ColorTrigger.CallbackFinished))
            {
                ColorTrigger = ColorDelegate.TriggerOnFutureUpdate(InterruptPriority.Recurring);
                if (ColorTrigger == null) // Delegate must be from a stale ProgramContext.  Stop trying to call it.
                    ColorDelegate = null;
            }
        }

        private void InitializeSuffixes()
        {
            AddSuffix(new[] { "VEC", "VECTOR" }, new SetSuffix<Vector>(() => new Vector(Vector), value =>
               {
                   Vector = value;
                   RenderPointCoords();
               }));
            AddSuffix(new[] { "VECUPDATER", "VECTORUPDATER" },
                      new SetSuffix<UserDelegate>(
                          () => VectorDelegate ?? new NoDelegate(shared.Cpu), // never return a null to user code - make it a DoNothingDelegate instead.
                          value => { VectorDelegate = (value is NoDelegate ? null : value); } // internally use null in place of DoNothingDelegate.
                         ));
            AddSuffix(new[] { "COLOR", "COLOUR" }, new SetSuffix<RgbaColor>(() => Color, value =>
               {
                   Color = value;
                   RenderColor();
               }));
            AddSuffix(new[] { "COLORUPDATER", "COLOURUPDATER" },
                      new SetSuffix<UserDelegate>(
                          () => ColorDelegate ?? new NoDelegate(shared.Cpu),  // never return a null to user code - make it a DoNothingDelegate instead.
                          value => { ColorDelegate = (value is NoDelegate ? null : value); } // internally use null in place of DoNothingDelegate.
                         ));
            AddSuffix("SHOW", new SetSuffix<BooleanValue>(() => enable, SetShow));
            AddSuffix("START", new SetSuffix<Vector>(() => new Vector(Start), value =>
            {
                Start = value;
                RenderPointCoords();
            }));
            AddSuffix("STARTUPDATER",
                      new SetSuffix<UserDelegate>(
                          () => StartDelegate ?? new NoDelegate(shared.Cpu),  // never return a null to user code - make it a DoNothingDelegate instead.
                          value => { StartDelegate = (value is NoDelegate ? null : value); } // internally use null in place of DoNothingDelegate.
                         ));
            AddSuffix("SCALE", new SetSuffix<ScalarValue>(() => Scale, value =>
            {
                Scale = value;
                RenderPointCoords();
            }));

            AddSuffix("LABEL", new SetSuffix<StringValue>(() => labelStr, SetLabel));
            AddSuffix("WIDTH", new SetSuffix<ScalarValue>(() => Width, value =>
            {
                Width = value;
                RenderPointCoords();
            }));

            AddSuffix("POINTY", new SetSuffix<BooleanValue>(() => Pointy, value => Pointy = value));
            AddSuffix("WIPING", new SetSuffix<BooleanValue>(() => Wiping, value => Wiping = value));
        }

        /// <summary>
        /// Update _shipCenterCoords, abstracting the different ways to do
        /// it depending on view mode:
        /// </summary>
        private void GetShipCenterCoords()
        {
            if (isOnMap)
                shipCenterCoords = ScaledSpace.LocalToScaledSpace(
                     shared.Vessel.CoMD);
            else
                shipCenterCoords = shared.Vessel.CoMD;
        }

        /// <summary>
        /// Update camera data, abstracting the different ways KSP does it
        /// depending on view mode:
        /// </summary>
        private void GetCamData()
        {
            prevIsOnMap = isOnMap;
            prevCamLookVec = camLookVec;
            prevCamRot = camRot;

            isOnMap = MapView.MapIsEnabled;

            var cam = Utils.GetCurrentCamera();
            camPos = (isOnMap ?  (Vector3)ScaledSpace.LocalToScaledSpace(cam.transform.position) : cam.transform.position);

            // the Distance coming from MapView.MapCamera.Distance
            // doesn't seem to work - calculating it myself below:
            // _camdist = pc.Distance();
            // camRot = cam.GetCameraTransform().rotation;
            camRot = cam.transform.rotation;
            camLookVec = camPos - shipCenterCoords;
            camLookUp = cam.transform.up;
        }

        /// <summary>
        /// Position the origins of the objects that make up the arrow
        /// such that they anchor relative to current ship position.
        /// </summary>
        private void PutAtShipRelativeCoords()
        {
            line.transform.localPosition = shipCenterCoords;
            hat.transform.localPosition = shipCenterCoords;
        }

        public bool GetShow()
        {
            return enable;
        }

        public void SetShow(BooleanValue newShowVal)
        {
            if (newShowVal)
            {
                if (line == null || hat == null || labelCanvasObj == null)
                {
                    lineObj = new GameObject("vecdrawLine");
                    hatObj = new GameObject("vecdrawHat");

                    line = lineObj.AddComponent<LineRenderer>();
                    hat = hatObj.AddComponent<LineRenderer>();

                    line.useWorldSpace = false;
                    hat.useWorldSpace = false;

                    GetShipCenterCoords();

                    // Note the Shader name string below comes from Kerbal's packaged shaders the
                    // game ships with - there's many to choose from but they're not documented what
                    // they are.  This was settled upon via trial and error:
                    // Additionally, Note that in KSP 1.8 because of the Unity update, some of these
                    // shaders Unity previously supplied were removed from Unity's DLLs.  SQUAD packaged them
                    // inside its own DLLs in 1.8 for modders who had been using them.  But because of that,
                    // mods have to use this different path to get to them:
                    Shader vecShader = Shader.Find("Particles/Alpha Blended"); // for when KSP version is < 1.8
                    if (vecShader == null)
                        vecShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended"); // for when KSP version is >= 1.8

                    line.material = new Material(vecShader);
                    hat.material = new Material(vecShader);

                    // This is how font loading would work if other fonts were available in KSP:
                    // Font lblFont = (Font)Resources.Load( "Arial", typeof(Font) );
                    // SafeHouse.Logger.Log( "lblFont is " + (lblFont == null ? "null" : "not null") );
                    // _label.font = lblFont;
                    CreateLabelHolder();

                    label.text = labelStr;
                    label.alignment = TextAnchor.MiddleCenter;

                    PutAtShipRelativeCoords();
                    RenderValues();
                }
                updateHandler.AddObserver(this);
                line.enabled = true;
                hat.enabled = Pointy;
                label.enabled = true;
            }
            else
            {
                updateHandler.RemoveObserver(this);
                if (label != null)
                {
                    label.enabled = false;
                    label = null;
                }
                if (hat != null)
                {
                    hat.enabled = false;
                    hat = null;
                }
                if (line != null)
                {
                    line.enabled = false;
                    line = null;
                }
                labelCanvasObj = null;
                hatObj = null;
                lineObj = null;
            }

            enable = newShowVal;
        }

        /// <summary>
        /// This replaces the use of GUIText with UI.Text, which takes a lot more work to
        /// set up, as Unity normally assumes you'll be creating code in the Unity Editor,
        /// which sets this up for you, rather than doing it by hand in your own C# code:
        /// </summary>
        private void CreateLabelHolder()
        {
            labelCanvasObj = new GameObject("labelCanvasObject", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            labelCanvasObj.AddComponent<CanvasRenderer>();
            labelTransform = labelCanvasObj.transform as RectTransform;
            labelTransform.SetParent(line.transform, false);
            label = labelCanvasObj.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            label.fontSize = 14;
        }

        public void SetLayer(int newVal)
        {
            if (lineObj != null) lineObj.layer = newVal;
            if (hatObj != null) hatObj.layer = newVal;
            if (labelCanvasObj != null) labelCanvasObj.layer = newVal;
        }

        public void SetLabel(StringValue newVal)
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
            if (line != null && hat != null && labelCanvasObj != null)
            {
                float mapLengthMult = 1.0f;
                if (isOnMap)
                {
                    mapLengthMult = ScaledSpace.InverseScaleFactor;
                }

                // From point1 to point3 is the vector.
                // point2 is the spot just short of point3 to start drawing
                // the pointy hat, if Pointy is enabled:
                Vector3d point1 = mapLengthMult * Start;
                Vector3d point2 = mapLengthMult * (Start + (Scale * 0.95 * Vector));
                Vector3d point3 = mapLengthMult * (Start + (Scale * Vector));

                float distToLine = DistanceFromPointToLineSegment((camPos - shared.Vessel.CoM), point1, point3);
                float mapWidthMult = 0.05f * distToLine;

                float useWidth = (float)(Width * Scale) * mapWidthMult;

                // Position the arrow line:
                line.positionCount = 2;
                line.startWidth = useWidth;
                line.endWidth = useWidth;
                line.SetPosition(0, point1);
                line.SetPosition(1, Pointy ? point2 : point3 );

                // Position the arrow hat.  Note, if Pointy = false, this will be invisible.
                hat.positionCount = 2;
                hat.startWidth = useWidth * 3.5f;
                hat.endWidth = 0.0f;
                hat.SetPosition(0, point2);
                hat.SetPosition(1, point3);

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
            Color lCol = UnityEngine.Color.Lerp(c2, UnityEngine.Color.white, 0.7f); // "whiten" the label color a lot.

            if (line != null && hat != null)
            {
                // If Wiping, then the line has the fade effect from color c1 to color c2,
                // else it stays at c2 the whole way:
                line.startColor = (Wiping ? c1 : c2); 
                line.endColor = c2;
                // The hat does not have the fade effect, staying at color c2 the whole way:
                hat.startColor = c2;
                hat.endColor = c2;
                label.color = lCol;     // The label does not have the fade effect.
            }
        }

        /// <summary>
        /// Calculate the closest distance between a point and a line segment.  This could be made
        /// into a generic utility if any other classes end up needing to use it too.
        /// Note, this is different from finding the closest distance between a point and a LINE.
        /// This is explicitly about a line SEGMENT, meaning if the closest distance to the line
        /// is outside the endpoints of the segment, it will measure from the nearest endpoint of
        /// the line segment rather than to the spot on the line that is outside the segment.
        /// </summary>
        /// <param name="point">the point to measure from</param>
        /// <param name="end1">endpoint 1 of the line segment</param>
        /// <param name="end2">endpoint 2 of the line segment</param>
        /// <returns>distance from point to the line (or the nearest endpoint of the segment)</returns>
        static private float DistanceFromPointToLineSegment(Vector3 point, Vector3 end1, Vector3 end2)
        {
            // theta = angle you get when you draw from end2 to end1 to point:
            float thetaDegrees = Vector3.Angle(end2 - end1, point - end1);
            // phi = angle you get when you draw from end1 to end2 to point:
            float phiDegrees = Vector3.Angle(end1 - end2, point - end2);

            // These are the degenerate cases where you have to pick an endpoint
            // because the closest point to the line is outside the line segment:
            if (thetaDegrees >= 90)
                return (point - end1).magnitude;
            if (phiDegrees >= 90)
                return (point - end2).magnitude;

            // If it got here, then the closest spot on the line is within the endpoints.
            // If we call that spot "s", then the triangle formed by end1->s->point
            // must be a right triangle with the corner at "s" being the corner with
            // the right angle.  So the length of the line s->point can come from trig:
            float hypotenuse = (point - end1).magnitude;
            return hypotenuse * Mathf.Sin(thetaDegrees * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Place the 2D label at the correct projected spot on
        /// the screen from its location in 3D space:
        /// </summary>
        private void LabelPlacement()
        {
            /*
            Vector3 screenPos = GetScreenPosfor( shipCenterCoords + labelLocation);

            // If the projected location is on-screen:
            if (screenPos.z >= 0
                 && screenPos.x >= 0 && screenPos.x <= Utils.GetCurrentCamera().pixelWidth
                 && screenPos.y >= 0 && screenPos.y <= Utils.GetCurrentCamera().pixelHeight)
            {
                label.enabled = true;
                // was: labelCanvasObj.transform.position = screenPos;
                labelCanvasObj.transform.position = screenPos;
            }
            else
            {
                label.enabled = false;
            }
            */
            labelTransform.localPosition = labelLocation;
            labelTransform.localRotation = camRot;

            Vector3 scaledLabelLocaton = (isOnMap ? (Vector3)ScaledSpace.LocalToScaledSpace(labelTransform.position) : labelTransform.position);
            float distanceFromCamera = (camPos - scaledLabelLocaton).magnitude;
            if (isOnMap)
                distanceFromCamera *= ScaledSpace.ScaleFactor;
            labelTransform.localScale = new Vector3(0.006f, 0.006f, 0.006f) * distanceFromCamera * (float)Width;
            label.enabled = true;
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