namespace kOS.Utilities
{
    public class Flushable<T>
    {
        private T value;
        private bool stale;

        public Flushable(T value)
        {
            this.value = value;
        }

        public Flushable() { }

        public T Value
        {
            get
            {
                return value;
            }
            set
            {
                stale = true;
                this.value = value;
            }
        }

        public bool IsStale { get { return stale; } }

        public T FlushValue
        {
            get
            {
                stale = false;
                return value;
            }
        }

        public static explicit operator Flushable<T>(T value)
        {
          return new Flushable<T>(value);
        }
        public static explicit operator T(Flushable<T> value)
        {
          return value.Value;
        }
    }
}