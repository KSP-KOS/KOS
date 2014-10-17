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
            AddSuffix(new [] { "R", "RED" } , new SetSuffix<float>(() => color.r, value => color.r = value));
            AddSuffix(new [] { "G", "GREEN" } , new SetSuffix<float>(() => color.g, value => color.g = value));
            AddSuffix(new [] { "B", "BLUE" } , new SetSuffix<float>(() => color.b, value => color.b = value));
            AddSuffix(new [] { "A", "ALPHA" } , new SetSuffix<float>(() => color.a, value => color.a = value));
        }
        
        public Color Color()
        {
            return new Color(color.r,color.g,color.b,color.a); 
        }

        public override string ToString()
        {
            return "RGBA(" + color.r + ", " + color.g + ", " + color.b + ", " + color.a + ")";
        }

    }
}
