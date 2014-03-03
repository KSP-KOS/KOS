using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public interface IUpdateObserver
    {
        void Update(double deltaTime);
    }
}
