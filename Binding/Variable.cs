namespace kOS.Binding
{
    public class Variable
    {
        public virtual object Value { get; set; }

        public Variable()
        {
            Value = 0.0f;
        }

        public virtual object GetSubValue(string svName)
        {
            return 0.0f;
        }
    }
}
