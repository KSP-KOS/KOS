using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Suffixed;
using kOS.Utilities;

namespace kOS.Function
{
    [FunctionAttribute("node")]
    public class FunctionNode : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double prograde = GetDouble(shared.Cpu.PopValue());
            double normal = GetDouble(shared.Cpu.PopValue());
            double radial = GetDouble(shared.Cpu.PopValue());
            double time = GetDouble(shared.Cpu.PopValue());

            Node result = new Node(time, radial, normal, prograde, shared);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("v")]
    public class FunctionVector : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double z = GetDouble(shared.Cpu.PopValue());
            double y = GetDouble(shared.Cpu.PopValue());
            double x = GetDouble(shared.Cpu.PopValue());

            Vector result = new Vector(x, y, z);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("r")]
    public class FunctionRotation : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double roll = GetDouble(shared.Cpu.PopValue());
            double yaw = GetDouble(shared.Cpu.PopValue());
            double pitch = GetDouble(shared.Cpu.PopValue());

            Direction result = new Direction(new Vector3d(pitch, yaw, roll), true);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("q")]
    public class FunctionQuaternion : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double angle = GetDouble(shared.Cpu.PopValue());
            double roll = GetDouble(shared.Cpu.PopValue());
            double yaw = GetDouble(shared.Cpu.PopValue());
            double pitch = GetDouble(shared.Cpu.PopValue());

            Direction result = new Direction(new UnityEngine.Quaternion((float)pitch, (float)yaw, (float)roll, (float)angle));
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("latlng")]
    public class FunctionLatLng : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double longitude = GetDouble(shared.Cpu.PopValue());
            double latitude = GetDouble(shared.Cpu.PopValue());

            GeoCoordinates result = new GeoCoordinates(shared, latitude, longitude);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("vessel")]
    public class FunctionVessel : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string vesselName = shared.Cpu.PopValue().ToString();
            VesselTarget result = new VesselTarget(VesselUtils.GetVesselByName(vesselName, shared.Vessel), shared);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("body")]
    public class FunctionBody : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string bodyName = shared.Cpu.PopValue().ToString();
            BodyTarget result = new BodyTarget(bodyName, shared);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("bodyatmosphere")]
    public class FunctionBodyAtmosphere : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string bodyName = shared.Cpu.PopValue().ToString();
            BodyAtmosphere result = new BodyAtmosphere(VesselUtils.GetBodyByName(bodyName));
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("heading")]
    public class FunctionHeading : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            double pitchAboveHorizon = GetDouble(shared.Cpu.PopValue());
            double degreesFromNorth = GetDouble(shared.Cpu.PopValue());

            Vessel currentVessel = shared.Vessel;
            var q = UnityEngine.Quaternion.LookRotation(VesselUtils.GetNorthVector(currentVessel), currentVessel.upAxis);
            q *= UnityEngine.Quaternion.Euler(new UnityEngine.Vector3((float)-pitchAboveHorizon, (float)degreesFromNorth, 0));

            Direction result = new Direction(q);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("list")]
    public class FunctionList : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            ListValue listValue = new ListValue();
            shared.Cpu.PushStack(listValue);
        }
    }

    [FunctionAttribute("rgb")]
    public class FunctionRgb : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            float b = (float) GetDouble(shared.Cpu.PopValue());
            float g = (float) GetDouble(shared.Cpu.PopValue());
            float r = (float) GetDouble(shared.Cpu.PopValue());
            shared.Cpu.PushStack( new RgbaColor(r,g,b) );
        }
    }

    [FunctionAttribute("rgba")]
    public class FunctionRgba : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            float a = (float) GetDouble(shared.Cpu.PopValue());
            float b = (float) GetDouble(shared.Cpu.PopValue());
            float g = (float) GetDouble(shared.Cpu.PopValue());
            float r = (float) GetDouble(shared.Cpu.PopValue());
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
    [FunctionAttribute("vecdraw")]
    public class FunctionVecDrawNull : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            VectorRenderer vRend = new VectorRenderer( shared.UpdateHandler );
            vRend.SetShow( false );
            
            shared.Cpu.PushStack( vRend );
        }
    }

    [FunctionAttribute("vecdrawargs")]
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

            VectorRenderer vRend = new VectorRenderer( shared.UpdateHandler );
            vRend.vec   = vec;
            vRend.start = start;
            vRend.rgba  = rgba;
            vRend.scale = scale;
            vRend.SetLabel( str );
            vRend.SetShow( show );
            
            shared.Cpu.PushStack( vRend );
        }
    }

    [FunctionAttribute("constant")]
    public class FunctionConstant : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            ConstantValue constants = new ConstantValue();
            shared.Cpu.PushStack(constants);
        }
    }
}
