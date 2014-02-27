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

            Node result = new Node(time, radial, normal, prograde);
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

            GeoCoordinates result = new GeoCoordinates(shared.Vessel, latitude, longitude);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("vessel")]
    public class FunctionVessel : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string vesselName = shared.Cpu.PopValue().ToString();
            VesselTarget result = new VesselTarget(VesselUtils.GetVesselByName(vesselName, shared.Vessel), shared.Vessel);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("body")]
    public class FunctionBody : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string bodyName = shared.Cpu.PopValue().ToString();
            BodyTarget result = new BodyTarget(bodyName, shared.Vessel);
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
