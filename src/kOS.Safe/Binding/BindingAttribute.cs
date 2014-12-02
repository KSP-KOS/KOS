using System;

namespace kOS.Safe.Binding
{
    public class BindingAttribute : Attribute
    {
        public string[] Contexts { get; set; }
        public BindingAttribute(params string[] contexts) { Contexts = contexts; }
    }
}