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
        private static string DumpH = "H";
        private static string DumpS = "S";
        private static string DumpV = "V";
        private static string DumpA = "A";
        private float hue;
        private float saturation;
        private float hsvValue;

        protected HsvaColor()
        {
            // InitAfterSettinFields() not called here, which is why this must
            // not be made public.  It's just bere because the IDumper
            // system needs it for how CreateFromDump() works.
        }

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

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static HsvaColor CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new HsvaColor();
            newObj.LoadDump(d);
            return newObj;
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
        public override Dump Dump()
        {
            DumpWithHeader dump = new DumpWithHeader
            {
                {DumpH, hue },
                {DumpS, saturation },
                {DumpV, hsvValue },
                {DumpA, Alpha }
            };
            return dump;
        }

        public override void LoadDump(Dump dump)
        {
            hue = (float)Convert.ToDouble(dump[DumpH]);
            saturation = (float)Convert.ToDouble(dump[DumpS]);
            hsvValue = (float)Convert.ToDouble(dump[DumpV]);
            Alpha = (float)Convert.ToDouble(dump[DumpA]);
            InitAfterSettingFields();
        }
    }
}