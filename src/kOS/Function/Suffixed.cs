﻿using System;
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
            double time = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            var result = new Node(time, radial, normal, prograde, shared);
            ReturnValue = result;
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

    [Function("bodyatmosphere")]
    public class FunctionBodyAtmosphere : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string bodyName = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);
            var result = new BodyAtmosphere(VesselUtils.GetBodyByName(bodyName));
            ReturnValue = result;
        }
    }

    [Function("heading")]
    public class FunctionHeading : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double pitchAboveHorizon = GetDouble(PopValueAssert(shared));
            double degreesFromNorth = GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);

            Vessel currentVessel = shared.Vessel;
            var q = UnityEngine.Quaternion.LookRotation(VesselUtils.GetNorthVector(currentVessel), currentVessel.upAxis);
            q *= UnityEngine.Quaternion.Euler(new UnityEngine.Vector3((float)-pitchAboveHorizon, (float)degreesFromNorth, 0));

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

            if (shared.AllVoiceValues.TryGetValue(voiceNum, out val))
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

    [Function("hsv")]
    public class FunctionHsv : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var v = (float) GetDouble(PopValueAssert(shared));
            var s = (float) GetDouble(PopValueAssert(shared));
            var h = (float) GetDouble(PopValueAssert(shared));
            AssertArgBottomAndConsume(shared);
            ReturnValue = new HsvColor(h,s,v);
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
        public override void Execute(SharedObjects shared)
        {
            int argc = CountRemainingArgs(shared);

            // Handle the var args that might be passed in, or give defaults if fewer args:
            double width   = (argc >= 7) ? GetDouble(PopValueAssert(shared))         : 0.2;
            bool   show    = (argc >= 6) ? Convert.ToBoolean(PopValueAssert(shared)) : false;
            double scale   = (argc >= 5) ? GetDouble(PopValueAssert(shared))         : 1.0;
            string str     = (argc >= 4) ? PopValueAssert(shared).ToString()         : "";
            RgbaColor rgba = (argc >= 3) ? GetRgba(PopValueAssert(shared))           : new RgbaColor(1.0f, 1.0f, 1.0f);
            Vector vec     = (argc >= 2) ? GetVector(PopValueAssert(shared))         : new Vector(1.0, 0.0, 0.0);
            Vector start   = (argc >= 1) ? GetVector(PopValueAssert(shared))         : new Vector(0.0, 0.0, 0.0);
            AssertArgBottomAndConsume(shared);
            DoExecuteWork(shared, start, vec, rgba, str, scale, show, width);
        }
        
        public void DoExecuteWork(SharedObjects shared, Vector start, Vector vec, RgbaColor rgba, string str, double scale, bool show, double width)
        {
            var vRend = new VectorRenderer( shared.UpdateHandler, shared )
                {
                    Vector = vec,
                    Start = start,
                    Color = rgba,
                    Scale = scale,
                    Width = width
                };
            vRend.SetLabel( str );
            vRend.SetShow( show );
            
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
            var when = GetTimeSpan(PopValueAssert(shared));
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
            var when = GetTimeSpan(PopValueAssert(shared));
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
            var when = GetTimeSpan(PopValueAssert(shared));
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
                returnList.Add(new WaypointValue(point, shared));
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

            ReturnValue = new WaypointValue(point, shared);
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
