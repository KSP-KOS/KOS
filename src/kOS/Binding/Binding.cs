namespace kOS.Binding
{
    public abstract class Binding : kOS.Safe.Binding.SafeBinding
    {
        public override void AddTo(Safe.SharedObjects shared)
        {
            AddTo(shared as SharedObjects);
        }
        public abstract void AddTo(SharedObjects shared);
        public virtual void Update() { }
    }
}
