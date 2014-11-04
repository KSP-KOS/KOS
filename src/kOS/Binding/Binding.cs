namespace kOS.Binding
{
    public class Binding
    {
        protected SharedObjects Shared { get; set; }
        public virtual void AddTo(SharedObjects shared) { }
        public virtual void Update() { }
    }
}
