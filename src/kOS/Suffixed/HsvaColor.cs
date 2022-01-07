using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Serialization;
using kOS.Safe;
using UnityEngine;
using System;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("HSVA")]
    public class HsvaColor : RgbaColor
    {
        private const string DumpH = "H";
        private const string DumpS = "S";
        private const string DumpV = "V";
        private const string DumpA = "A";
        private float hue;
        private float saturation;
        private float hsvValue;

        public HsvaColor(float hue, float saturation, float value, float alpha = 1.0f)
        {
            this.hue = hue;
            this.saturation = saturation;
            hsvValue = value;
            Alpha = alpha;

            InitAfterSettingFields();
        }

        private void InitAfterSettingFields()
        {
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

        public override Dump Dump(DumperState s)
        {
            DumpDictionary dump = new DumpDictionary(this.GetType());

            dump.Add(DumpH, hue);
            dump.Add(DumpS, saturation);
            dump.Add(DumpV, hsvValue);
            dump.Add(DumpA, Alpha);

            return dump;
        }

        [DumpDeserializer]
        public static new HsvaColor CreateFromDump(DumpDictionary d, SafeSharedObjects shared)
        {
            double h = d.GetDouble(DumpH);
            double s = d.GetDouble(DumpS);
            double v = d.GetDouble(DumpV);
            double a = d.GetDouble(DumpA);

            return new HsvaColor((float)h, (float)s, (float)v, (float)a);
        }

        [DumpPrinter]
        public static new void Print(DumpDictionary d, IndentedStringBuilder sb)
        {
            double h = d.GetDouble(DumpH);
            double s = d.GetDouble(DumpS);
            double v = d.GetDouble(DumpV);
            double a = d.GetDouble(DumpA);

            sb.Append(string.Format("HSVA({0}, {1}, {2}, {3})", h, s, v, a));
        }
    }
}