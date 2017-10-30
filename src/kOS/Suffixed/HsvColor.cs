using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("HSVA")]
    public class HsvColor : RgbaColor
    {
        private float hue;
        private float saturation;
        private float hsvValue;

        public HsvColor(float hue, float saturation, float value, float alpha = 1.0f)
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
            AddSuffix(new[] { "H", "HUE" }, new ClampSetSuffix<ScalarValue>(() => hue, value => { hue = value; ReconcileHsvToRgb(); }, 0, 255));
            AddSuffix(new[] { "S", "SATURATION" }, new ClampSetSuffix<ScalarValue>(() => saturation, value => { saturation = value; ReconcileHsvToRgb(); }, 0, 255));
            AddSuffix(new[] { "V", "VALUE" }, new ClampSetSuffix<ScalarValue>(() => hsvValue, value => { hsvValue = value; ReconcileHsvToRgb(); }, 0, 255));
        }

        // Converts an RGB color to an HSV color.
        private void ReconcileRgbToHsv()
        {
            double h = 0, s;

            double min = System.Math.Min(System.Math.Min(Red, Green), Blue);
            double v = System.Math.Max(System.Math.Max(Red, Green), Blue);
            double delta = v - min;

            if (v == 0.0)
            {
                s = 0;
            }
            else
                s = delta / v;

            if (s == 0)
                h = 0.0f;
            else
            {
                if (Red == v)
                    h = (Green - Blue) / delta;
                else if (Green == v)
                    h = 2 + (Blue - Red) / delta;
                else if (Blue == v)
                    h = 4 + (Red - Green) / delta;

                h *= 60;
                if (h < 0.0)
                    h = h + 360;
            }

            hue = (float)h;
            saturation = (float)s;
            hsvValue = (float)v / 255;
        }

        // Converts an HSV color to an RGB color.
        private void ReconcileHsvToRgb()
        {
            double r, g, b;

            if (saturation == 0)
            {
                r = hsvValue;
                g = hsvValue;
                b = hsvValue;
            }
            else
            {
                if (hue == 360)
                    hue = 0;
                else
                    hue = hue / 60;

                var i = (int)(hue);
                double f = hue - i;

                double p = hsvValue * (1.0 - saturation);
                double q = hsvValue * (1.0 - (saturation * f));
                double t = hsvValue * (1.0 - (saturation * (1.0f - f)));

                switch (i)
                {
                    case 0:
                        r = hsvValue;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = hsvValue;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = hsvValue;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = hsvValue;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = hsvValue;
                        break;

                    default:
                        r = hsvValue;
                        g = p;
                        b = q;
                        break;
                }
            }

            Red = (float)r;
            Green = (float)g;
            Blue = (float)b;
        }
    }
}