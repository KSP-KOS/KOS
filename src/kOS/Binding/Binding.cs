namespace kOS.Binding
{
    public abstract class Binding : kOS.Safe.Binding.SafeBindingBase
    {
        public override void AddTo(Safe.SafeSharedObjects shared)
        {
            AddTo(shared as SharedObjects);
        }
        public abstract void AddTo(SharedObjects shared);
    }
}
