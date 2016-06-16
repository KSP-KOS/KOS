using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.AddOns
{
    public class kOSAddonAttribute : Attribute
    {
        public string Identifier { get; set; }

        public kOSAddonAttribute(string identifier)
        {
            Identifier = identifier;
        }
    }
}
