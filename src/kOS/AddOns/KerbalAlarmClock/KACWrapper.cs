using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace kOS.AddOns.KerbalAlarmClock
{

    ///////////////////////////////////////////////////////////////////////////////////////////
    // BELOW HERE SHOULD NOT BE EDITED - this links to the loaded KAC module without requiring a Hard Dependancy
    ///////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// The Wrapper class to access KAC from another plugin
    /// </summary>
    public class KACWrapper
    {
        protected static System.Type KACType;
        protected static System.Type KACAlarmType;

        protected static Object actualKAC = null;

        /// <summary>
        /// This is the Kerbal Alarm Clock object
        /// 
        /// SET AFTER INIT
        /// </summary>
        public static KACAPI KAC = null;
        /// <summary>
        /// Whether we found the KerbalAlarmClock assembly in the loadedassemblies. 
        /// 
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblyExists { get { return (KACType != null); } }
        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly. 
        /// 
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceExists { get { return (KAC != null); } }
        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance. 
        /// 
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _KACWrapped = false;

        /// <summary>
        /// Whether the object has been wrapped and the APIReady flag is set in the real KAC
        /// </summary>
        public static Boolean APIReady { get { return _KACWrapped && KAC.APIReady && !NeedUpgrade; } }


        public static Boolean NeedUpgrade { get; private set; }

        /// <summary>
        /// This method will set up the KAC object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static Boolean InitKACWrapper()
        {
            //if (!_KACWrapped )
            //{
            //reset the internal objects
            _KACWrapped = false;
            actualKAC = null;
            KAC = null;
            LogFormatted("Attempting to Grab KAC Types...");

            //find the base type
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName == "KerbalAlarmClock.KerbalAlarmClock")
                    KACType = t;
            });

            if (KACType == null)
            {
                return false;
            }

            LogFormatted("KAC Version:{0}", KACType.Assembly.GetName().Version.ToString());
            if (KACType.Assembly.GetName().Version.CompareTo(new System.Version(3, 0, 0, 5)) < 0)
            {
                //No TimeEntry or alarmchoice options = need a newer version
                NeedUpgrade = true;
            }
            
            //now the Alarm Type
            KACAlarmType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "KerbalAlarmClock.KACAlarm");

            if (KACAlarmType == null)
            {
                return false;
            }

            //now grab the running instance
            LogFormatted("Got Assembly Types, grabbing Instance");

            try {
                actualKAC = KACType.GetField("APIInstance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            } catch (Exception) {
                NeedUpgrade = true;
                LogFormatted("No APIInstance found - most likely you have KAC v2 installed");
                //throw;
            }
            if (actualKAC == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            //If we get this far we can set up the local object and its methods/functions
            LogFormatted("Got Instance, Creating Wrapper Objects");
            KAC = new KACAPI(actualKAC);
            //}
            _KACWrapped = true;
            return true;
        }

        /// <summary>
        /// The Type that is an analogue of the real KAC. This lets you access all the API-able properties and Methods of the KAC
        /// </summary>
        public class KACAPI
        {

            internal KACAPI(Object KAC)
            {
                //store the actual object
                actualKAC = KAC;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler
                LogFormatted("Getting APIReady Object");
                APIReadyField = KACType.GetField("APIReady", BindingFlags.Public | BindingFlags.Static);
                LogFormatted("Success: " + (APIReadyField != null).ToString());

                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPEL HERE
                LogFormatted("Getting Alarms Object");
                AlarmsField = KACType.GetField("alarms", BindingFlags.Public | BindingFlags.Static);
                actualAlarms = AlarmsField.GetValue(actualKAC);
                LogFormatted("Success: " + (actualAlarms != null).ToString());

                //Events
                LogFormatted("Getting Alarm State Change Event");
                onAlarmStateChangedEvent = KACType.GetEvent("onAlarmStateChanged", BindingFlags.Public | BindingFlags.Instance);
                LogFormatted_DebugOnly("Success: " + (onAlarmStateChangedEvent != null).ToString());
                LogFormatted_DebugOnly("Adding Handler");
                AddHandler(onAlarmStateChangedEvent, actualKAC, AlarmStateChanged);

                //Methods
                LogFormatted("Getting Create Method");
                CreateAlarmMethod = KACType.GetMethod("CreateAlarm", BindingFlags.Public | BindingFlags.Instance);
                LogFormatted_DebugOnly("Success: " + (CreateAlarmMethod != null).ToString());

                LogFormatted("Getting Delete Method");
                DeleteAlarmMethod = KACType.GetMethod("DeleteAlarm", BindingFlags.Public | BindingFlags.Instance);
                LogFormatted_DebugOnly("Success: " + (DeleteAlarmMethod != null).ToString());

                LogFormatted("Getting DrawAlarmAction");
                DrawAlarmActionChoiceMethod = KACType.GetMethod("DrawAlarmActionChoiceAPI", BindingFlags.Public | BindingFlags.Instance);
                LogFormatted_DebugOnly("Success: " + (DrawAlarmActionChoiceMethod != null).ToString());

                //LogFormatted("Getting DrawTimeEntry");
                //DrawTimeEntryMethod = KACType.GetMethod("DrawTimeEntryAPI", BindingFlags.Public | BindingFlags.Instance);
                //LogFormatted_DebugOnly("Success: " + (DrawTimeEntryMethod != null).ToString());

				//Commenting out rubbish lines
                //MethodInfo[] mis = KACType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                //foreach (MethodInfo mi in mis)
                //{
                //    LogFormatted("M:{0}-{1}", mi.Name, mi.DeclaringType);
                //}
            }

            private Object actualKAC;

            private FieldInfo APIReadyField;
            /// <summary>
            /// Whether the APIReady flag is set in the real KAC
            /// </summary>
            public Boolean APIReady
            {
                get
                {
                    if (APIReadyField == null)
                        return false;

                    return (Boolean)APIReadyField.GetValue(null);
                }
            }

            #region Alarms
            private Object actualAlarms;
            private FieldInfo AlarmsField;

            /// <summary>
            /// The list of Alarms that are currently active in game
            /// </summary>
            internal KACAlarmList Alarms
            {
                get
                {
                    return ExtractAlarmList(actualAlarms);
                }
            }

            /// <summary>
            /// This converts the KACAlarmList actual object to a new List for consumption
            /// </summary>
            /// <param name="actualAlarmList"></param>
            /// <returns></returns>
            private KACAlarmList ExtractAlarmList(Object actualAlarmList)
            {
                KACAlarmList ListToReturn = new KACAlarmList();
                try
                {
                    //iterate each "value" in the dictionary

                    foreach (var item in (IList)actualAlarmList)
                    {
                        KACAlarm r1 = new KACAlarm(item);
                        ListToReturn.Add(r1);
                    }
                }
                catch (Exception)
                {
                    //LogFormatted("Arrggg: {0}", ex.Message);
                    //throw ex;
                    //
                }
                return ListToReturn;
            }

            #endregion

            #region Events
            /// <summary>
            /// Takes an EventInfo and binds a method to the event firing
            /// </summary>
            /// <param name="Event">EventInfo of the event we want to attach to</param>
            /// <param name="KACObject">actual object the eventinfo is gathered from</param>
            /// <param name="Handler">Method that we are going to hook to the event</param>
            protected void AddHandler(EventInfo Event, Object KACObject, Action<Object> Handler)
            {
                //build a delegate
                Delegate d = Delegate.CreateDelegate(Event.EventHandlerType, Handler.Target, Handler.Method);
                //get the Events Add method
                MethodInfo addHandler = Event.GetAddMethod();
                //and add the delegate
                addHandler.Invoke(KACObject, new System.Object[] { d });
            }

            //the info about the event;
            private EventInfo onAlarmStateChangedEvent;

            /// <summary>
            /// Event that fires when the State of an Alarm changes
            /// </summary>
            public event AlarmStateChangedHandler onAlarmStateChanged;
            /// <summary>
            /// Structure of the event delegeate
            /// </summary>
            /// <param name="e"></param>
            public delegate void AlarmStateChangedHandler(AlarmStateChangedEventArgs e);
            /// <summary>
            /// This is the structure that holds the event arguments
            /// </summary>
            public class AlarmStateChangedEventArgs
            {
                public AlarmStateChangedEventArgs(System.Object actualEvent, KACAPI kac)
                {
                    Type type = actualEvent.GetType();
                    this.alarm = new KACAlarm(type.GetField("alarm").GetValue(actualEvent));
                    this.eventType = (KACAlarm.AlarmStateEventsEnum)type.GetField("eventType").GetValue(actualEvent);

                }

                /// <summary>
                /// Alarm that has had the state change
                /// </summary>
                public KACAlarm alarm;
                /// <summary>
                /// What the state was before the event
                /// </summary>
                public KACAlarm.AlarmStateEventsEnum eventType;
            }


            /// <summary>
            /// private function that grabs the actual event and fires our wrapped one
            /// </summary>
            /// <param name="actualEvent">actual event from the KAC</param>
            private void AlarmStateChanged(object actualEvent)
            {
                if (onAlarmStateChanged != null)
                {
                    onAlarmStateChanged(new AlarmStateChangedEventArgs(actualEvent, this));
                }
            }
            #endregion


            #region Methods
            private MethodInfo CreateAlarmMethod;

            /// <summary>
            /// Create a new Alarm
            /// </summary>
            /// <param name="AlarmType">What type of alarm are we creating</param>
            /// <param name="Name">Name of the Alarm for the display</param>
            /// <param name="UT">Universal Time for the alarm</param>
            /// <returns>ID of the newly created alarm</returns>
            internal String CreateAlarm(AlarmTypeEnum AlarmType, String Name, Double UT)
            {
                return (String)CreateAlarmMethod.Invoke(actualKAC, new System.Object[] { (Int32)AlarmType, Name, UT });
            }


            private MethodInfo DeleteAlarmMethod;
            /// <summary>
            /// Delete an Alarm
            /// </summary>
            /// <param name="AlarmID">Unique ID of the alarm</param>
            /// <returns>Success of the deletion</returns>
            internal Boolean DeleteAlarm(String AlarmID)
            {
                return (Boolean)DeleteAlarmMethod.Invoke(actualKAC, new System.Object[] { AlarmID });
            }


            private MethodInfo DrawAlarmActionChoiceMethod;
            /// <summary>
            /// Delete an Alarm
            /// </summary>
            /// <param name="AlarmID">Unique ID of the alarm</param>
            /// <returns>Success of the deletion</returns>
            internal Boolean DrawAlarmActionChoice(ref AlarmActionEnum Choice, String LabelText, Int32 LabelWidth, Int32 ButtonWidth)
            {
                Int32 InValue = (Int32)Choice;
                Int32 OutValue = (Int32)DrawAlarmActionChoiceMethod.Invoke(actualKAC, new System.Object[] { InValue, LabelText, LabelWidth, ButtonWidth });

                Choice = (AlarmActionEnum)OutValue;
                return (InValue != OutValue);
            }

            //Remmed out due to it borking window layout
            //private MethodInfo DrawTimeEntryMethod;
            ///// <summary>
            ///// Delete an Alarm
            ///// </summary>
            ///// <param name="AlarmID">Unique ID of the alarm</param>
            ///// <returns>Success of the deletion</returns>

            //internal Boolean DrawTimeEntry(ref Double Time, TimeEntryPrecisionEnum Prec, String LabelText, Int32 LabelWidth)
            //{
            //    Double InValue = Time;
            //    Double OutValue = (Double)DrawTimeEntryMethod.Invoke(actualKAC, new System.Object[] { InValue, (Int32)Prec, LabelText, LabelWidth });

            //    Time = OutValue;
            //    return (InValue != OutValue);
            //}


            #endregion

            public class KACAlarm
            {
                internal KACAlarm(Object a)
                {
                    actualAlarm = a;
                    VesselIDField = KACAlarmType.GetField("VesselID");
                    IDField = KACAlarmType.GetField("ID");
                    NameField = KACAlarmType.GetField("Name");
                    NotesField = KACAlarmType.GetField("Notes");
                    AlarmTypeField = KACAlarmType.GetField("TypeOfAlarm");
                    AlarmTimeProperty = KACAlarmType.GetProperty("AlarmTimeUT");
                    AlarmMarginField = KACAlarmType.GetField("AlarmMarginSecs");
                    RemainingField = KACAlarmType.GetField("Remaining");

                    AlarmActionField = KACAlarmType.GetField("AlarmAction");
                    ActionActionProperty = KACAlarmType.GetProperty("AlarmActionConvert");

                    XferOriginBodyNameField = KACAlarmType.GetField("XferOriginBodyName");
                    //LogFormatted("XFEROrigin:{0}", XferOriginBodyNameField == null);
                    XferTargetBodyNameField = KACAlarmType.GetField("XferTargetBodyName");

                    RepeatAlarmField = KACAlarmType.GetField("RepeatAlarm");
                    RepeatAlarmPeriodProperty = KACAlarmType.GetProperty("RepeatAlarmPeriodUT");

                    //PropertyInfo[] pis = KACAlarmType.GetProperties();
                    //foreach (PropertyInfo pi in pis)
                    //{
                    //    LogFormatted("P:{0}-{1}", pi.Name, pi.DeclaringType);
                    //}
                    //FieldInfo[] fis = KACAlarmType.GetFields();
                    //foreach (FieldInfo fi in fis)
                    //{
                    //    LogFormatted("F:{0}-{1}", fi.Name, fi.DeclaringType);
                    //}
                }
                private Object actualAlarm;

                private FieldInfo VesselIDField;
                /// <summary>
                /// Unique Identifier of the Vessel that the alarm is attached to
                /// </summary>
                public String VesselID
                {
                    get { return (String)VesselIDField.GetValue(actualAlarm); }
                    set { VesselIDField.SetValue(actualAlarm, value); }
                }

                private FieldInfo IDField;
                /// <summary>
                /// Unique Identifier of this alarm
                /// </summary>
                public String ID
                {
                    get { return (String)IDField.GetValue(actualAlarm); }
                }

                private FieldInfo NameField;
                /// <summary>
                /// Short Text Name for the Alarm
                /// </summary>
                public String Name
                {
                    get { return (String)NameField.GetValue(actualAlarm); }
                    set { NameField.SetValue(actualAlarm, value); }
                }

                private FieldInfo NotesField;
                /// <summary>
                /// Longer Text Description for the Alarm
                /// </summary>
                public String Notes
                {
                    get { return (String)NotesField.GetValue(actualAlarm); }
                    set { NotesField.SetValue(actualAlarm, value); }
                }

                private FieldInfo XferOriginBodyNameField;
                /// <summary>
                /// Name of the origin body for a transfer
                /// </summary>
                public String XferOriginBodyName
                {
                    get { return (String)XferOriginBodyNameField.GetValue(actualAlarm); }
                    set { XferOriginBodyNameField.SetValue(actualAlarm, value); }
                }

                private FieldInfo XferTargetBodyNameField;
                /// <summary>
                /// Name of the destination body for a transfer
                /// </summary>
                public String XferTargetBodyName
                {
                    get { return (String)XferTargetBodyNameField.GetValue(actualAlarm); }
                    set { XferTargetBodyNameField.SetValue(actualAlarm, value); }
                }
                
                private FieldInfo AlarmTypeField;
                /// <summary>
                /// What type of Alarm is this - affects icon displayed and some calc options
                /// </summary>
                public AlarmTypeEnum AlarmType { get { return (AlarmTypeEnum)AlarmTypeField.GetValue(actualAlarm); } }

                private PropertyInfo AlarmTimeProperty;
                /// <summary>
                /// In game UT value of the alarm
                /// </summary>
                public Double AlarmTime
                {
                    get { return (Double)AlarmTimeProperty.GetValue(actualAlarm,null); }
                    set { AlarmTimeProperty.SetValue(actualAlarm, value, null); }
                }

                private FieldInfo AlarmMarginField;
                /// <summary>
                /// In game seconds the alarm will fire before the event it is for
                /// </summary>
                public Double AlarmMargin
                {
                    get { return (Double)AlarmMarginField.GetValue(actualAlarm); }
                    set { AlarmMarginField.SetValue(actualAlarm, value); }
                }

                private FieldInfo AlarmActionField;
                /// <summary>
                /// What should the Alarm Clock do when the alarm fires
                /// </summary>
                //public AlarmActionEnum AlarmAction
                //{
                //    get { return (AlarmActionEnum)AlarmActionField.GetValue(actualAlarm); }
                //    set { AlarmActionField.SetValue(actualAlarm, (Int32)value); }
                //}
                /// <summary>
                /// What should the Alarm Clock do when the alarm fires
                /// </summary>
                public AlarmActionEnum AlarmAction
                {
                    get { return (AlarmActionEnum)ActionActionProperty.GetValue(actualAlarm, null); }
                    set { ActionActionProperty.SetValue(actualAlarm, (Int32)value, null); }
                }
                private PropertyInfo ActionActionProperty;



                private FieldInfo RemainingField;
                /// <summary>
                /// How much Game time is left before the alarm fires
                /// </summary>
                public Double Remaining { get { return (Double)RemainingField.GetValue(actualAlarm); } }


                private FieldInfo RepeatAlarmField;
                /// <summary>
                /// Whether the alarm will be repeated after it fires
                /// </summary>
                public Boolean RepeatAlarm
                {
                    get { return (Boolean)RepeatAlarmField.GetValue(actualAlarm); }
                    set { RepeatAlarmField.SetValue(actualAlarm, value); }
                }
                private PropertyInfo RepeatAlarmPeriodProperty;
                /// <summary>
                /// Value in Seconds after which the alarm will repeat
                /// </summary>
                public Double RepeatAlarmPeriod
                {
                    get
                    {
                        try { return (Double)RepeatAlarmPeriodProperty.GetValue(actualAlarm, null); }
                        catch (Exception) { return 0; }
                    }
                    set { RepeatAlarmPeriodProperty.SetValue(actualAlarm, value, null); }
                }

                public enum AlarmStateEventsEnum
                {
                    Created,
                    Triggered,
                    Closed,
                    Deleted,
                }
            }

            public enum AlarmTypeEnum
            {
                Raw,
                Maneuver,
                ManeuverAuto,
                Apoapsis,
                Periapsis,
                AscendingNode,
                DescendingNode,
                LaunchRendevous,
                Closest,
                SOIChange,
                SOIChangeAuto,
                Transfer,
                TransferModelled,
                Distance,
                Crew,
                EarthTime,
                Contract,
                ContractAuto,
                ScienceLab
            }

            public enum AlarmActionEnum
            {
                [Description("Do Nothing-Delete When Past")]        DoNothingDeleteWhenPassed,
                [Description("Do Nothing")]                         DoNothing,
                [Description("Message Only-No Affect on warp")]     MessageOnly,
                [Description("Kill Warp Only-No Message")]          KillWarpOnly,
                [Description("Kill Warp and Message")]              KillWarp,
                [Description("Pause Game and Message")]             PauseGame
            }

            public enum TimeEntryPrecisionEnum
            {
                Seconds = 0,
                Minutes = 1,
                Hours = 2,
                Days = 3,
                Years = 4
            }

            public class KACAlarmList : List<KACAlarm>
            {

            }
        }
        #region Logging Stuff
        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(String Message, params Object[] strParams)
        {
            LogFormatted(Message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted(String Message, params Object[] strParams)
        {
            Message = String.Format(Message, strParams);
            String strMessageLine = String.Format("{0},{2}-{3},{1}",
                DateTime.Now, Message, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            UnityEngine.Debug.Log(strMessageLine);
        }
        #endregion
    }
}
