
using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class RgbaColor : SpecialValue
    {
        private Color _color;

        public RgbaColor( float red, float green, float blue, float alpha = (float) 1.0 )
        {
            _color = new Color(red,green,blue,alpha);
        }
        public RgbaColor( RgbaColor copyFrom )
        {
            _color = copyFrom._color;
        }
        public Color color()
        {
            return _color;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "R":
                case "RED":
                    return (double) _color.r;
                case "G":
                case "GREEN":
                    return (double) _color.g;
                case "B":
                case "BLUE":
                    return (double) _color.b;
                case "A":
                case "ALPHA":
                    return (double) _color.a;
            }

            return base.GetSuffix(suffixName);
        }
        
        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "R":
                case "RED":
                    _color.r = (float) value;
                    return true;
                case "G":
                case "GREEN":
                    _color.g = (float) value;
                    return true;
                case "B":
                case "BLUE":
                    _color.b = (float) value;
                    return true;
                case "A":
                case "ALPHA":
                    _color.a = (float) value;
                    return true;
            }

            return base.SetSuffix(suffixName,value);
        }

        public override string ToString()
        {
            return "RGBA(" + _color.r + ", " + _color.g + ", " + _color.b + ", " + _color.a + ")";
        }

    }
}
