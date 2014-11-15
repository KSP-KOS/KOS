namespace kOS.Binding
{
    public abstract class Binding
    {
        public virtual void AddTo(SharedObjects shared) { }
        public virtual void Update() { }
    }
}
