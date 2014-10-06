using UnityEngine;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    public class RgbaColor : Structure
    {
        private Color color;

        public RgbaColor( float red, float green, float blue, float alpha = (float) 1.0 ):this()
        {
            color = new Color(red,green,blue,alpha);
        }
        public RgbaColor( RgbaColor copyFrom ):this()
        {
            color = copyFrom.color;
        }
        private RgbaColor()
        {
            AddSuffix(new [] { "R", "RED" } , new SetSuffix<Color,float>(color, model => model.r, (model, value) => model.r = value));
            AddSuffix(new [] { "G", "GREEN" } , new SetSuffix<Color,float>(color, model => model.g, (model, value) => model.g = value));
            AddSuffix(new [] { "B", "BLUE" } , new SetSuffix<Color,float>(color, model => model.b, (model, value) => model.b = value));
            AddSuffix(new [] { "A", "ALPHA" } , new SetSuffix<Color,float>(color, model => model.a, (model, value) => model.a = value));
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
