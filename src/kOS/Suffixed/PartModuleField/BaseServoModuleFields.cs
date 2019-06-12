using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using kOS.Safe.Utilities;

// ABOUT THIS "using Expansions.Serenity;" LINE:
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// NOTE: "Serenity" means "Breaking Ground DLC", apparently.  It must have been
// some internal name for the project.
// Also, this namespace and its classes appear to ship with stock KSP 1.7.1's DLLs.
// So it appears to be safe for us to have code that references the DLC's
// namespace and its classes even though some users won't have the DLC.  Lacking
// the DLC merely means you won't have any instances of these classes because none
// of your parts use them, rather than lacking the class definitions themselves.)
using Expansions.Serenity;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed.PartModuleField
{
    [Safe.Utilities.KOSNomenclature("BaseServo")]
    public class BaseServoModuleFields : PartModuleFields
    {
        private readonly BaseServo servo;
        public BaseServoModuleFields(BaseServo servo, SharedObjects sharedObj) : base(servo, sharedObj)
        {
            this.servo = servo;
        }

        /// <summary>
        /// Get the UI_Controls on a KSPField which are user editable.
        /// BaseServo modules will sometimes use an alternative rule for
        /// how their range limits work, and this checks to see if that
        /// alternative is in play instead of the normal way.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        override protected List<UI_Control> GetFieldControls(BaseField field)
        {
            List<UI_Control> controls = base.GetFieldControls(field);
            Console.WriteLine("eraseme:   servo.useLimits = true.");
            AxisFieldLimit limitOverride = servo.GetAxisFieldLimit(field.name);
            if (limitOverride != null)
            {
                Console.WriteLine("eraseme:     limitOverride != null.");
                // This is the condition in which the DLC servo's KSPField's range values are
                // utter lies.  (Example: In a case like a Piston who's user interface lets
                // players pick any "Target Extension" values from 0 meters to 2.4 meters, down to
                // the exact centimeter, the UI_FloatRange control for "Target Extension" is lying,
                // reporting FloatRange values as if its valid values were actually 0 to 100 meters
                // and must be rounded to the nearest 1 meter.  That effectively meant kOS could only
                // set that slider to 3 distinct values: 0, 1, and 2.  Anything else got rounded to
                // one of those, making it impossible for kOS to let a script use that slider properly.)
                //
                // When this condition happens, we need to utterly ignore the
                // actual UI_Control values that the API claims are in use, and instead
                // replace it with this overridden version.
                //
                // If you are looking at this ugly code and want to judge me - remember I had to work this all
                // out from trial and error reverse engineering.  None of this was documented for modders.
                // There couild very well be a proper API call that does this cleanly, but I don't know what it is.

                for (int idx = 0; idx < controls.Count; ++idx)
                {
                    Console.WriteLine("eraseme:      index = " + idx);
                    UI_Control control = controls[idx];
                    if (control.controlEnabled && control is UI_FloatRange || control is UI_FieldFloatRange)
                    {
                        Console.WriteLine("eraseme:        Doing the override to new limits.");
                        // I do NOT want to overwrite the actual contents of the control that is
                        // returned by the API, because I fear it's a reference to the one that is
                        // really used by the rest of the game, and I don't know what I might break
                        // by editing its values directly.  So instead I want to make a new one that
                        // mimics the actual one, with the relevant range limit fields edited.
                        // But there is no copy constructor or assignment operator for it, so
                        // if I tried to copy all the fields I'd inevitably miss a few.  Instead I will
                        // just only write to the few fields we actually use here in kOS.  This is not
                        // a general all-purpose solution for every modder because many of these fields
                        // are skipped in this copy:
                        UI_FloatRange trueRange = new UI_FloatRange();
                        trueRange.minValue = limitOverride.softLimits.x;
                        trueRange.maxValue = limitOverride.softLimits.y;
                        trueRange.stepIncrement = 0f;
                        trueRange.controlEnabled = control.controlEnabled;
                        controls[idx] = trueRange; // overwrite the bogus range info the API hands out by default.
                        Console.WriteLine(string.Format("eraseme: just overwrote with min={0}, max={1}, step={2}", ((UI_FloatRange)controls[idx]).minValue, ((UI_FloatRange)controls[idx]).maxValue, ((UI_FloatRange)controls[idx]).stepIncrement));
                    }
                }
            }
            return controls;
        }
    }
}
