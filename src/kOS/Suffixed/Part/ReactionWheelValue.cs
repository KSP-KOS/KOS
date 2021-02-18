using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed.PartModuleField;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("ReactionWheel")]
    public class ReactionWheelValue : PartValue
    {
        private readonly ModuleReactionWheel module;

        /// <summary>
        /// Do not call! VesselTarget.ConstructPart uses this, would use `friend VesselTarget` if this was C++!
        /// </summary>
        internal ReactionWheelValue(SharedObjects shared, global::Part part, PartValue parent, DecouplerValue decoupler, ModuleReactionWheel module)
            : base(shared, part, parent, decoupler)
        {
            this.module = module;
            RegisterInitializer(RCSInitializeSuffixes);
        }
        private void RCSInitializeSuffixes()
        {
            AddSuffix("AUTHORITYLIMITER", new SetSuffix<ScalarValue>(() => module.authorityLimiter, value => module.authorityLimiter = Math.Max(Math.Min(value, 0), 100), "Sets the authority limiter for this reaction wheel."));
            AddSuffix("WHEELSTATE", new Suffix<StringValue>(() => module.wheelState.ToString().ToUpper(), "The status of the reaction wheel: ACTIVE, DISABLED or BROKEN."));
            AddSuffix("MAXTORQUE", new Suffix<Vector>(() => new Vector(module.PitchTorque, module.YawTorque, module.RollTorque), "A vector representing max torque force over (pitch, yaw, roll)."));
            AddSuffix("TORQUERESPONSESPEED", new Suffix<ScalarValue>(() => module.torqueResponseSpeed));
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            var vessel = VesselTarget.CreateOrGetExisting(sharedObj);
            foreach (var part in parts)
            {
                foreach (var module in part.Modules)
                {
                    if (module is ModuleReactionWheel)
                    {
                        toReturn.Add(vessel[part]);
                        // Only add each part once
                        break;
                    }
                }
            }
            return toReturn;
        }
    }
}
