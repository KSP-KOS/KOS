using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("HSVA")]
    public class HsvaColor : RgbaColor
    {
        private float hue;
        private float saturation;
        private float hsvValue;

        public HsvaColor(float hue, float saturation, float value, float alpha = 1.0f)
        {
            this.hue = hue;
            this.saturation = saturation;
            hsvValue = value;
            Alpha = alpha;

            InitializeSuffixColor();
            ReconcileHsvToRgb();
        }

        public override string ToString()
        {
            return string.Format("HSVA({0}, {1}, {2}, {3})", hue, saturation, hsvValue, Alpha);
        }

        protected override void Recalculate()
        {
            base.Recalculate();
            ReconcileRgbToHsv();
        }

        private void InitializeSuffixColor()
        {
            AddSuffix(new[] { "H", "HUE" }, new ClampSetSuffix<ScalarValue>(() => hue, value => { hue = value; ReconcileHsvToRgb(); }, 0, 1));
            AddSuffix(new[] { "S", "SATURATION" }, new ClampSetSuffix<ScalarValue>(() => saturation, value => { saturation = value; ReconcileHsvToRgb(); }, 0, 1));
            AddSuffix(new[] { "V", "VALUE" }, new ClampSetSuffix<ScalarValue>(() => hsvValue, value => { hsvValue = value; ReconcileHsvToRgb(); }, 0, 1));
        }

        private void ReconcileHsvToRgb()
        {
            Color newColor = Color.HSVToRGB(hue, saturation, hsvValue);
            Red = newColor.r;
            Blue = newColor.b;
            Green = newColor.g;
        }
        
        private void ReconcileRgbToHsv()
        {
            UnityEngine.Color.RGBToHSV(Color, out float newHue,  out float newSaturation, out float newValue);
            hue = newHue;
            saturation = newSaturation;
            hsvValue = newValue;
        }
    }
}