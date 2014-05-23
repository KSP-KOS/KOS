namespace kOS.Binding
{
    public class Binding
    {
        protected SharedObjects _shared;

        public virtual void AddTo(SharedObjects shared) { }
        public virtual void Update() { }
    }
}
