using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kOS
{
    public class VesselTarget : SpecialValue
    {
        public ExecutionContext context;
        public Vessel target;

        public VesselTarget(Vessel target, ExecutionContext context)
        {
            this.context = context;
            this.target = target;
        }

        public bool IsInRange(float range)
        {
            if ((float)GetDistance() <= range) return true;

            return false;
        }

        public double GetDistance()
        {
            return Vector3d.Distance(context.Vessel.GetWorldPos3D(), target.GetWorldPos3D());
        }

        public override string ToString()
        {
            return "VESSEL(\"" + target.vesselName + "\")";
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "DISTANCE") return (float)GetDistance();
            if (suffixName == "DIRECTION") 
            {
                var vector = (target.GetWorldPos3D() - context.Vessel.GetWorldPos3D());
                return new Direction(vector, false);
            }

            if (suffixName == "BEARING")
            {
                return VesselUtils.GetTargetBearing(context.Vessel, target);
            }

            if (suffixName == "HEADING")
            {
                return VesselUtils.GetTargetHeading(context.Vessel, target);
            }

            return base.GetSuffix(suffixName);
        }
    }
}
