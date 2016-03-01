using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using UnityEngine;
using Math = kOS.Safe.Utilities.Math;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("RGBA")]
    public class RgbaColor : Structure
    {
        protected float Red { get; set; }

        protected float Green { get; set; }

        protected float Blue { get; set; }

        protected float Alpha { get; set; }

        protected RgbaColor()
        {
            InitializeSuffixColor();
        }

        public RgbaColor(float red, float green, float blue, float alpha = (float) 1.0)
            : this()
        {
            Red = Math.Clamp(red, 0, 255);
            Green = Math.Clamp(green, 0, 255);
            Blue = Math.Clamp(blue, 0, 255);
            Alpha = Math.Clamp(alpha, 0, 255);
        }

        public RgbaColor(RgbaColor copyFrom)
            : this()
        {
            SafeHouse.Logger.Log(" --RgbaColor.Construct-- ");
            Red = copyFrom.Red;
            Green = copyFrom.Green;
            Blue = copyFrom.Blue;
            Alpha = copyFrom.Alpha;
        }

        private void InitializeSuffixColor()
        {
            AddSuffix(new[] { "R", "RED" }, new ClampSetSuffix<ScalarValue>(() => Red, value => { Red = value; Recalculate(); }, 0, 255));
            AddSuffix(new[] { "G", "GREEN" }, new ClampSetSuffix<ScalarValue>(() => Green, value => { Green = value; Recalculate(); }, 0, 255));
            AddSuffix(new[] { "B", "BLUE" }, new ClampSetSuffix<ScalarValue>(() => Blue, value => { Blue = value; Recalculate(); }, 0, 255));
            AddSuffix(new[] { "A", "ALPHA" }, new ClampSetSuffix<ScalarValue>(() => Alpha, value => { Alpha = value; Recalculate(); }, 0, 1));
            AddSuffix(new[] { "HTML", "HEX" }, new NoArgsSuffix<StringValue>(ToHexNotation, "Returns a string representing the color in HTML, i.e. \"#ff0000\".  Ignores transparency (alpha) information."));
        }

        protected virtual void Recalculate()
        {
            // No-op
        }

        public Color Color
        {
            get
            {
                return new Color(Red, Green, Blue, Alpha);
            }
        }

        public override string ToString()
        {
            return string.Format("RGBA({0}, {1}, {2}, {3})", Red, Green, Blue, Alpha);
        }

        /// <summary>
        /// Returns a string representing the Hex color code "#rrggbb" format
        /// for the color.  (i.e. RED is "#ff0000").  Note that this cannot represent
        /// the transparency (alpha) information, and will treat the color as if it was
        /// fully opaque regardless of whether it really is or not.  Although there have
        /// been some extensions to the HTML specification that added a fourth byte for
        /// alpha information, i.e. "#80ff0000" would be a semitransparent red, those never
        /// got accepted as standard and remain special proprietary extensions.
        /// </summary>
        /// <returns>A color in hexadecimal notation</returns>
        public StringValue ToHexNotation()
        {
            var redByte = (byte)Mathf.Min(255, (int)(Red * 255f));
            var greenByte = (byte)Mathf.Min(255, (int)(Green * 255f));
            var blueByte = (byte)Mathf.Min(255, (int)(Blue * 255f));
            return string.Format("#{0:x2}{1:x2}{2:x2}", redByte, greenByte, blueByte);
        }
    }
}