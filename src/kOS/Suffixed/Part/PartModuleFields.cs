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
        private bool IsEditable(BaseField field)
        {
            string fieldType = field.FieldInfo.FieldType.ToString();
            
            // There isn't a quick simple boolean "is editable" value.  Instead we have to
            // check if there's an attribute attached that declares of the tweakable
            // UI controls for the field:
            
            List<object> attribs = new List<object>();
            attribs.AddRange(field.FieldInfo.GetCustomAttributes(true));
            bool returnValue = false;
            foreach (object obj in attribs)
            {
                if (obj is UI_Control) // all tweakable controls: sliders, toggle buttons, etc, are subclasses of this attribute.
                {
                    if ( ((UI_Control)obj).controlEnabled )
                    {
                        returnValue = true;
                        break;
                    }
                }
            }
            return returnValue;
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
                                              (IsEditable(field)?"settable":"get-only"),
                                              field.guiName.ToLower().Replace(' ','_'),
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
                                              kspEvent.guiName.ToLower().Replace(' ','_'),
                                              "KSPEvent") );
            }
            return returnValue;
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
            ListValue fields = AllFields(formatter);
            ListValue events = AllEvents(formatter);
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
            AddSuffix("ALL", new Suffix<PartModuleFields,ListValue>(this, model => model.AllThings()));
            foreach (BaseField field in partModule.Fields)
            {
                string fieldNameForKSP = field.guiName.ToUpper().Replace(' ','_');
                ISuffix suf;
                if (IsEditable(field))
                {
                    // I actually do know the type of the ksp field at *runtime* but not at
                    // *compile* time, so I can't pass it in to the generics syntax <....>
                    // in this statement.  That's why it just says "object":
                    suf = new SetSuffix<PartModuleFields,object>(
                        this, model => model.GetFieldValue(fieldNameForKSP),
                        (model, value) => model.SetFieldValue(fieldNameForKSP,value)
                       );
                }
                else
                {
                    // I actually do know the type of the ksp field at *runtime* but not at
                    // *compile* time, so I can't pass it in to the generics syntax <....>
                    // in this statement.  That's why it just says "object":
                    suf = new Suffix<PartModuleFields,object>(this, model => model.GetFieldValue(fieldNameForKSP));
                }
                AddSuffix( fieldNameForKSP, suf);
            }
        }
        
        public object GetFieldValue(string suffixName)
        {
            BaseField field = GetField(suffixName);
            if (field!=null)
            {
                object obj = field.GetValue(partModule);
                if (obj!=null)
                    return obj;
            }
            throw new KOSSuffixUseException( "GET", suffixName, this);
        }
        
        public void SetFieldValue(string suffixName, object value)
        {
            BaseField field = GetField(suffixName);
            if (field!=null)
            {
                if (IsEditable(field))
                {
                    field.SetValue(value, partModule);
                    return;
                }
            }
            throw new KOSSuffixUseException( "SET", suffixName, this);
        }        
    }
}
