using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed.Part;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using kOS.AddOns.RemoteTech;
using kOS.Safe.Utilities;

namespace kOS.Suffixed.PartModuleField
{
    /// <summary>
    /// An abstraction of a part module attached to a PartValue, that allows
    /// the kOS script to get access to the module's KSPFields as kOS suffix terms.
    /// each KSPField of the PartModule becomes a suffix you can get to with
    /// GetSuffix().
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("PartModule")]
    public class PartModuleFields : Structure
    {
        protected readonly PartModule partModule;
        protected readonly SharedObjects shared;

        /// <summary>
        /// Create a kOS-user variable wrapper around a KSP PartModule attached to a part.
        /// </summary>
        /// <param name="partModule">the KSP PartModule to make a wrapper for</param>
        /// <param name="shared">The omnipresent shared data</param>
        public PartModuleFields(PartModule partModule, SharedObjects shared)
        {
            this.partModule = partModule;
            this.shared = shared;

            // Overriding Structure.InitializeSuffixes() doesn't work because the base constructor calls it
            // prior to calling this constructor, and so partModule isn't set yet:
            InitializeSuffixesAfterConstruction();
        }

        public void ThrowIfNotCPUVessel()
        {
            if (partModule.vessel.id != shared.Vessel.id)
                throw new KOSWrongCPUVesselException();
        }

        /// <summary>
        /// Get the string type of the module
        /// </summary>
        /// <returns>the string type</returns>
        private string GetModuleName()
        {
            return partModule.moduleName;
        }

        public override string ToString()
        {
            var returnValue = new StringBuilder();
            returnValue.AppendLine(GetModuleName() + ", containing:");
            returnValue.AppendLine(AllThings().ToString());

            return returnValue.ToString();
        }

        /// <summary>
        /// Return true if the value given is allowed for the field given.  This uses the hints from the GUI
        /// system to decide what is and isn't allowed.  (For example if a GUI slider goes from 10 to 20 at
        /// increments of 2, then a value of 17 is not something you could achieve in the GUI, being only able
        /// to go from 16 to 18 skipping over 17, and therefore this routine would return false for trying to
        /// set it to 17).
        /// <br/><br/>
        /// Note that the value passed in may be altered (which is why it's ref) when it's not
        /// exactly legal, but it's CLOSE ENOUGH to be coerced into a legal value.  For example,
        /// you set a slider to 10.45 and the slider only allows values on the 0.5 marks like 10, 10.5, 11. etc.
        /// Rather than deny the attempt, the value will be nudged to the nearest legal value.
        /// </summary>
        /// <param name="field">Which KSPField is being checked?</param>
        /// <param name="newVal">What is the value it's being set to?  It is possible to
        ///    alter the value to an acceptable replacement which is why it passes by ref</param>
        /// <param name="except">An exception you can choose to throw if you want, or null if the value is legal.</param>
        /// <returns>Is it legal?</returns>
        private bool IsLegalValue(BaseField field, ref Structure newVal, out KOSException except)
        {
            except = null;
            bool isLegal = true;

            Type fType = field.FieldInfo.FieldType;
            object convertedVal = newVal;

            // Using TryGetFieldUIControl() to obtain the control that goes with this
            // field is from advice from TriggerAU, who gave that advice in
            // a forum post when I described the problems we were having with the servo
            // parts in Breaking Ground DLC.  (There is some kind of work being done here
            // that seems to allow one field's ranges to override another's as the servo
            // parts need to do.  This is work which doesn't seem to happen if you look at
            // the KSPField's control ranges directly):
            UI_Control control;
            if (!partModule.Fields.TryGetFieldUIControl(field.name, out control))
            {
                throw new KOSInvalidFieldValueException("Field appears to have no UI control attached so kOS refuses to let a script change it.");
            }
            if (!control.controlEnabled)
            {
                except = new KOSInvalidFieldValueException("Field is read-only");
                return false;
            }
            if (!newVal.GetType().IsSubclassOf(fType))
            {
                try
                {
                    convertedVal = Convert.ChangeType(newVal, fType);
                }
                catch (InvalidCastException)
                {
                    except = new KOSCastException(newVal.GetType(), fType);
                    return false;
                }
                catch (FormatException)
                {
                    except = new KOSCastException(newVal.GetType(), fType);
                    return false;
                }
            }

            // Some of these are subclasses of each other, so don't change this to an if/else.
            // It's a series of if's on purpose so it checks all classes the control is derived from.
            if (control is UI_Toggle)
            {
                // Seems there's nothing to check here, but maybe later there will be?
            }
            if (control is UI_Label)
            {
                except = new KOSInvalidFieldValueException("Labels are read-only objects that can't be changed");
                isLegal = false;
            }
            var vector2 = control as UI_Vector2;
            if (vector2 != null)
            {
                // I have no clue what this actually looks like in the UI?  What is a
                // user editable 2-D vector widget?  I've never seen this before.
                if (convertedVal != null)
                {
                    var vec2 = (Vector2)convertedVal;
                    if (vec2.x < vector2.minValueX || vec2.x > vector2.maxValueX ||
                        vec2.y < vector2.minValueY || vec2.y > vector2.maxValueY)
                    {
                        except = new KOSInvalidFieldValueException("Vector2 is outside of allowed range of values");
                        isLegal = false;
                    }
                }
            }
            var range = control as UI_FloatRange;
            if (range != null)
            {
                float val = Convert.ToSingle(convertedVal);
                val = KOSMath.ClampToIndent(val, range.minValue, range.maxValue, range.stepIncrement);
                convertedVal = Convert.ToDouble(val);
            }
            newVal = FromPrimitiveWithAssert(convertedVal);
            return isLegal;
        }

        protected string GetFieldName(BaseField kspField)
        {
            return kspField.guiName.Length > 0 ? kspField.guiName : kspField.name;
        }
        // Note that BaseEvent has the GUIName property which effectively does this already.
        protected string GetEventName(BaseEvent kspEvent)
        {
            return kspEvent.GUIName;
        }
        protected string GetActionName(BaseAction kspAction)
        {
            return kspAction.guiName.Length > 0 ? kspAction.guiName : kspAction.name;
        }
        /// <summary>
        /// Return a list of all the strings of all KSPfields registered to this PartModule
        /// which are currently showing on the part's RMB menu.
        /// </summary>
        /// <returns>List of all the strings field names.</returns>
        protected virtual ListValue AllFields(string formatter)
        {
            var returnValue = new ListValue();

            IEnumerable<BaseField> visibleFields = partModule.Fields.Cast<BaseField>().Where(FieldIsVisible);

            foreach (BaseField field in visibleFields)
            {
                UI_Control control;
                if ( partModule.Fields.TryGetFieldUIControl(field.name, out control))
                {
                    returnValue.Add(new StringValue(string.Format(formatter,
                                                  control.controlEnabled ? "settable" : "get-only",
                                                  GetFieldName(field).ToLower(),
                                                  Utilities.Utils.KOSType(field.FieldInfo.FieldType))));
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Return a list of all the strings of all KSPfields registered to this PartModule
        /// which are currently showing on the part's RMB menu, without formating.
        /// </summary>
        /// <returns>List of all the strings field names.</returns>
        protected virtual ListValue AllFieldNames()
        {
            var returnValue = new ListValue();

            IEnumerable<BaseField> visibleFields = partModule.Fields.Cast<BaseField>().Where(FieldIsVisible);

            foreach (BaseField field in visibleFields)
            {
                returnValue.Add(new StringValue(GetFieldName(field).ToLower()));
            }
            return returnValue;
        }

        /// <summary>
        /// Determine if the Partmodule has this KSPField on it, which is publicly
        /// usable by a kOS script at the moment:
        /// </summary>
        /// <param name="fieldName">The field to search for</param>
        /// <returns>true if it is on the PartModule, false if it is not</returns>
        public virtual BooleanValue HasField(StringValue fieldName)
        {
            return FieldIsVisible(GetField(fieldName));
        }

        /// <summary>
        /// Return the field itself that goes with the name (the BaseField, not the value).
        /// </summary>
        /// <param name="cookedGuiName">The case-insensitive guiName (or name if guiname is empty) of the field.</param>
        /// <returns>a BaseField - a KSP type that can be used to get the value, or its GUI name or its reflection info.</returns>
        protected BaseField GetField(string cookedGuiName)
        {
            // Conceptually this should be a single hit using FirstOrDefault(), because there should only
            // be one Field with the given GUI name.  But Issue #2666 forced kOS to change it to an array of hits
            // because KSP started naming two fields with the same gui name, only one of which is visible
            // at a time:
            BaseField[] allMatches = partModule.Fields.Cast<BaseField>().
                Where(field => string.Equals(GetFieldName(field), cookedGuiName, StringComparison.CurrentCultureIgnoreCase)).
                ToArray<BaseField>();
            // When KSP is *not* doing the weird thing of two fields with the same name, there's just one hit and it's simple:
            if (allMatches.Count() == 1)
                return allMatches.First();
            if (allMatches.Count() == 0)
                return null;

            // Issue #2666 is handled here.  kOS should not return the invisible field when there's
            // a visible one of the same name it could have picked instead.  Only return an invisible
            // field if there's no visible one to pick.
            BaseField preferredMatch = allMatches.FirstOrDefault(field => FieldIsVisible(field));
            return preferredMatch ?? allMatches.First();
        }

        /// <summary>
        /// Return a list of all the KSPEvents the module has in it which are currently
        /// visible on the RMB menu.
        /// </summary>
        /// <returns></returns>
        private ListValue AllEvents(string formatter)
        {
            var returnValue = new ListValue();

            IEnumerable<BaseEvent> visibleEvents = partModule.Events.Where(EventIsVisible);

            foreach (BaseEvent kspEvent in visibleEvents)
            {
                returnValue.Add(new StringValue(string.Format(formatter,
                                              "callable",
                                              GetEventName(kspEvent).ToLower(),
                                              "KSPEvent")));
            }
            return returnValue;
        }

        /// <summary>
        /// Return a list of all the KSPEvents the module has in it which are currently
        /// visible on the RMB menu, without formatting.
        /// </summary>
        /// <returns>List of Event Names</returns>
        private ListValue AllEventNames()
        {
            var returnValue = new ListValue();

            IEnumerable<BaseEvent> visibleEvents = partModule.Events.Where(EventIsVisible);

            foreach (BaseEvent kspEvent in visibleEvents)
            {
                returnValue.Add(new StringValue(GetEventName(kspEvent).ToLower()));
            }
            return returnValue;
        }

        /// <summary>
        /// Determine if the Partmodule has this KSPEvent on it, which is publicly
        /// usable by a kOS script:
        /// </summary>
        /// <param name="eventName">The event name to search for</param>
        /// <returns>true if it is on the PartModule, false if it is not</returns>
        public BooleanValue HasEvent(StringValue eventName)
        {
            return EventIsVisible(GetEvent(eventName));
        }

        /// <summary>
        /// Return the KSP BaseEvent going with the given name.
        /// </summary>
        /// <param name="cookedGuiName">The event's case-insensitive guiname (or name if guiname is empty).</param>
        /// <returns></returns>
        private BaseEvent GetEvent(string cookedGuiName)
        {
            return partModule.Events.
                FirstOrDefault(kspEvent => string.Equals(GetEventName(kspEvent), cookedGuiName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Return a list of all the KSPActions the module has in it.
        /// </summary>
        /// <returns></returns>
        private ListValue AllActions(string formatter)
        {
            var returnValue = new ListValue();

            foreach (BaseAction kspAction in partModule.Actions)
            {
                returnValue.Add(new StringValue(string.Format(formatter,
                                              "callable",
                                              GetActionName(kspAction).ToLower(),
                                              "KSPAction")));
            }
            return returnValue;
        }

        /// <summary>
        /// Return a list of all the KSPActions the module has in it, without formatting.
        /// </summary>
        /// <returns>List of Action Names</returns>
        private ListValue AllActionNames()
        {
            var returnValue = new ListValue();

            foreach (BaseAction kspAction in partModule.Actions)
            {
                returnValue.Add(new StringValue(GetActionName(kspAction).ToLower()));
            }
            return returnValue;
        }

        /// <summary>
        /// Determine if the Partmodule has this KSPAction on it, which is publicly
        /// usable by a kOS script:
        /// </summary>
        /// <param name="actionName">The action name to search for</param>
        /// <returns>true if it is on the PartModule, false if it is not</returns>
        public BooleanValue HasAction(StringValue actionName)
        {
            return partModule.Actions.Any(kspAction => string.Equals(GetActionName(kspAction), actionName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Return the KSP BaseAction going with the given name.
        /// </summary>
        /// <param name="cookedGuiName">The event's case-insensitive guiname (or name if guiname is empty).</param>
        /// <returns></returns>
        private BaseAction GetAction(string cookedGuiName)
        {
            return partModule.Actions.FirstOrDefault(kspAction => string.Equals(GetActionName(kspAction), cookedGuiName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Build a list of all the KSP things (fields, events, actions) that
        /// this class will support as a suffix on this instance.
        /// </summary>
        /// <returns>list of all the suffixes except the hardcoded ones in GetSuffix()</returns>
        private ListValue AllThings()
        {
            const string FORMATTER = "({0}) {1}, is {2}";
            var all = new ListValue();

            // We appear to have not implemented a concatenator or range add for
            // our ListValue type.  Thus the for-loops below:
            ListValue fields = AllFields(FORMATTER);
            ListValue events = AllEvents(FORMATTER);
            ListValue actions = AllActions(FORMATTER);
            foreach (Structure field in fields)
            {
                all.Add(field);
            }
            foreach (Structure kspevent in events)
            {
                all.Add(kspevent);
            }
            foreach (Structure action in actions)
            {
                all.Add(action);
            }
            return all;
        }

        private void InitializeSuffixesAfterConstruction()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => partModule.moduleName));
            AddSuffix("PART", new Suffix<PartValue>(() => PartValueFactory.Construct(partModule.part, shared)));
            AddSuffix("ALLFIELDS", new Suffix<ListValue>(() => AllFields("({0}) {1}, is {2}")));
            AddSuffix("ALLFIELDNAMES", new Suffix<ListValue>(AllFieldNames));
            AddSuffix("HASFIELD", new OneArgsSuffix<BooleanValue, StringValue>(HasField));
            AddSuffix("ALLEVENTS", new Suffix<ListValue>(() => AllEvents("({0}) {1}, is {2}")));
            AddSuffix("AllEVENTNAMES", new Suffix<ListValue>(AllEventNames));
            AddSuffix("HASEVENT", new OneArgsSuffix<BooleanValue, StringValue>(HasEvent));
            AddSuffix("ALLACTIONS", new Suffix<ListValue>(() => AllActions("({0}) {1}, is {2}")));
            AddSuffix("ALLACTIONNAMES", new Suffix<ListValue>(AllActionNames));
            AddSuffix("HASACTION", new OneArgsSuffix<BooleanValue, StringValue>(HasAction));
            AddSuffix("GETFIELD", new OneArgsSuffix<Structure, StringValue>(GetKSPFieldValue));
            AddSuffix("SETFIELD", new TwoArgsSuffix<StringValue, Structure>(SetKSPFieldValue));
            AddSuffix("DOEVENT", new OneArgsSuffix<StringValue>(CallKSPEvent));
            AddSuffix("DOACTION", new TwoArgsSuffix<StringValue, BooleanValue>(CallKSPAction));
        }

        private static bool FieldIsVisible(BaseField field)
        {
            return (field != null) && (HighLogic.LoadedSceneIsEditor ? field.guiActiveEditor : field.guiActive);
        }

        private static bool EventIsVisible(BaseEvent evt)
        {
            return (evt != null) && (
                (HighLogic.LoadedSceneIsEditor ? evt.guiActiveEditor : evt.guiActive) &&
                /* evt.externalToEVAOnly) && */ // this flag seems bugged.  It always returns true no matter what.
                evt.active
                );
        }

        /// <summary>
        /// Get a KSPField with the kOS suffix name given.
        /// </summary>
        /// <param name="suffixName"></param>
        /// <returns></returns>
        protected Structure GetKSPFieldValue(StringValue suffixName)
        {
            BaseField field = GetField(suffixName);
            if (field == null)
                throw new KOSLookupFailException("FIELD", suffixName, this);
            if (!FieldIsVisible(field))
                throw new KOSLookupFailException("FIELD", suffixName, this, true);
            Structure obj = FromPrimitiveWithAssert(field.GetValue(partModule));
            return obj;
        }

        /// <summary>
        /// Set a KSPField with the kOS suffix name given to the new value given.
        /// </summary>
        /// <param name="suffixName"></param>
        /// <param name="newValue"></param>
        protected virtual void SetKSPFieldValue(StringValue suffixName, Structure newValue)
        {
            ThrowIfNotCPUVessel();
            BaseField field = GetField(suffixName);
            if (field == null)
                throw new KOSLookupFailException("FIELD", suffixName, this);
            if (!FieldIsVisible(field))
                throw new KOSLookupFailException("FIELD", suffixName, this, true);

            KOSException except;
            if (IsLegalValue(field, ref newValue, out except))
            {
                object convertedValue = Convert.ChangeType(newValue, field.FieldInfo.FieldType);
                field.SetValue(convertedValue, partModule);
            }
            else
            {
                throw except;
            }
        }

        /// <summary>
        /// Trigger whatever code the PartModule has attached to this Event, given the kOS name for the suffix.
        /// </summary>
        /// <param name="suffixName"></param>
        private void CallKSPEvent(StringValue suffixName)
        {
            ThrowIfNotCPUVessel();
            BaseEvent evt = GetEvent(suffixName);
            if (evt == null)
                throw new KOSLookupFailException("EVENT", suffixName, this);
            if (!EventIsVisible(evt))
                throw new KOSLookupFailException("EVENT", suffixName, this, true);

            if (RemoteTechHook.IsAvailable())
            {
                RemoteTechHook.Instance.InvokeOriginalEvent(evt);
            }
            else
            {
                evt.Invoke();
            }
        }

        /// <summary>
        /// Trigger whatever action the PartModule has attached to this Action, given the kOS name for the action.
        /// Warning - it probably triggers the entire action group that is attached to this action if there is one,
        /// not just the action on this one part.
        /// <br/><br/>
        /// NOTE: After kOS 0.15.5, this ability is limited by career progress of the VAB/SPH.
        /// </summary>
        /// <param name="suffixName"></param>
        /// <param name="param">true = activate, false = de-activate</param>
        private void CallKSPAction(StringValue suffixName, BooleanValue param)
        {
            ThrowIfNotCPUVessel();
            BaseAction act = GetAction(suffixName);
            if (act == null)
                throw new KOSLookupFailException("ACTION", suffixName, this);
            string careerReason;
            if (!Career.CanDoActions(out careerReason))
                throw new KOSLowTechException("use :DOACTION", careerReason);
            act.Invoke(new KSPActionParam(act.actionGroup, (param ? KSPActionType.Activate : KSPActionType.Deactivate)));
        }
    }
}