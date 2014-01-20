using System;

namespace kOS.Binding
{
    public class KOSBinding : Attribute
    {
        public string[] Contexts;
        public KOSBinding(params string[] contexts) { Contexts = contexts; }
    }
}