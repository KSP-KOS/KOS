using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Function;
using kOS.Suffixed;
using kOS.Utilities;

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
    // be done with just one fuction that counts how many args it
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
    
    [Function("constant")]
    public class FunctionConstant : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var constants = new ConstantValue();
            shared.Cpu.PushStack(constants);
        }
    }
}
