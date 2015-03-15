using kOS.Safe.Utilities;

namespace kOS.Safe.Encapsulation.Suffixes
{
    public class ClampSetSuffix<TValue> : SetSuffix<TValue>
    {
        private readonly double min;
        private readonly double max;
        private readonly float stepIncrement;

        public ClampSetSuffix(SuffixGetDlg<TValue> getter, SuffixSetDlg<TValue> setter, double min, double max, float stepIncrement, string description = "") 
            : this(getter, setter, min, max, description)
        {
            this.stepIncrement = stepIncrement;
        }

        public ClampSetSuffix(SuffixGetDlg<TValue> getter, SuffixSetDlg<TValue> setter, double min, double max, string description = "")
            : base(getter, setter, description)
        {
            this.min = min;
            this.max = max;
        }

        public override void Set(object value)
        {
            //HACK, this is assumes the value parses as a double
            var dblValue = double.Parse(value.ToString());

            base.Set(System.Math.Abs(stepIncrement) < 0.0001
                ? Math.Clamp(dblValue, min, max)
                : Math.ClampToIndent(dblValue, min, max, stepIncrement));
        }
    }
}