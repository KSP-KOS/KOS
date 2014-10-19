using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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
        private bool debugMsg = true;
        
        protected PartModule partModule;
        
        /// <summary>
        /// Create a kOS-user variable wrapper around a KSP PartModule attached to a part.
        /// </summary>
        /// <param name="partModule">the KSP PartModule to make a wrapper for</param>
        public PartModuleFields(PartModule partModule)
        {
            this.partModule = partModule;

            // Overriding Structure.InitializeSuffixes() doesn't work because the base constructor calls it
            // prior to calling this constructor, and so partModule isn't set yet:
            InitializeSuffixesAfterConstruction();
        }

        /// <summary>
        /// Get the string type of the module
        /// </summary>
        /// <returns>the string type</returns>
        public string GetModuleName()
        {
            return partModule.moduleName;
        }

        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();
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
        private bool isEditable(BaseField field)
        {
            return getFieldControls(field).Count > 0;
        }
        
        /// <summary>
        /// Get the UI_Controls on a KSPField which are user editable.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        private List<UI_Control> getFieldControls(BaseField field)
        {
            string fieldType = field.FieldInfo.FieldType.ToString();
            
            List<UI_Control> returnControls = new List<UI_Control>();
            
            List<object> attribs = new List<object>();
            attribs.AddRange(field.FieldInfo.GetCustomAttributes(true));
            foreach (object obj in attribs)
            {
                if (obj is UI_Control) // all tweakable controls: sliders, toggle buttons, etc, are subclasses of this attribute.
                {
                    if ( ((UI_Control)obj).controlEnabled )
                    {
                        returnControls.Add( (UI_Control)obj );
                    }
                }
            }
            return returnControls;
        }
        
        /// <summary>
        /// Return true if the value given is allowed for the field given.  This uses the hints from the GUI
        /// system to decide what is and isn't allowed.  (For example if a GUI slider goes from 10 to 20 at
        /// increments of 2, then a value of 17 is not something you could achive in the GUI, being only able
        /// to go from 16 to 18 skipping over 17, and therefore this routine would return false for trying to
        /// set it to 17).
        /// </summary>
        /// <param name="field">Which KSPField is being checked?</param>
        /// <param name="newVal">What is the value it's being set to?</param>
        /// <param name="except">An exception you can choose to throw if you want, or null if the value is legal.</param>
        /// <returns>Is it legal?</returns>
        private bool isLegalValue(BaseField field, object newVal, out KOSException except)
        {
            except = null;
            bool isLegal = true;
            
            Type fType = field.FieldInfo.FieldType;
            object convertedVal = newVal;
            
            if (!isEditable(field))
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
            List<UI_Control> controls = getFieldControls(field);

            // It's really normal for there to be only one control on a KSPField, but because
            // it's technically possible to have more than one according to the structure of
            // the API, this loop is here to check all of "them":
            foreach (UI_Control control in controls)
            {
                float floatCompareTolerance = 0.0001f; // because using exact equality with floats is a bad idea.
                
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
                if (control is UI_Vector2)
                {
                    // I have no clue what this actually looks like in the UI?  What is a
                    // user editable 2-D vector widget?  I've never seen this before.
                    Vector2 vec2 = (Vector2)convertedVal;
                    if (vec2.x < ((UI_Vector2)control).minValueX || vec2.x > ((UI_Vector2)control).maxValueX ||
                        vec2.y < ((UI_Vector2)control).minValueY || vec2.y > ((UI_Vector2)control).maxValueY)
                    {
                        except = new KOSInvalidFieldValueException("Vector2 is outside of allowed range of values");
                        isLegal = false;
                    }
                }
                if (control is UI_FloatRange)
                {
                    float val = Convert.ToSingle(convertedVal);
                    float min = ((UI_FloatRange)control).minValue;
                    float max = ((UI_FloatRange)control).maxValue;
                    float inc = ((UI_FloatRange)control).stepIncrement;
                    if (val < min || val > max)
                    {
                        except = new KOSInvalidFieldValueException("Value is outside the allowed range ["+min+","+max+"]");
                        isLegal = false;
                    }
                    else if (Math.Abs((val - min) % inc) > floatCompareTolerance)
                    {
                        except = new KOSInvalidFieldValueException("Value is not an exact integer multiple of allowed step increment "+inc);
                        isLegal = false;
                    }
                }
                if (control is UI_Control)
                {
                    // Generic cases to check for ... are there any? For now I can't think of any so this is blank.
                }
                if (! isLegal)
                    break;
            }
            return isLegal;
        }

        /// <summary>
        /// Return a list of all the strings of all KSPfields registered to this PartModule.
        /// </summary>
        /// <returns>List of all the strings.  NOTE, the GUI strings are used because
        /// that's all the kOS script writer can see - they don't know the real C# field
        /// name behind the gui.
        /// Also, because gui names can have spaces. any spaces are turned into underscores
        /// for the sake of making them legal kOS identifiers.</returns>
        public ListValue AllFields(string formatter)
        {            
            ListValue returnValue = new ListValue();
            
            foreach (BaseField field in partModule.Fields)
            {
                returnValue.Add(String.Format(formatter,
                                              (isEditable(field)?"settable":"get-only"),
                                              field.guiName.ToLower(),
                                              Utilities.Utils.KOSType(field.FieldInfo.FieldType)) );
            }
            return returnValue;
        }
        
        /// <summary>
        /// Return whatever the field's current value is in the PartModule.
        /// </summary>
        /// <param name="cookedGuiName">The guiName of the field, with spaces turned to underscores.
        /// Matching will be done case-insensitively.</param>
        /// <returns>a BaseField - a KSP type that can be used to get the value, or its gui name or its reflection info.</returns>
        public BaseField GetField(string cookedGuiName)
        {            
            string lowerName = cookedGuiName.ToLower();
            
            foreach (BaseField field in partModule.Fields)
            {
                if (field.guiName.ToLower().Replace(' ','_') == lowerName)
                {
                    // maybe cache the above lookup after the first time it happens??
                    return field;
                }
            }
            return null;
        }

        /// <summary>
        /// Return a list of all the KSPEvents the module has in it.
        /// </summary>
        /// <returns></returns>
        public ListValue AllEvents(string formatter)
        {            
            ListValue returnValue = new ListValue();
            
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
        /// Return the KSP BaseEvent going with the given name.
        /// </summary>
        /// <param name="cookedGuiName">The event's guiname in KOS terms (ie, spaces turned to underscores).
        /// Matching will be done case-insensitively.</param>
        /// <returns></returns>
        public BaseEvent GetEvent(string cookedGuiName)
        {            
            string lowerName = cookedGuiName.ToLower();
            
            foreach (BaseEvent kspEvent  in partModule.Events)
            {
                if (kspEvent.guiName.ToLower() == lowerName)
                {
                    return kspEvent;
                }
            }
            return null;
        }

        /// <summary>
        /// Return a list of all the KSPActions the module has in it.
        /// </summary>
        /// <returns></returns>
        public ListValue AllActions(string formatter)
        {            
            ListValue returnValue = new ListValue();
            
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
        /// Return the KSP BaseAction going with the given name.
        /// </summary>
        /// <param name="cookedGuiName">The event's guiname in KOS terms (ie, spaces turned to underscores).
        /// Matching will be done case-insensitively.</param>
        /// <returns></returns>
        public BaseAction GetAction(string cookedGuiName)
        {            
            string lowerName = cookedGuiName.ToLower();
            
            foreach (BaseAction kspAction in partModule.Actions)
            {
                Debug.Log("eraseme: does \""+kspAction.guiName.ToLower()+"\" == \""+lowerName+"\"?");
                if (kspAction.guiName.ToLower() == lowerName)
                {
                    Debug.Log("eraseme: yes");
                    return kspAction;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Build a list of all the KSP things (fields, events, actions) that
        /// this class will support as a suffix on this instance.
        /// </summary>
        /// <returns>list of all the suffixes except the hardcoded ones in GetSuffix()</returns>
        public ListValue AllThings()
        {
            string formatter = "({0}) {1}, is {2}";
            ListValue all = new ListValue();

            // We appear to have not implemented a concatenator or range add for
            // our ListValue type.  Thus the for-loops below:
            ListValue fields  = AllFields(formatter);
            ListValue events  = AllEvents(formatter);
            ListValue actions = AllActions(formatter);
            for (int i = 0; i < fields.Count ; ++i)
            {
                all.Add(fields.GetIndex(i));
            }
            for (int i = 0; i < events.Count ; ++i)
            {
                all.Add(events.GetIndex(i));
            }
            return all;
        }

        protected void InitializeSuffixesAfterConstruction()
        {
            AddSuffix("ALLFIELDS",  new Suffix<ListValue>(() => AllFields("({0}) {1}, is {2}")));
            AddSuffix("ALLEVENTS",  new Suffix<ListValue>(() => AllEvents("({0}) {1}, is {2}")));
            AddSuffix("ALLACTIONS", new Suffix<ListValue>(() => AllActions("({0}) {1}, is {2}")));
            AddSuffix("GETFIELD",   new OneArgsSuffix<object, string>((sufName) => GetKSPFieldValue(sufName)));
            AddSuffix("SETFIELD",   new TwoArgsSuffix<string, object>((sufName,newVal) => SetKSPFieldValue(sufName, newVal)));
            AddSuffix("DOEVENT",    new OneArgsSuffix<string>((sufName) => CallKSPEvent(sufName)));
            AddSuffix("DOACTION",   new TwoArgsSuffix<string, bool>((sufName,flag) => CallKSPAction(sufName,flag)));
        }
        
        // TODO - delete this unused method after verifying the new way works:
        protected void InitializeKSPFields()
        {
            foreach (BaseField field in partModule.Fields)
            {
                if (debugMsg) Debug.Log( "Adding PartModuleField suffixes for " + partModule.moduleName );
                string fieldNameForKOS = field.guiName.ToUpper();
                if (debugMsg) Debug.Log( "  KSP suffix name: " + fieldNameForKOS );
                ISuffix suf;
                if (field.guiActive)
                {
                    if (isEditable(field))
                    {
                        if (debugMsg) Debug.Log( "      is Editable");
                        // I actually do know the type of the ksp field at *runtime* but not at
                        // *compile* time, so I can't pass it in to the generics syntax <....>
                        // in this statement.  That's why it just says "object":
                        suf = new SetSuffix<object>(() => GetKSPFieldValue(fieldNameForKOS), value => SetKSPFieldValue(fieldNameForKOS,value));
                    }
                    else
                    {
                        if (debugMsg) Debug.Log( "      is NOT Editable");
                        // I actually do know the type of the ksp field at *runtime* but not at
                        // *compile* time, so I can't pass it in to the generics syntax <....>
                        // in this statement.  That's why it just says "object":
                        suf = new Suffix<object>(()=>GetKSPFieldValue(fieldNameForKOS));
                    }
                    AddSuffix( fieldNameForKOS, suf);
                }
            }
        }
        
        // TODO - delete this unused method after verifying the new way works:
        protected void InitializeKSPEvents()
        {
            // Using variable name "evt" because "event" is a reserved word:
            foreach (BaseEvent evt in partModule.Events)
            {
                if (debugMsg) Debug.Log( "Adding PartModuleEvent suffixes for " + partModule.moduleName );
                string evtNameForKOS = evt.guiName.ToUpper();
                if (debugMsg) Debug.Log( "  KSP suffix name: " + evtNameForKOS );
                if (evt.guiActive)
                {
                    if (debugMsg) Debug.Log( "      is activatable");
                    AddSuffix(evtNameForKOS, new NoArgsSuffix(() => CallKSPEvent(evtNameForKOS)));
                }
                else
                {
                    if (debugMsg) Debug.Log( "      is NOT activatable");
                }
            }
        }

        // TODO - delete this unused method after verifying the new way works:
        protected void InitializeKSPActions()
        {
            foreach (BaseAction act in partModule.Actions)
            {
                if (debugMsg) Debug.Log( "Adding PartModuleAction suffixes for " + partModule.moduleName );
                string actNameForKOS = "ACTION_" + act.guiName.ToUpper();
                if (debugMsg) Debug.Log( "  KSP suffix name: " + actNameForKOS );
                if (debugMsg) Debug.Log( "      is activatable");
                AddSuffix(actNameForKOS, new OneArgsSuffix<bool>((newFlag) => CallKSPAction(actNameForKOS, newFlag )));
            }
        }
        
        /// <summary>
        /// Get a KSPField with the kOS suffix name given.
        /// </summary>
        /// <param name="suffixName"></param>
        /// <returns></returns>
        public object GetKSPFieldValue(string suffixName)
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
        public void SetKSPFieldValue(string suffixName, object newValue)
        {
            BaseField field = GetField(suffixName);
            if (field==null)
                throw new KOSLookupFailException( "FIELD", suffixName, this);

            KOSException except;                
            if (isLegalValue(field, newValue, out except))
            {
                object convertedValue = Convert.ChangeType(newValue,field.FieldInfo.FieldType);
                field.SetValue(convertedValue, partModule);
                return;
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
        public void CallKSPEvent(string suffixName)
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
        public void CallKSPAction(string suffixName, bool param)
        {
            BaseAction act = GetAction(suffixName);
            if (act==null)
                throw new KOSLookupFailException( "ACTION", suffixName, this);
            act.Invoke( new KSPActionParam( act.actionGroup, (param ? KSPActionType.Activate : KSPActionType.Deactivate) ));
        }
    }
}
