using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.AddOns
{
    [AssemblyWalk(AttributeType = typeof(kOSAddonAttribute), InherritedType = typeof(Suffixed.Addon), StaticRegisterMethod = "RegisterMethod")]
    public class AddonManager
    {
        public Dictionary<string, Suffixed.Addon> AllAddons { get; private set; }
        private static readonly Dictionary<kOSAddonAttribute, Type> rawAttributes = new Dictionary<kOSAddonAttribute, Type>();

        public AddonManager(SharedObjects sharedObj)
        {
            AllAddons = new Dictionary<string, Suffixed.Addon>(StringComparer.OrdinalIgnoreCase);
            foreach (kOSAddonAttribute attr in rawAttributes.Keys)
            {
                AllAddons.Add(attr.Identifier, (Suffixed.Addon)Activator.CreateInstance(rawAttributes[attr], sharedObj));
            }
        }

        public static void RegisterMethod(Attribute attr, Type type)
        {
            var addonAttr = attr as kOSAddonAttribute;
            if (addonAttr != null)
            {
                rawAttributes[addonAttr] = type;
            }
            
        }

        public static string GetAddonIdentifier(Suffixed.Addon addon)
        {
            Type t = addon.GetType();
            return rawAttributes.Where(e => e.Value.Equals(t)).Select(e => e.Key.Identifier).FirstOrDefault();
        }
    }
}
