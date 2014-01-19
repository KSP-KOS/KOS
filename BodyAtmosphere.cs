using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace kOS
{
    public class BodyAtmosphere : SpecialValue
    {

        protected string bodyName;
        protected double scale;
        protected float height;
        protected bool exists;
        protected bool oxygen;

        public BodyAtmosphere(CelestialBody b)
        {
            bodyName = b.bodyName;

            exists = b.atmosphere;

            scale = exists ? b.atmosphereScaleHeight : 0;
            height = exists ? b.maxAtmosphereAltitude : 0;
            oxygen = exists ? b.atmosphereContainsOxygen : false;
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "BODY") return bodyName;
            if (suffixName == "EXISTS") return exists;
            if (suffixName == "HASOXYGEN") return oxygen;
            if (suffixName == "SCALE") return scale;
            if (suffixName == "HEIGHT") return height;

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return "BODYATMOSPHERE(\"" + bodyName + "\")";
        }
    }
}