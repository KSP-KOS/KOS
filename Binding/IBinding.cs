namespace kOS.Binding
{
    public interface IBinding : IUpdatable
    {
        void BindTo(IBindingManager manager);
    }
}
