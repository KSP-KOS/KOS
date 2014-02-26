using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Suffixed
{
    public interface IIndexable
    {
        object GetIndex(int index);
        void SetIndex(int index, object value);
    }
}
