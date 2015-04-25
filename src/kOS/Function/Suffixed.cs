using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Execution;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Function;
using kOS.Suffixed;
using kOS.Utilities;
using FinePrint;

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
            var result = new VesselTarget(VesselUtils.GetVesselByName(vesselName, shared.Vessel), shared);
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
            var result = new BodyTarget(bodyName, shared);
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

    [Function("list")]
    public class FunctionList : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object[] argArray = new object[CountRemainingArgs(shared)];
            for (int i = argArray.Length - 1 ; i >= 0 ; --i)
                argArray[i] = PopValueAssert(shared); // fill array in reverse order because .. stack args.
            AssertArgBottomAndConsume(shared);
            var listValue = new ListValue(argArray.ToList());
            ReturnValue = listValue;
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
            ReturnValue = new HsvColor(h,s,v,a);
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
            // If I was called with arguments, then run the version of the constructor that takes args
            if (argc == 6)
            {
                bool      show  = Convert.ToBoolean(PopValueAssert(shared));
                double    scale = GetDouble(PopValueAssert(shared));
                string    str   = PopValueAssert(shared).ToString();
                RgbaColor rgba  = GetRgba(PopValueAssert(shared));
                Vector    vec   = GetVector(PopValueAssert(shared));
                Vector    start = GetVector(PopValueAssert(shared));
                AssertArgBottomAndConsume(shared);
                DoExecuteWork(shared, start, vec, rgba, str, scale, show);
            }
            else if (argc == 0)
            {
                AssertArgBottomAndConsume(shared); // no args
                DoExecuteWork(shared);  // default constructor:
            }
            else
            {
                throw new KOSArgumentMismatchException("Vecdraw() expected either 0 or 6 arguments passed, but got " + argc +" instead.");
            }
        }
        
        public void DoExecuteWork(SharedObjects shared)
        {
            var vRend = new VectorRenderer( shared.UpdateHandler, shared );
            vRend.SetShow( false );
            
            ReturnValue = vRend;            
        }

        public void DoExecuteWork(SharedObjects shared, Vector start, Vector vec, RgbaColor rgba, string str, double scale, bool show)
        {
            var vRend = new VectorRenderer( shared.UpdateHandler, shared )
                {
                    Vector = vec,
                    Start = start,
                    Color = rgba,
                    Scale = scale
                };
            vRend.SetLabel( str );
            vRend.SetShow( show );
            
            ReturnValue = vRend;
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
    
    [Function("constant")]
    public class FunctionConstant : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared); // no args
            ReturnValue = new ConstantValue();
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

            List<Waypoint> points = wpm.AllWaypoints();

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
            if (wpm == null) // When zero waypoints exist, there might not even be a waypoint manager.
            {
                ReturnValue = null;
                // I don't like returning null here without the user being able to test for that, but
                // we don't have another way to communicate "no such waypoint".  We really need to address
                // that problem once and for all.
                return;
            }
            
            string baseName;
            int index;
            bool hasGreek = WaypointValue.GreekToInteger(pointName, out index, out baseName);
            Waypoint point = wpm.AllWaypoints().FirstOrDefault(
                p => String.Equals(p.name, baseName,StringComparison.CurrentCultureIgnoreCase) && (!hasGreek || p.index == index));

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
            if (Double.TryParse(amount.ToString(), out parsedAmount))
            {
                object toPush = shared.TransferManager.CreateTransfer(resourceInfo, transferTo, transferFrom, parsedAmount);
                ReturnValue = toPush;
            }
        }
    }
}
