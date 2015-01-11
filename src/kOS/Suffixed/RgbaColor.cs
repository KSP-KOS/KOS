using UnityEngine;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    public class RgbaColor : Structure
    {
        private Color color;

        public RgbaColor( float red, float green, float blue, float alpha = (float) 1.0 )
        {
            color = new Color(red,green,blue,alpha);
            InitializeSuffixColor();
        }
        public RgbaColor( RgbaColor copyFrom )
        {
            Safe.Utilities.Debug.Logger.Log("kOS: --RgbaColor.Construct-- ");
            color = copyFrom.color;
            InitializeSuffixColor();
        }
        private void InitializeSuffixColor()
        {
            AddSuffix(new [] { "R", "RED" } , new ClampSetSuffix<float>(() => color.r, value => color.r = value, 0, 255));
            AddSuffix(new [] { "G", "GREEN" } , new ClampSetSuffix<float>(() => color.g, value => color.g = value, 0, 255));
            AddSuffix(new [] { "B", "BLUE" } , new ClampSetSuffix<float>(() => color.b, value => color.b = value, 0, 255));
            AddSuffix(new [] { "A", "ALPHA" } , new ClampSetSuffix<float>(() => color.a, value => color.a = value, 0, 1));
        }
        
        public Color Color()
        {
            return new Color(color.r,color.g,color.b,color.a); 
        }

        public override bool KOSEquals(object other)
        {
            RgbaColor that = other as RgbaColor;
            if (that == null) return false;
            return this.color.Equals(that.color);
        } 

        public override string ToString()
        {
            return "RGBA(" + color.r + ", " + color.g + ", " + color.b + ", " + color.a + ")";
        }

    }
}
