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
            double prograde = GetDouble(shared.Cpu.PopValue());
            double normal = GetDouble(shared.Cpu.PopValue());
            double radial = GetDouble(shared.Cpu.PopValue());
            double time = GetDouble(shared.Cpu.PopValue());

            var result = new Node(time, radial, normal, prograde, shared);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("v")]
    public class FunctionVector : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double z = GetDouble(shared.Cpu.PopValue());
            double y = GetDouble(shared.Cpu.PopValue());
            double x = GetDouble(shared.Cpu.PopValue());

            var result = new Vector(x, y, z);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("r")]
    public class FunctionRotation : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double roll = GetDouble(shared.Cpu.PopValue());
            double yaw = GetDouble(shared.Cpu.PopValue());
            double pitch = GetDouble(shared.Cpu.PopValue());

            var result = new Direction(new Vector3d(pitch, yaw, roll), true);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("q")]
    public class FunctionQuaternion : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double angle = GetDouble(shared.Cpu.PopValue());
            double roll = GetDouble(shared.Cpu.PopValue());
            double yaw = GetDouble(shared.Cpu.PopValue());
            double pitch = GetDouble(shared.Cpu.PopValue());

            var result = new Direction(new UnityEngine.Quaternion((float)pitch, (float)yaw, (float)roll, (float)angle));
            shared.Cpu.PushStack(result);
        }
    }

    [Function("rotatefromto")]
    public class FunctionRotateFromTo : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Vector toVector = GetVector(shared.Cpu.PopValue());
            Vector fromVector = GetVector(shared.Cpu.PopValue());

            var result = Direction.FromVectorToVector(fromVector, toVector);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("lookdirup")]
    public class FunctionLookDirUp : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Vector topVector = GetVector(shared.Cpu.PopValue());
            Vector lookVector = GetVector(shared.Cpu.PopValue());

            var result = Direction.LookRotation(lookVector, topVector);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("angleaxis")]
    public class FunctionAngleAxis : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Vector axisVector = GetVector(shared.Cpu.PopValue());
            double degrees = GetDouble(shared.Cpu.PopValue());

            var result = Direction.AngleAxis(degrees, axisVector);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("latlng")]
    public class FunctionLatLng : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double longitude = GetDouble(shared.Cpu.PopValue());
            double latitude = GetDouble(shared.Cpu.PopValue());

            var result = new GeoCoordinates(shared, latitude, longitude);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("vessel")]
    public class FunctionVessel : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string vesselName = shared.Cpu.PopValue().ToString();
            var result = new VesselTarget(VesselUtils.GetVesselByName(vesselName, shared.Vessel), shared);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("body")]
    public class FunctionBody : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string bodyName = shared.Cpu.PopValue().ToString();
            var result = new BodyTarget(bodyName, shared);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("bodyatmosphere")]
    public class FunctionBodyAtmosphere : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string bodyName = shared.Cpu.PopValue().ToString();
            var result = new BodyAtmosphere(VesselUtils.GetBodyByName(bodyName));
            shared.Cpu.PushStack(result);
        }
    }

    [Function("heading")]
    public class FunctionHeading : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double pitchAboveHorizon = GetDouble(shared.Cpu.PopValue());
            double degreesFromNorth = GetDouble(shared.Cpu.PopValue());

            Vessel currentVessel = shared.Vessel;
            var q = UnityEngine.Quaternion.LookRotation(VesselUtils.GetNorthVector(currentVessel), currentVessel.upAxis);
            q *= UnityEngine.Quaternion.Euler(new UnityEngine.Vector3((float)-pitchAboveHorizon, (float)degreesFromNorth, 0));

            var result = new Direction(q);
            shared.Cpu.PushStack(result);
        }
    }

    [Function("list")]
    public class FunctionList : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var listValue = new ListValue();
            shared.Cpu.PushStack(listValue);
        }
    }

    [Function("hsv")]
    public class FunctionHsv : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var v = (float) GetDouble(shared.Cpu.PopValue());
            var s = (float) GetDouble(shared.Cpu.PopValue());
            var h = (float) GetDouble(shared.Cpu.PopValue());
            shared.Cpu.PushStack( new HsvColor(h,s,v) );
        }
    }

    [Function("hsva")]
    public class FunctionHsva : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var a = (float) GetDouble(shared.Cpu.PopValue());
            var v = (float) GetDouble(shared.Cpu.PopValue());
            var s = (float) GetDouble(shared.Cpu.PopValue());
            var h = (float) GetDouble(shared.Cpu.PopValue());
            shared.Cpu.PushStack( new HsvColor(h,s,v,a) );
        }
    }

    [Function("rgb")]
    public class FunctionRgb : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var b = (float) GetDouble(shared.Cpu.PopValue());
            var g = (float) GetDouble(shared.Cpu.PopValue());
            var r = (float) GetDouble(shared.Cpu.PopValue());
            shared.Cpu.PushStack( new RgbaColor(r,g,b) );
        }
    }

    [Function("rgba")]
    public class FunctionRgba : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var a = (float) GetDouble(shared.Cpu.PopValue());
            var b = (float) GetDouble(shared.Cpu.PopValue());
            var g = (float) GetDouble(shared.Cpu.PopValue());
            var r = (float) GetDouble(shared.Cpu.PopValue());
            shared.Cpu.PushStack( new RgbaColor(r,g,b,a) );
        }
    }

    // There are two ways to initialize a vecdraw, with
    //   vecdraw()
    // or with
    //   vecdrawargs(,vector,vector,rgba,double,bool)
    // If varying args were more easily supported, this could
    // be done with just one function that counts how many args it
    // was given.
    //
    [Function("vecdraw")]
    public class FunctionVecDrawNull : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var vRend = new VectorRenderer( shared.UpdateHandler, shared );
            vRend.SetShow( false );
            
            shared.Cpu.PushStack( vRend );
        }
    }

    [Function("vecdrawargs")]
    public class FunctionVecDraw : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            bool      show  = Convert.ToBoolean(shared.Cpu.PopValue());
            double    scale = GetDouble(shared.Cpu.PopValue());
            string    str   = shared.Cpu.PopValue().ToString();
            RgbaColor rgba  = GetRgba(shared.Cpu.PopValue());
            Vector    vec   = GetVector(shared.Cpu.PopValue());
            Vector    start = GetVector(shared.Cpu.PopValue());

            var vRend = new VectorRenderer( shared.UpdateHandler, shared )
                {
                    Vector = vec,
                    Start = start,
                    Color = rgba,
                    Scale = scale
                };
            vRend.SetLabel( str );
            vRend.SetShow( show );
            
            shared.Cpu.PushStack( vRend );
        }
    }

    [Function("positionat")]
    public class FunctionPositionAt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var when = GetTimeSpan(shared.Cpu.PopValue());
            var what = GetOrbitable(shared.Cpu.PopValue());

            var pos = what.GetPositionAtUT(when);
            
            shared.Cpu.PushStack(pos);
        }
    }

    [Function("velocityat")]
    public class FunctionVelocityAt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var when = GetTimeSpan(shared.Cpu.PopValue());
            var what = GetOrbitable(shared.Cpu.PopValue());

            var vels = what.GetVelocitiesAtUT(when);
            
            shared.Cpu.PushStack(vels);
        }
    }

    [Function("highlight")]
    public class FunctionHightlight : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var color = GetRgba(shared.Cpu.PopValue());
            var obj = shared.Cpu.PopValue();

            var toPush = new HighlightStructure(shared.UpdateHandler, obj, color);
            shared.Cpu.PushStack(toPush);
        }
    }

    [Function("orbitat")]
    public class FunctionOrbitAt : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var when = GetTimeSpan(shared.Cpu.PopValue());
            var what = GetOrbitable(shared.Cpu.PopValue());

            var orb = new OrbitInfo( what.GetOrbitAtUT(when.ToUnixStyleTime()), shared );
            
            shared.Cpu.PushStack(orb);
        }
    }
    
    [Function("career")]
    public class FunctionCareer : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var career = new Career();
            shared.Cpu.PushStack(career);
        }
    }
    
    [Function("constant")]
    public class FunctionConstant : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var constants = new ConstantValue();
            shared.Cpu.PushStack(constants);
        }
    }
    
    [Function("allwaypoints")]
    public class FunctionAllWaypoints : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            // ReSharper disable SuggestUseVarKeywordEvident
            ListValue<WaypointValue> returnList = new ListValue<WaypointValue>();
            // ReSharper enable SuggestUseVarKeywordEvident

            WaypointManager wpm = WaypointManager.Instance();
            if (wpm == null)
            {
                shared.Cpu.PushStack(returnList); // When no waypoints exist, there isn't even a waypoint manager at all.
                return;
            }


            List<Waypoint> points = wpm.AllWaypoints();

            // If the code below gets used in more places it may be worth moving into a factory method
            // akin to how PartValueFactory makes a ListValue<PartValue> from a List<Part>.
            // But for now, this is the only place it's done:

            foreach (Waypoint point in points)
                returnList.Add(new WaypointValue(point, shared));
            shared.Cpu.PushStack(returnList);
        }
    }
    
    [Function("waypoint")]
    public class FunctionWaypoint : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string pointName = shared.Cpu.PopValue().ToString();

            WaypointManager wpm = WaypointManager.Instance();
            if (wpm == null) // When zero waypoints exist, there might not even be a waypoint manager.
            {
                shared.Cpu.PushStack(null);
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

            shared.Cpu.PushStack(new WaypointValue(point, shared));
        }
    }    

    [Function("transferall")]
    public class FunctionTransferAll : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var transferTo = shared.Cpu.PopValue();
            var transferFrom = shared.Cpu.PopValue();
            var resourceName = shared.Cpu.PopValue().ToString();

            var resourceInfo = TransferManager.ParseResource(resourceName);
            if (resourceInfo == null)
            {
                throw new KOSInvalidArgumentException("TransferAll", "Resource",
                    resourceName + " was not found in the resource list");
            }

            object toPush = shared.TransferManager.CreateTransfer(resourceInfo, transferTo, transferFrom);
            shared.Cpu.PushStack(toPush);
        }

    }

    [Function("transfer")]
    public class FunctionTransfer : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var amount = shared.Cpu.PopValue();
            var transferTo = shared.Cpu.PopValue();
            var transferFrom = shared.Cpu.PopValue();
            var resourceName = shared.Cpu.PopValue().ToString();

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
                shared.Cpu.PushStack(toPush);
            }
        }
    }
}
