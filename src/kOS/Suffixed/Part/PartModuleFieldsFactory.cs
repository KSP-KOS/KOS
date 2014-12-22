/*
 * Created by SharpDevelop.
 * User: Dunbaratu
 * Date: 11/5/2014
 * Time: 3:13 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed.Part
{
    /// <summary>
    /// Description of PartModuleFieldFactory.
    /// </summary>
    public class PartModuleFieldsFactory 
    {
        public static ListValue Construct(IEnumerable<global::PartModule> modules, SharedObjects shared)
        {
            var list = new List<PartModuleFields>();
            foreach (var mod in modules)
            {
                list.Add(Construct(mod, shared));
            }
            return ListValue.CreateList(list);
        } 

        public static PartModuleFields Construct(global::PartModule mod, SharedObjects shared)
        {
            // This seems pointlessly do-nothing for a special factory now,
            // but it's here so that it can possibly become more
            // sophisticated later if need be:
            return new PartModuleFields(mod, shared);
        }
    }
}