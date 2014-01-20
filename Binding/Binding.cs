using kOS.Context;

namespace kOS.Binding
{
    public class Binding
    {
        public virtual void AddTo(BindingManager manager) { }

        public virtual void Update(float time) { }
    }
}
