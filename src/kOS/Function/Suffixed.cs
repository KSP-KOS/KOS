using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using kOS.Execution;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Function;
using kOS.Suffixed;
using kOS.Suffixed.Widget;
using kOS.Sound;
using kOS.Utilities;
using FinePrint;
using kOS.Safe;

namespace kOS.Function
{
    [Function("node")]
    public class FunctionNode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double prograde = GetDouble(PopValueAssert(shared));
            double normal = GetDouble(PopValueAssert(shared));
            double radial = GetDouble(PopValueAssert(shared));
            object time = PopValueAssert(shared);
            AssertArgBottomAndConsume(shared);
            if (time is kOS.Suffixed.TimeSpan)
            {
                ReturnValue = new Node((kOS.Suffixed.TimeSpan)time, radial, normal, prograde, shared);
            }
            else if (time is kOS.Suffixed.TimeStamp)
            {
                ReturnValue = new Node((TimeStamp)time, radial, normal, prograde, shared);
            }
            else
            {
                ReturnValue = new Node(GetDouble(time), radial, normal, prograde, shared);
            }
        }
    }

    [Function("v")]
    public class FunctionVector : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double z = GetDouble(PopValueAssert(shared));
            double y = GetDouble(PopValueAssert(shared));
            double x = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            var result = new Vector(x, y, z);
            ReturnValue = result;
        }
    }

    [Function("r")]
    public class FunctionRotation : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double roll = GetDouble(PopValueAssert(shared));
            double yaw = GetDouble(PopValueAssert(shared));
            double pitch = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            var result = new Direction(new Vector3d(pitch, yaw, roll), true);
            ReturnValue = result;
        }
    }

    [Function("q")]
    public class FunctionQuaternion : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double angle = GetDouble(PopValueAssert(shared));
            double roll = GetDouble(PopValueAssert(shared));
            double yaw = GetDouble(PopValueAssert(shared));
            double pitch = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            var result = new Direction(new UnityEngine.Quaternion((float)pitch, (float)yaw, (float)roll, (float)angle));
            ReturnValue = result;
        }
    }

    [Function("createOrbit")]
    public class FunctionCreateOrbit : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            bool cartesian = CountRemainingArgs(shared) == 4;

            double ut = cartesian ? GetDouble(PopValueAssert(shared)) : double.NaN;

            CelestialBody body;
            var bodyArg = PopValueAssert(shared);
            if (bodyArg is BodyTarget bodyTarget)
            {
                body = bodyTarget.Body;
            } else
            {
                var bodyName = bodyArg.ToString();
                body = VesselUtils.GetBodyByName(bodyName);
                if (body == null)
                    throw new KOSInvalidArgumentException("CREATEORBIT() constructor", bodyName, "Body not found in this solar system");
            }

            if (cartesian)
            {
                var velocity = GetVector(PopValueAssert(shared));
                var position = GetVector(PopValueAssert(shared));
                AssertArgBottomAndConsume(shared);
                var ret = new Orbit();
                ret.UpdateFromStateVectors(position, velocity, body, ut);
                ReturnValue = new OrbitInfo(ret, shared);
            } else 
            {
                double t = GetDouble(PopValueAssert(shared));
                double mEp = DegreesToRadians(GetDouble(PopValueAssert(shared)));
                double argPe = GetDouble(PopValueAssert(shared));
                double lan = GetDouble(PopValueAssert(shared));
                double sma = GetDouble(PopValueAssert(shared));
                double e = GetDouble(PopValueAssert(shared));
                double inc = GetDouble(PopValueAssert(shared));
                AssertArgBottomAndConsume(shared);
                ReturnValue = new OrbitInfo(new Orbit(inc, e, sma, lan, argPe, mEp, t, body), shared);
            }
        }
    }

    [Function("rotatefromto")]
    public class FunctionRotateFromTo : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Vector toVector = GetVector(PopValueAssert(shared));
            Vector fromVector = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            var result = Direction.FromVectorToVector(fromVector, toVector);
            ReturnValue = result;
        }
    }

    [Function("lookdirup")]
    public class FunctionLookDirUp : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Vector topVector = GetVector(PopValueAssert(shared));
            Vector lookVector = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            var result = Direction.LookRotation(lookVector, topVector);
            ReturnValue = result;
        }
    }

    [Function("angleaxis")]
    public class FunctionAngleAxis : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Vector axisVector = GetVector(PopValueAssert(shared));
            double degrees = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            var result = Direction.AngleAxis(degrees, axisVector);
            ReturnValue = result;
        }
    }

    [Function("latlng")]
    public class FunctionLatLng : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double longitude = GetDouble(PopValueAssert(shared));
            double latitude = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            var result = new GeoCoordinates(shared, latitude, longitude);
            ReturnValue = result;
        }
    }

    [Function("vessel")]
    public class FunctionVessel : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string vesselName = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            var result = VesselTarget.CreateOrGetExisting(VesselUtils.GetVesselByName(vesselName, shared.Vessel), shared);
            ReturnValue = result;
        }
    }

    [Function("body")]
    public class FunctionBody : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string bodyName = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            var result = BodyTarget.CreateOrGetExisting(bodyName, shared);
            ReturnValue = result;
        }
    }

    [Function("bodyexists")]
    public class FunctionBodyExists : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string bodyName = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            ReturnValue = VesselUtils.GetBodyByName(bodyName) != null;
        }
    }

    [Function("bodyatmosphere")]
    public class FunctionBodyAtmosphere : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string bodyName = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            var bod = VesselUtils.GetBodyByName(bodyName);
            if (bod == null)
                throw new KOSInvalidArgumentException(GetFuncName(), bodyName, "Body not found in this solar system");
            var result = new BodyAtmosphere(bod, shared);
            ReturnValue = result;
        }
    }

    [Function("bounds")]
    public class FunctionBounds : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Vector relMax = GetVector(PopValueAssert(shared));
            Vector relMin = GetVector(PopValueAssert(shared));
            Direction facing = GetDirection(PopValueAssert(shared));
            Vector absOrigin = GetVector(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            ReturnValue = new BoundsValue(relMin, relMax, absOrigin, facing, shared);
        }
    }

    [Function("heading")]
    public class FunctionHeading : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int argCount = CountRemainingArgs(shared);
            double roll = (argCount >= 3) ? GetDouble(PopValueAssert(shared)) : double.NaN;
            double pitchAboveHorizon = GetDouble(PopValueAssert(shared));
            double degreesFromNorth = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            Vessel currentVessel = shared.Vessel;
            var q = UnityEngine.Quaternion.LookRotation(VesselUtils.GetNorthVector(currentVessel), currentVessel.upAxis);
            q *= UnityEngine.Quaternion.Euler(new UnityEngine.Vector3((float)-pitchAboveHorizon, (float)degreesFromNorth, 0));
            if (!double.IsNaN(roll))
                q *= UnityEngine.Quaternion.Euler(0, 0, (float)roll);

            var result = new Direction(q);
            ReturnValue = result;
        }
    }
    
    [Function("slidenote")]
    public class FunctionSlideNote : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int argCount = CountRemainingArgs(shared);
            float vol = 1.0f;
            float keyDownDuration = -1f;
            if (argCount >= 5)
                vol = (float) GetDouble(PopValueAssert(shared));
            if (argCount >= 4)
                keyDownDuration = (float)GetDouble(PopValueAssert(shared));
            float duration = (float)GetDouble(PopValueAssert(shared));
            object endNote = PopValueAssert(shared);
            object startNote = PopValueAssert(shared);
            AssertArgBottomAndConsume(shared);
            if (keyDownDuration < 0)
                keyDownDuration = duration * 0.9f; // default to 90% of the total duration, allowing a short window for the release
            if (keyDownDuration > duration)
                keyDownDuration = duration; // clamp keyDown to the total duration

            if (startNote is ScalarValue)
                ReturnValue = new NoteValue((float)GetDouble(startNote), (float)GetDouble(endNote), vol, keyDownDuration, duration);
            else if (startNote is StringValue)
                ReturnValue = new NoteValue(startNote.ToString(), endNote.ToString(), vol, keyDownDuration, duration);
            else
                ReturnValue = new NoteValue(0f, vol, keyDownDuration, duration);
        }
    }

    [Function("note")]
    public class FunctionNote : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int argCount = CountRemainingArgs(shared);
            float vol = 1.0f;
            float keyDownDuration = -1f;
            if (argCount >= 4)
                vol = (float) GetDouble(PopValueAssert(shared));
            if (argCount >= 3)
                keyDownDuration = (float)GetDouble(PopValueAssert(shared));
            float duration = (float)GetDouble(PopValueAssert(shared)); 
            object note = PopValueAssert(shared);
            AssertArgBottomAndConsume(shared);
            if (keyDownDuration < 0)
                keyDownDuration = duration * 0.9f; // default to 90% of the total duration, allowing a short window for the release
            if (keyDownDuration > duration)
                keyDownDuration = duration; // clamp keyDown to the total duration

            if (note is ScalarValue)
                ReturnValue = new NoteValue((float)GetDouble(note), vol, keyDownDuration, duration);
            else if (note is StringValue)
                ReturnValue = new NoteValue(note.ToString(), vol, keyDownDuration, duration);
            else
                ReturnValue = new NoteValue(0f, vol, keyDownDuration, duration);
        }
    }

    [Function("GetVoice")]
    public class FunctionGetVoice : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int voiceNum = GetInt(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            VoiceValue val;

            if (shared.AllVoiceValues.TryGetValue(voiceNum,out val))
                ReturnValue = val;
            else
            {
                shared.AllVoiceValues[voiceNum] = new VoiceValue(shared.UpdateHandler, voiceNum, shared.SoundMaker);
                ReturnValue = shared.AllVoiceValues[voiceNum];
            }
        }
    }


    [Function("StopAllVoices")]
    public class FunctionStopAllVoices : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            shared.SoundMaker.StopAllVoices();
        }
    }
    [Function("timestamp", "time")]
    public class FunctionTimeStamp : FunctionBase
    {
        // Note: "TIME" is both a bound variable AND a built-in function now.
        // If it gets called with parentheses(), the script calls this built-in function.
        // If it gets called without them, then the bound variable is what gets called instead.
        // Calling it using parentheses but with empty args: TIME() gives the same result
        // as the bound variable.  While it would be cleaner to make it JUST a built-in function,
        // the bound variable had to be retained for backward compatibility with scripts
        // that call TIME without parentheses.
        public override void Execute(SharedObjects shared)
        {
            double ut;
            // Accepts zero or one arg:
            int argCount = CountRemainingArgs(shared);

            // If zero args, then the default is to assume you want to
            // make a TimeStamp of "now":
            if (argCount == 0)
            {
                ReturnValue = new kOS.Suffixed.TimeStamp(Planetarium.GetUniversalTime());
            }
            // If one arg, then assume its in UT timestamp seconds:
            else if (argCount == 1)
            {
                ut = GetDouble(PopValueAssert(shared));
                ReturnValue = new kOS.Suffixed.TimeStamp(ut);
            }
            // If more args, assume they are year, day, hour, minute, second, with optional
            // args at the end (eg. if there's only 3 args, it's year, day, hour with no minutes or seconds).
            else if (argCount == 2)
            {
                double day = GetDouble(PopValueAssert(shared));
                double year = GetDouble(PopValueAssert(shared));
                ReturnValue = new kOS.Suffixed.TimeStamp(year, day, 0.0, 0.0, 0.0);
            }
            else if (argCount == 3)
            {
                double hour = GetDouble(PopValueAssert(shared));
                double day = GetDouble(PopValueAssert(shared));
                double year = GetDouble(PopValueAssert(shared));
                ReturnValue = new kOS.Suffixed.TimeStamp(year, day, hour, 0.0, 0.0);
            }
            else if (argCount == 4)
            {
                double minute = GetDouble(PopValueAssert(shared));
                double hour = GetDouble(PopValueAssert(shared));
                double day = GetDouble(PopValueAssert(shared));
                double year = GetDouble(PopValueAssert(shared));
                ReturnValue = new kOS.Suffixed.TimeStamp(year, day, hour, minute, 0.0);
            }
            else if (argCount == 5)
            {
                double second = GetDouble(PopValueAssert(shared));
                double minute = GetDouble(PopValueAssert(shared));
                double hour = GetDouble(PopValueAssert(shared));
                double day = GetDouble(PopValueAssert(shared));
                double year = GetDouble(PopValueAssert(shared));
                ReturnValue = new kOS.Suffixed.TimeStamp(year, day, hour, minute, second);
            }
            AssertArgBottomAndConsume(shared);
        }
    }

    [Function("timespan")]
    public class FunctionTimeSpan : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double ut;
            // Accepts zero or one arg:
            int argCount = CountRemainingArgs(shared);

            // If one arg, then assume its seconds:
            if (argCount == 1)
            {
                ut = GetDouble(PopValueAssert(shared));
                ReturnValue = new kOS.Suffixed.TimeSpan(ut);
            }
            // If more args, assume they are year, day, hour, minute, second, with optional
            // args at the end (eg. if there's only 3 args, it's year, day, hour with no minutes or seconds).
            else if (argCount == 2)
            {
                double day = GetDouble(PopValueAssert(shared));
                double year = GetDouble(PopValueAssert(shared));
                ReturnValue = new kOS.Suffixed.TimeSpan(year, day, 0.0, 0.0, 0.0);
            }
            else if (argCount == 3)
            {
                double hour = GetDouble(PopValueAssert(shared));
                double day = GetDouble(PopValueAssert(shared));
                double year = GetDouble(PopValueAssert(shared));
                ReturnValue = new kOS.Suffixed.TimeSpan(year, day, hour, 0.0, 0.0);
            }
            else if (argCount == 4)
            {
                double minute = GetDouble(PopValueAssert(shared));
                double hour = GetDouble(PopValueAssert(shared));
                double day = GetDouble(PopValueAssert(shared));
                double year = GetDouble(PopValueAssert(shared));
                ReturnValue = new kOS.Suffixed.TimeSpan(year, day, hour, minute, 0.0);
            }
            else if (argCount == 5)
            {
                double second = GetDouble(PopValueAssert(shared));
                double minute = GetDouble(PopValueAssert(shared));
                double hour = GetDouble(PopValueAssert(shared));
                double day = GetDouble(PopValueAssert(shared));
                double year = GetDouble(PopValueAssert(shared));
                ReturnValue = new kOS.Suffixed.TimeSpan(year, day, hour, minute, second);
            }
            AssertArgBottomAndConsume(shared);
        }
    }

    [Function("hsv")]
    public class FunctionHsv : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var v = (float) GetDouble(PopValueAssert(shared));
            var s = (float) GetDouble(PopValueAssert(shared));
            var h = (float) GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            ReturnValue = new HsvaColor(h,s,v);
        }
    }

    [Function("hsva")]
    public class FunctionHsva : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var a = (float) GetDouble(PopValueAssert(shared));
            var v = (float) GetDouble(PopValueAssert(shared));
            var s = (float) GetDouble(PopValueAssert(shared));
            var h = (float) GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            ReturnValue = new HsvaColor(h,s,v,a);
        }
    }

    [Function("rgb")]
    public class FunctionRgb : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var b = (float) GetDouble(PopValueAssert(shared));
            var g = (float) GetDouble(PopValueAssert(shared));
            var r = (float) GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            ReturnValue = new RgbaColor(r,g,b);
        }
    }

    [Function("rgba")]
    public class FunctionRgba : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var a = (float) GetDouble(PopValueAssert(shared));
            var b = (float) GetDouble(PopValueAssert(shared));
            var g = (float) GetDouble(PopValueAssert(shared));
            var r = (float) GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            ReturnValue = new RgbaColor(r,g,b,a);
        }
    }

    // There are two ways to initialize a vecdraw, with
    //   vecdraw()
    // or with
    //   vecdrawargs(,vector,vector,rgba,double,bool)
    //
    // Note: vecdraw now counts the args and changes its behavior accordingly.
    // For backward compatibility, vecdrawargs has been aliased to vecdraw.
    //
    [Function("vecdraw", "vecdrawargs")]
    public class FunctionVecDrawNull : FunctionBase
    {
        protected RgbaColor GetDefaultColor()
        {
            return new RgbaColor(1.0f, 1.0f, 1.0f);
        }

        protected Vector GetDefaultVector()
        {
            return new Vector(1.0, 0.0, 0.0);
        }

        protected Vector GetDefaultStart()
        {
            return new Vector(0, 0, 0);
        }

        public override void Execute(SharedObjects shared)
        {
            int argc = CountRemainingArgs(shared);

            // Handle the var args that might be passed in, or give defaults if fewer args:
            bool   wiping   = (argc >= 9) ? Convert.ToBoolean(PopValueAssert(shared)) : true;
            bool   pointy   = (argc >= 8) ? Convert.ToBoolean(PopValueAssert(shared)) : true;
            double width    = (argc >= 7) ? GetDouble(PopValueAssert(shared))         : 0.2;
            bool   show     = (argc >= 6) ? Convert.ToBoolean(PopValueAssert(shared)) : false;
            double scale    = (argc >= 5) ? GetDouble(PopValueAssert(shared))         : 1.0;
            string str      = (argc >= 4) ? PopValueAssert(shared).ToString()         : "";

            // Pop the arguments or use the default if omitted
            object argRgba  = (argc >= 3) ? PopValueAssert(shared) : GetDefaultColor();
            object argVec   = (argc >= 2) ? PopValueAssert(shared) : GetDefaultVector();
            object argStart = (argc >= 1) ? PopValueAssert(shared) : GetDefaultStart();

            // Assign the arguments of type delegate or null otherwise
            KOSDelegate colorUpdater = argRgba  as KOSDelegate;
            KOSDelegate vecUpdater   = argVec   as KOSDelegate;
            KOSDelegate startUpdater = argStart as KOSDelegate;

            // Get the values or use the default if its a delegate
            RgbaColor rgba = (colorUpdater == null) ? GetRgba(argRgba)    : GetDefaultColor();
            Vector vec     = (vecUpdater == null)   ? GetVector(argVec)   : GetDefaultVector();
            Vector start   = (startUpdater == null) ? GetVector(argStart) : GetDefaultStart();

            AssertArgBottomAndConsume(shared);
            DoExecuteWork(shared, start, vec, rgba, str, scale, show, width, pointy, wiping, colorUpdater, vecUpdater, startUpdater);
        }
        
        public void DoExecuteWork(
            SharedObjects shared,
            Vector start,
            Vector vec,
            RgbaColor rgba,
            string str,
            double scale,
            bool show,
            double width,
            bool pointy,
            bool wiping,
            KOSDelegate colorUpdater,
            KOSDelegate vecUpdater,
            KOSDelegate startUpdater)
        {
            var vRend = new VectorRenderer(shared.UpdateHandler, shared)
                {
                    Vector = vec,
                    Start = start,
                    Color = rgba,
                    Scale = scale,
                    Width = width,
                    Pointy = pointy,
                    Wiping = wiping
                };
            vRend.SetLabel( str );
            vRend.SetShow( show );

            if (colorUpdater != null)
                vRend.SetSuffix("COLORUPDATER", colorUpdater);

            if (vecUpdater != null)
                vRend.SetSuffix("VECUPDATER", vecUpdater);

            if (startUpdater != null)
                vRend.SetSuffix("STARTUPDATER", startUpdater);
            
            ReturnValue = vRend;
        }
    }

    [Function("clearvecdraws")]
    public class FunctionHideAllVecdraws : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            VectorRenderer.ClearAll(shared.UpdateHandler);
        }
    }

    [Function("clearguis")]
    public class FunctionClearAllGuis : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            GUIWidgets.ClearAll(shared);
        }
    }

    [Function("gui")]
    public class FunctionWidgets : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int argc = CountRemainingArgs(shared);
            int height = argc > 1 ? GetInt(PopValueAssert(shared)) : 0;
            int width = GetInt(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            ReturnValue = new GUIWidgets(width,height,shared);
        }
    }

    [Function("positionat")]
    public class FunctionPositionAt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var when = GetTimeStamp(PopValueAssert(shared));
            var what = GetOrbitable(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            ReturnValue = what.GetPositionAtUT(when);
        }
    }

    [Function("velocityat")]
    public class FunctionVelocityAt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var when = GetTimeStamp(PopValueAssert(shared));
            var what = GetOrbitable(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            ReturnValue = what.GetVelocitiesAtUT(when);
        }
    }

    [Function("highlight")]
    public class FunctionHightlight : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var color = GetRgba(PopValueAssert(shared));
            var obj = PopValueAssert(shared);
            AssertArgBottomAndConsume(shared);

            ReturnValue = new HighlightStructure(shared.UpdateHandler, obj, color);
        }
    }

    [Function("orbitat")]
    public class FunctionOrbitAt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var when = GetTimeStamp(PopValueAssert(shared));
            var what = GetOrbitable(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            ReturnValue = new OrbitInfo( what.GetOrbitAtUT(when.ToUnixStyleTime()), shared );
        }
    }
    
    [Function("career")]
    public class FunctionCareer : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared); // no args
            ReturnValue = new Career();
        }
    }
    
    [Function("allwaypoints")]
    public class FunctionAllWaypoints : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared); // no args
            
            // ReSharper disable SuggestUseVarKeywordEvident
            ListValue<WaypointValue> returnList = new ListValue<WaypointValue>();
            // ReSharper enable SuggestUseVarKeywordEvident

            WaypointManager wpm = WaypointManager.Instance();
            if (wpm == null)
            {
                ReturnValue = returnList; // When no waypoints exist, there isn't even a waypoint manager at all.
                return;
            }

            List<Waypoint> points = wpm.Waypoints;

            // If the code below gets used in more places it may be worth moving into a factory method
            // akin to how PartValueFactory makes a ListValue<PartValue> from a List<Part>.
            // But for now, this is the only place it's done:

            foreach (Waypoint point in points)
            {
                WaypointValue wp = WaypointValue.CreateWaypointValueWithCheck(point, shared, true);
                if (wp != null)
                    returnList.Add(wp);
            }
            ReturnValue = returnList;
        }
    }
    
    [Function("waypoint")]
    public class FunctionWaypoint : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string pointName = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);

            WaypointManager wpm = WaypointManager.Instance();

            // If no contracts have been generated with waypoints in them,
            // then sometimes the stock game's waypoint manager doesn't even
            // exist yet either.  (The base game seems not to instance one until the
            // first time a contract with a waypoint is created).
            if (wpm == null)
                throw new KOSInvalidArgumentException("waypoint", "\""+pointName+"\"", "no waypoints exist");
            
            // If this name has a greek letter in it's spelling like "alpha", "beta", etc, then it
            // is probably part of a waypoint cluster.
            // Waypoint clusters are actually 1 waypoint with an array of "children" by index number.
            // where the waypoint's name is just the base part with the "alpha", "beta", etc suffix removed.
            string baseName;
            int index;
            bool hasGreek = WaypointValue.GreekToInteger(pointName, out index, out baseName);

            Waypoint point = null;
            if (hasGreek) // Attempt to find it as part of a waypoint cluster.
            {
                point = wpm.Waypoints.FirstOrDefault(
                    p => string.Equals(p.name, baseName,StringComparison.CurrentCultureIgnoreCase) && (!hasGreek || p.index == index));
                if (point != null) // Will be null if this name is not really a waypoint cluster.
                    pointName = baseName;
            }
            if (point == null) // Either it had no greek letter, or it did but wasn't a waypoint cluster.  Try it as a vanilla waypoint:
            {
                point = wpm.Waypoints.FirstOrDefault(
                    p => string.Equals(p.name, pointName, StringComparison.CurrentCultureIgnoreCase) && (!hasGreek || p.index == index));
            }

            // If it's still null at this point then give up - we can't find such a waypoint name:
            if (point == null)
                throw new KOSInvalidArgumentException("waypoint", "\""+pointName+"\"", "no such waypoint");

        ReturnValue = WaypointValue.CreateWaypointValueWithCheck(point, shared, false);
        }
    }

    [Function("transferall")]
    public class FunctionTransferAll : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var transferTo = PopValueAssert(shared);
            var transferFrom = PopValueAssert(shared);
            var resourceName = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);

            var resourceInfo = TransferManager.ParseResource(resourceName);
            if (resourceInfo == null)
            {
                throw new KOSInvalidArgumentException("TransferAll", "Resource",
                    resourceName + " was not found in the resource list");
            }

            object toPush = shared.TransferManager.CreateTransfer(resourceInfo, transferTo, transferFrom);
            ReturnValue = toPush;
        }

    }

    [Function("transfer")]
    public class FunctionTransfer : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var amount = PopValueAssert(shared);
            var transferTo = PopValueAssert(shared);
            var transferFrom = PopValueAssert(shared);
            var resourceName = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);

            var resourceInfo = TransferManager.ParseResource(resourceName);
            if (resourceInfo == null)
            {
                throw new KOSInvalidArgumentException("TransferAll", "Resource",
                    resourceName + " was not found in the resource list");
            }

            double parsedAmount;
            if (Double.TryParse(amount.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsedAmount))
            {
                object toPush = shared.TransferManager.CreateTransfer(resourceInfo, transferTo, transferFrom, parsedAmount);
                ReturnValue = toPush;
            }
        }
    }
}
