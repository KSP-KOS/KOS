using kOS.Safe.Utilities;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class ClampSetSuffix<TValue> : SetSuffix<TValue> 
    {
        private readonly double min;
        private readonly double max;

        public ClampSetSuffix(SuffixGetDlg<TValue> getter, SuffixSetDlg<TValue> setter, double min, double max, string description = "")
            : base(getter, setter, description)
        {
            this.min = min;
            this.max = max;
        }

        public override void Set(object value)
        {
            //HACK, this is assumes the value parses as a double 
            base.Set(Math.Clamp(double.Parse(value.ToString()), min, max));
        }
    }
}