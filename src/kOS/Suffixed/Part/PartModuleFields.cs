using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using System;


namespace kOS.Suffixed.Part
{
    /// <summary>
    /// An abstraction of a part module attached to a PartValue, that allows
    /// the kOS script to get access to the module's KSPFields as kOS suffix terms.
    /// each KSPField of the PartModule becomes a suffix you can get to with
    /// GetSuffix().
    /// </summary>
    public class PartModuleFields : Structure
    {
        private readonly PartModule partModule;
        private readonly SharedObjects shared;
        
        /// <summary>
        /// Create a kOS-user variable wrapper around a KSP PartModule attached to a part.
        /// </summary>
        /// <param name="partModule">the KSP PartModule to make a wrapper for</param>
        public PartModuleFields(PartModule partModule, SharedObjects shared)
        {
            this.partModule = partModule;
            this.shared = shared;

            // Overriding Structure.InitializeSuffixes() doesn't work because the base constructor calls it
            // prior to calling this constructor, and so partModule isn't set yet:
            InitializeSuffixesAfterConstruction();
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
            returnValue.AppendLine( AllThings().ToString());

            return returnValue.ToString();
        }
        
        /// <summary>
        /// Return true if the field in question is editable in the KSP rightclick menu
        /// as an in-game tweakable right now.
        /// </summary>
        /// <param name="field">the BaseField from the KSP API</param>
        /// <returns>true if this has a GUI edit widget on it, false if it doesn't.</returns>
        private bool IsEditable(BaseField field)
        {
            return GetFieldControls(field).Count > 0;
        }
        
        /// <summary>
        /// Get the UI_Controls on a KSPField which are user editable.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        private List<UI_Control> GetFieldControls(BaseField field)
        {
            var attribs = new List<object>();
            attribs.AddRange(field.FieldInfo.GetCustomAttributes(true));
            return attribs.OfType<UI_Control>().Where(obj => (obj).controlEnabled).ToList();
        }
        
        /// <summary>
        /// Return true if the value given is allowed for the field given.  This uses the hints from the GUI
        /// system to decide what is and isn't allowed.  (For example if a GUI slider goes from 10 to 20 at
        /// increments of 2, then a value of 17 is not something you could achieve in the GUI, being only able
        /// to go from 16 to 18 skipping over 17, and therefore this routine would return false for trying to
        /// set it to 17).
        /// </summary>
        /// <param name="field">Which KSPField is being checked?</param>
        /// <param name="newVal">What is the value it's being set to?</param>
        /// <param name="except">An exception you can choose to throw if you want, or null if the value is legal.</param>
        /// <returns>Is it legal?</returns>
        private bool IsLegalValue(BaseField field, object newVal, out KOSException except)
        {
            except = null;
            bool isLegal = true;
            
            Type fType = field.FieldInfo.FieldType;
            object convertedVal = newVal;
            
            if (!IsEditable(field))
            {
                except = new KOSInvalidFieldValueException("Field is not editable");
                return false;
            }
            if (! newVal.GetType().IsSubclassOf(fType))
            {
                try {
                    convertedVal = Convert.ChangeType(newVal,fType);
                }
                catch (InvalidCastException) {
                    except = new KOSCastException(newVal.GetType(),fType);
                    return false;
                }
                catch (FormatException) {
                    except = new KOSCastException(newVal.GetType(),fType);
                    return false;
                }
            }
            List<UI_Control> controls = GetFieldControls(field);

            // It's really normal for there to be only one control on a KSPField, but because
            // it's technically possible to have more than one according to the structure of
            // the API, this loop is here to check all of "them":
            foreach (UI_Control control in controls)
            {
                const float FLOAT_EPSILON = 0.0001f; // because using exact equality with floats is a bad idea.
                
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
                    float min = range.minValue;
                    float max = range.maxValue;
                    float inc = range.stepIncrement;
                    if (val < min || val > max)
                    {
                        except = new KOSInvalidFieldValueException("Value is outside the allowed range ["+min+","+max+"]");
                        isLegal = false;
                    }
                    else if (Math.Abs((val - min) % inc) > FLOAT_EPSILON)
                    {
                        except = new KOSInvalidFieldValueException("Value is not an exact integer multiple of allowed step increment "+inc);
                        isLegal = false;
                    }
                }
                if (! isLegal)
                    break;
            }
            return isLegal;
        }

        /// <summary>
        /// Return a list of all the strings of all KSPfields registered to this PartModule.
        /// </summary>
        /// <returns>List of all the strings field names.</returns>
        private ListValue AllFields(string formatter)
        {            
            var returnValue = new ListValue();
            
            foreach (BaseField field in partModule.Fields)
            {
                returnValue.Add(String.Format(formatter,
                                              (IsEditable(field)?"settable":"get-only"),
                                              field.guiName.ToLower(),
                                              Utilities.Utils.KOSType(field.FieldInfo.FieldType)) );
            }
            return returnValue;
        }
        
        /// <summary>
        /// Determine if the Partmodule has this KSPField on it, which is publicly
        /// usable by a kOS script:
        /// </summary>
        /// <param name="fieldName">The field to search for</param>
        /// <returns>true if it is on the PartModule, false if it is not</returns>
        public bool HasField(string fieldName)
        {
            return partModule.Fields.Cast<BaseField>().
                Any(field => String.Equals(field.guiName, fieldName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Return whatever the field's current value is in the PartModule.
        /// </summary>
        /// <param name="cookedGuiName">The case-insensitive guiName of the field.</param>
        /// <returns>a BaseField - a KSP type that can be used to get the value, or its GUI name or its reflection info.</returns>
        private BaseField GetField(string cookedGuiName)
        {            
            return partModule.Fields.Cast<BaseField>().
                FirstOrDefault(field => String.Equals(field.guiName, cookedGuiName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Return a list of all the KSPEvents the module has in it.
        /// </summary>
        /// <returns></returns>
        private ListValue AllEvents(string formatter)
        {            
            var returnValue = new ListValue();
            
            foreach (BaseEvent kspEvent  in partModule.Events)
            {
                returnValue.Add(String.Format(formatter,
                                              "callable",
                                              kspEvent.guiName.ToLower(),
                                              "KSPEvent") );
            }
            return returnValue;
        }
        
        /// <summary>
        /// Determine if the Partmodule has this KSPEvent on it, which is publicly
        /// usable by a kOS script:
        /// </summary>
        /// <param name="eventName">The event name to search for</param>
        /// <returns>true if it is on the PartModule, false if it is not</returns>
        public bool HasEvent(string eventName)
        {
            return partModule.Events.
                Any(kspEvent => String.Equals(kspEvent.guiName, eventName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Return the KSP BaseEvent going with the given name.
        /// </summary>
        /// <param name="cookedGuiName">The event's case-insensitive guiname.</param>
        /// <returns></returns>
        private BaseEvent GetEvent(string cookedGuiName)
        {            
            return partModule.Events.
                FirstOrDefault(kspEvent => String.Equals(kspEvent.guiName, cookedGuiName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Return a list of all the KSPActions the module has in it.
        /// </summary>
        /// <returns></returns>
        private ListValue AllActions(string formatter)
        {            
            var returnValue = new ListValue();
            
            foreach (BaseAction kspAction  in partModule.Actions)
            {
                returnValue.Add(String.Format(formatter,
                                              "callable",
                                              kspAction.guiName.ToLower(),
                                              "KSPAction") );
            }
            return returnValue;
        }

        /// <summary>
        /// Determine if the Partmodule has this KSPAction on it, which is publicly
        /// usable by a kOS script:
        /// </summary>
        /// <param name="actionName">The action name to search for</param>
        /// <returns>true if it is on the PartModule, false if it is not</returns>
        public bool HasAction(string actionName)
        {
            return partModule.Actions.
                Any(kspAction => String.Equals(kspAction.guiName, actionName, StringComparison.CurrentCultureIgnoreCase));
        }

        /// <summary>
        /// Return the KSP BaseAction going with the given name.
        /// </summary>
        /// <param name="cookedGuiName">The event's case-insensitive guiname.</param>
        /// <returns></returns>
        private BaseAction GetAction(string cookedGuiName)
        {            
            foreach (BaseAction kspAction in partModule.Actions)
            {
                if (!String.Equals(kspAction.guiName, cookedGuiName, StringComparison.CurrentCultureIgnoreCase)) continue;
                Debug.Log("eraseme: yes");
                return kspAction;
            }
            return null;
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
            ListValue fields  = AllFields(FORMATTER);
            ListValue events  = AllEvents(FORMATTER);
            ListValue actions = AllActions(FORMATTER);
            for (int i = 0; i < fields.Count ; ++i)
            {
                all.Add(fields.GetIndex(i));
            }
            for (int i = 0; i < events.Count ; ++i)
            {
                all.Add(events.GetIndex(i));
            }
            for (int i = 0; i < actions.Count ; ++i)
            {
                all.Add(actions.GetIndex(i));
            }
            return all;
        }

        private void InitializeSuffixesAfterConstruction()
        {
            AddSuffix("NAME",       new Suffix<string>(() => partModule.moduleName));
            AddSuffix("PART",       new Suffix<PartValue>(() => new PartValue(partModule.part,shared)));
            AddSuffix("ALLFIELDS",  new Suffix<ListValue>(() => AllFields("({0}) {1}, is {2}")));
            AddSuffix("HASFIELD",   new OneArgsSuffix<bool, string>(HasField));
            AddSuffix("ALLEVENTS",  new Suffix<ListValue>(() => AllEvents("({0}) {1}, is {2}")));
            AddSuffix("HASEVENT",   new OneArgsSuffix<bool, string>(HasEvent));
            AddSuffix("ALLACTIONS", new Suffix<ListValue>(() => AllActions("({0}) {1}, is {2}")));
            AddSuffix("HASACTION",  new OneArgsSuffix<bool, string>(HasAction));
            AddSuffix("GETFIELD",   new OneArgsSuffix<object, string>(GetKSPFieldValue));
            AddSuffix("SETFIELD",   new TwoArgsSuffix<string, object>(SetKSPFieldValue));
            AddSuffix("DOEVENT",    new OneArgsSuffix<string>(CallKSPEvent));
            AddSuffix("DOACTION",   new TwoArgsSuffix<string, bool>(CallKSPAction));
        }
        
        /// <summary>
        /// Get a KSPField with the kOS suffix name given.
        /// </summary>
        /// <param name="suffixName"></param>
        /// <returns></returns>
        private object GetKSPFieldValue(string suffixName)
        {
            BaseField field = GetField(suffixName);
            if (field==null) {
                throw new KOSLookupFailException( "FIELD", suffixName, this);
            }
            object obj = field.GetValue(partModule);
            return obj;
        }
        
        /// <summary>
        /// Set a KSPField with the kOS suffix name given to the new value given.
        /// </summary>
        /// <param name="suffixName"></param>
        /// <param name="newValue"></param>
        private void SetKSPFieldValue(string suffixName, object newValue)
        {
            BaseField field = GetField(suffixName);
            if (field==null)
                throw new KOSLookupFailException( "FIELD", suffixName, this);

            KOSException except;                
            if (IsLegalValue(field, newValue, out except))
            {
                object convertedValue = Convert.ChangeType(newValue,field.FieldInfo.FieldType);
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
        private void CallKSPEvent(string suffixName)
        {
            BaseEvent evt = GetEvent(suffixName);
            if (evt==null)
                throw new KOSLookupFailException( "EVENT", suffixName, this);
            evt.Invoke();
        }

        /// <summary>
        /// Trigger whatever action the PartModule has attached to this Action, given the kOS name for the action.
        /// Warning - it probably triggers the entire action group that is attached to this action if there is one,
        /// not just the action on this one part.
        /// </summary>
        /// <param name="suffixName"></param>
        /// <param name="param">true = activate, false = de-activate</param>
        private void CallKSPAction(string suffixName, bool param)
        {
            BaseAction act = GetAction(suffixName);
            if (act==null)
                throw new KOSLookupFailException( "ACTION", suffixName, this);
            act.Invoke( new KSPActionParam( act.actionGroup, (param ? KSPActionType.Activate : KSPActionType.Deactivate) ));
        }
    }
}
