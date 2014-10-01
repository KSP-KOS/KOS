using UnityEngine;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class RgbaColor : Structure
    {
        private Color color;

        public RgbaColor( float red, float green, float blue, float alpha = (float) 1.0 )
        {
            color = new Color(red,green,blue,alpha);
        }
        public RgbaColor( RgbaColor copyFrom )
        {
            color = copyFrom.color;
        }
        
        public Color Color()
        {
            return color;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "R":
                case "RED":
                    return (double) color.r;
                case "G":
                case "GREEN":
                    return (double) color.g;
                case "B":
                case "BLUE":
                    return (double) color.b;
                case "A":
                case "ALPHA":
                    return (double) color.a;
            }

            return base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "R":
                case "RED":
                    color.r = System.Convert.ToSingle(value);
                    return true;
                case "G":
                case "GREEN":
                    color.g = System.Convert.ToSingle(value);
                    return true;
                case "B":
                case "BLUE":
                    color.b = System.Convert.ToSingle(value);
                    return true;
                case "A":
                case "ALPHA":
                    color.a = System.Convert.ToSingle(value);
                    return true;
            }

            return base.SetSuffix(suffixName,value);
        }

        public override string ToString()
        {
            return "RGBA(" + color.r + ", " + color.g + ", " + color.b + ", " + color.a + ")";
        }

    }
}
