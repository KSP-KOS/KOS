using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Bindings
{
    public class kOSBinding : Attribute
    {
        public string[] Contexts;
        public kOSBinding(params string[] contexts) { Contexts = contexts; }
    }
}