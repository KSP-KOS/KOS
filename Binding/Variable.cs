namespace kOS.Binding
{
    public class Variable
    {
        private object value = 0.0f;

        public virtual object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public virtual object GetSubValue(string svName)
        {
            return 0.0f;
        }
    }
}