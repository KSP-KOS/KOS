using System;
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
            AddSuffix("HTML", new NoArgsSuffix<string>(ToHTMLString, "Returns a string representing the color in HTML, i.e. \"#ff0000\".  Ignores transparency (alpha) information."));
        }
        
        public Color Color()
        {
            return new Color(color.r,color.g,color.b,color.a); 
        }

        public override string ToString()
        {
            return "RGBA(" + color.r + ", " + color.g + ", " + color.b + ", " + color.a + ")";
        }
        
        /// <summary>
        /// Returns a string represnting the HTML color code "#rrggbb" format
        /// for the color.  (i.e. RED is "#ff0000").  Note that this cannot represent
        /// the transparency (alpha) information, and will treat the color as if it was
        /// fully opaque regardless of whether it really is or not.  Although there have
        /// been some extensions to the HTML specification that added a fourth byte for
        /// alpha information, i.e. "#80ff0000" would be a semitransparent red, those never
        /// got accepted as standard and remain special proprietary extensions.
        /// </summary>
        /// <returns>The html spec string returned, using lowercase lettering for the alpha digits</returns>
        public string ToHTMLString()
        {
            byte redByte   = (byte)(Mathf.Min(255, (int)(color.r * 255f)));
            byte greenByte = (byte)(Mathf.Min(255, (int)(color.g * 255f)));
            byte blueByte  = (byte)(Mathf.Min(255, (int)(color.b * 255f)));
            return String.Format("#{0:x2}{1:x2}{2:x2}", redByte, greenByte, blueByte);
        }

    }
}
