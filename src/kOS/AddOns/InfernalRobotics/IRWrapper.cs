using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

// TODO: Change this namespace to something specific to your plugin here.
namespace kOS.AddOns.InfernalRobotics
{

    public class IRWrapper
    {
        protected static System.Type IRServoControllerType;
        protected static System.Type IRControlGroupType;
        protected static System.Type IRServoType;
        protected static System.Type IRServoPartType;
        protected static System.Type IRServoMechanismType;

        protected static Object actualServoController = null;

        public static IRAPI IRController = null;
        public static Boolean AssemblyExists { get { return (IRServoControllerType != null); } }
        public static Boolean InstanceExists { get { return (IRController != null); } }
        private static Boolean isWrapped = false;
        public static Boolean APIReady { get { return isWrapped && IRController.APIReady; } }

        public static Boolean InitWrapper()
        {
            isWrapped = false;
            actualServoController = null;
            IRController = null;
            LogFormatted("Attempting to Grab IR Types...");

            IRServoControllerType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "InfernalRobotics.Command.ServoController");

            if (IRServoControllerType == null)
            {
                return false;
            }

            LogFormatted("IR Version:{0}", IRServoControllerType.Assembly.GetName().Version.ToString());

            IRServoMechanismType = AssemblyLoader.loadedAssemblies
               .Select(a => a.assembly.GetExportedTypes())
               .SelectMany(t => t)
               .FirstOrDefault(t => t.FullName == "InfernalRobotics.Control.IMechanism");

            if (IRServoMechanismType == null)
            {
                LogFormatted("[IR Wrapper] Failed to grab Mechanism Type");
                return false;
            }

            IRServoType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "InfernalRobotics.Control.IServo");

            if (IRServoType == null)
            {
                LogFormatted("[IR Wrapper] Failed to grab Servo Type");
                return false;
            }

            IRServoPartType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "InfernalRobotics.Control.IPart");

            if (IRServoType == null)
            {
                LogFormatted("[IR Wrapper] Failed to grab ServoPart Type");
                return false;
            }

            IRControlGroupType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "InfernalRobotics.Command.ServoController+ControlGroup");

            if (IRControlGroupType == null)
            {
                var irassembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.FullName.Contains("InfernalRobotics"));
                if (irassembly == null)
                {
                    LogFormatted("[IR Wrapper] cannot find InvernalRobotics.dll");
                    return false;
                }
                foreach (Type t in irassembly.assembly.GetExportedTypes())
                {
                    LogFormatted("[IR Wrapper] Exported type: " + t.FullName);
                }

                LogFormatted("[IR Wrapper] Failed to grab ControlGroup Type");
                return false;
            }

            LogFormatted("Got Assembly Types, grabbing Instance");

            try
            {
                var fi = IRServoControllerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                
                if (fi == null)
                    LogFormatted("[IR Wrapper] Cannot find Instance Property");
                actualServoController = fi.GetValue(null, null);
            }
            catch (Exception e)
            {
                LogFormatted("No Instance found, " + e.Message);
            }

            if (actualServoController == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            LogFormatted("Got Instance, Creating Wrapper Objects");
            IRController = new IRAPI(actualServoController);
            isWrapped = true;
            return true;
        }

        public class IRAPI
        {
            internal IRAPI(Object IRServoController)
            {
                actualServoController = IRServoController;

                LogFormatted("Getting APIReady Object");
                APIReadyProperty = IRServoControllerType.GetProperty("APIReady", BindingFlags.Public | BindingFlags.Static);
                LogFormatted("Success: " + (APIReadyProperty != null).ToString());

                LogFormatted("Getting ServoGroups Object");
                ServoGroupsField = IRServoControllerType.GetField("ServoGroups");
                if (ServoGroupsField == null)
                    LogFormatted("Failed Getting ServoGroups fieldinfo");
                actualServoGroups = ServoGroupsField.GetValue(actualServoController);
                LogFormatted("Success: " + (actualServoGroups != null).ToString());
                
            }

            private Object actualServoController;

            private PropertyInfo APIReadyProperty;
            public Boolean APIReady
            {
                get
                {
                    if (APIReadyProperty == null)
                        return false;

                    return (Boolean)APIReadyProperty.GetValue(null, null);
                }
            }

            private Object actualServoGroups;
            private FieldInfo ServoGroupsField;

            internal IRServoGroupsList ServoGroups
            {
                get
                {
                    return ExtractServoGroups(actualServoGroups);
                }
            }

            private IRServoGroupsList ExtractServoGroups(Object actualServoGroups)
            {
                IRServoGroupsList ListToReturn = new IRServoGroupsList();
                try
                {
                    //iterate each "value" in the dictionary
                    foreach (var item in (IList)actualServoGroups)
                    {
                        IRControlGroup r1 = new IRControlGroup(item);
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

            public class IRControlGroup
            {
                internal IRControlGroup(Object cg)
                {
                    actualControlGroup = cg;
                    NameProperty = IRControlGroupType.GetProperty("Name");
                    ForwardKeyProperty = IRControlGroupType.GetProperty("ForwardKey");
                    ReverseKeyProperty = IRControlGroupType.GetProperty("ReverseKey");
                    SpeedProperty = IRControlGroupType.GetProperty("Speed");
                    ExpandedProperty = IRControlGroupType.GetProperty("Expanded");

                    ServosProperty = IRControlGroupType.GetProperty("Servos");
                    actualServos = ServosProperty.GetValue(actualControlGroup, null);

                    MoveRightMethod = IRControlGroupType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                    MoveLeftMethod = IRControlGroupType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                    MoveCenterMethod = IRControlGroupType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                    MoveNextPresetMethod = IRControlGroupType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                    MovePrevPresetMethod = IRControlGroupType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                    StopMethod = IRControlGroupType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
                }
                private Object actualControlGroup;

                private PropertyInfo NameProperty;
                public String Name
                {
                    get { return (String)NameProperty.GetValue(actualControlGroup, null); }
                    set { NameProperty.SetValue(actualControlGroup, value, null); }
                }

                private PropertyInfo ForwardKeyProperty;
                public String ForwardKey
                {
                    get { return (String)ForwardKeyProperty.GetValue(actualControlGroup, null); }
                    set { ForwardKeyProperty.SetValue(actualControlGroup, value, null); }
                }

                private PropertyInfo ReverseKeyProperty;
                public String ReverseKey
                {
                    get { return (String)ReverseKeyProperty.GetValue(actualControlGroup, null); }
                    set { ReverseKeyProperty.SetValue(actualControlGroup, value, null); }
                }

                private PropertyInfo SpeedProperty;
                public float Speed
                {
                    get { return (float)SpeedProperty.GetValue(actualControlGroup, null); }
                    set { SpeedProperty.SetValue(actualControlGroup, value, null); }
                }

                private PropertyInfo ExpandedProperty;
                public bool Expanded
                {
                    get { return (bool)ExpandedProperty.GetValue(actualControlGroup, null); }
                    set { ExpandedProperty.SetValue(actualControlGroup, value, null); }
                }

                private Object actualServos;
                private PropertyInfo ServosProperty;

                internal IRServosList Servos
                {
                    get
                    {
                        return ExtractServos(actualServos);
                    }
                }

                private IRServosList ExtractServos(Object actualServos)
                {
                    IRServosList ListToReturn = new IRServosList();
                    try
                    {
                        //iterate each "value" in the dictionary
                        foreach (var item in (IList)actualServos)
                        {
                            IRServo r1 = new IRServo(item);
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

                private MethodInfo MoveRightMethod;
                internal void MoveRight()
                {
                    MoveRightMethod.Invoke(actualControlGroup, new System.Object[] { });
                }

                private MethodInfo MoveLeftMethod;
                internal void MoveLeft()
                {
                    MoveLeftMethod.Invoke(actualControlGroup, new System.Object[] { });
                }

                private MethodInfo MoveCenterMethod;
                internal void MoveCenter()
                {
                    MoveCenterMethod.Invoke(actualControlGroup, new System.Object[] { });
                }

                private MethodInfo MoveNextPresetMethod;
                internal void MoveNextPreset()
                {
                    MoveNextPresetMethod.Invoke(actualControlGroup, new System.Object[] { });
                }

                private MethodInfo MovePrevPresetMethod;
                internal void MovePrevPreset()
                {
                    MovePrevPresetMethod.Invoke(actualControlGroup, new System.Object[] { });
                }

                private MethodInfo StopMethod;
                internal void Stop()
                {
                    StopMethod.Invoke(actualControlGroup, new System.Object[] { });
                }
            }

            public class IRServo
            {

                internal IRServo(Object s)
                {
                    actualServo = s;

                    NameProperty = IRServoPartType.GetProperty("Name");
                    HighlightProperty = IRServoPartType.GetProperty("Highlight");
                    
                    MechanismProperty = IRServoType.GetProperty("Mechanism");
                    actualServoMechanism = MechanismProperty.GetValue(actualServo, null);

                    PositionProperty = IRServoMechanismType.GetProperty("Position");
                    MinPositionProperty = IRServoMechanismType.GetProperty("MinPositionLimit");
                    MaxPositionProperty = IRServoMechanismType.GetProperty("MaxPositionLimit");

                    MinConfigPositionProperty = IRServoMechanismType.GetProperty("MinPosition");
                    MaxConfigPositionProperty = IRServoMechanismType.GetProperty("MaxPosition");

                    SpeedProperty = IRServoMechanismType.GetProperty("SpeedLimit");
                    ConfigSpeedProperty = IRServoMechanismType.GetProperty("DefaultSpeed");
                    CurrentSpeedProperty = IRServoMechanismType.GetProperty("CurrentSpeed");
                    AccelerationProperty = IRServoMechanismType.GetProperty("AccelerationLimit");
                    IsMovingProperty = IRServoMechanismType.GetProperty("IsMoving");
                    IsFreeMovingProperty = IRServoMechanismType.GetProperty("IsFreeMoving");
                    IsLockedProperty = IRServoMechanismType.GetProperty("IsLocked");
                    IsAxisInvertedProperty = IRServoMechanismType.GetProperty("IsAxisInverted");
                    
                    MoveRightMethod = IRServoMechanismType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                    MoveLeftMethod = IRServoMechanismType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                    MoveCenterMethod = IRServoMechanismType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                    MoveNextPresetMethod = IRServoMechanismType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                    MovePrevPresetMethod = IRServoMechanismType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                    StopMethod = IRServoMechanismType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);

                    MoveToMethod = IRServoMechanismType.GetMethod("MoveTo", new Type[] { typeof(float), typeof(float) });
                }
                private Object actualServo;

                private PropertyInfo MechanismProperty;
                private Object actualServoMechanism;

                private PropertyInfo NameProperty;
                public String Name
                {
                    get { return (String)NameProperty.GetValue(actualServo, null); }
                    set { NameProperty.SetValue(actualServo, value, null); }
                }

                private PropertyInfo HighlightProperty;
                public bool Highlight
                {
                    //get { return (bool)HighlightProperty.GetValue(actualServo, null); }
                    set { HighlightProperty.SetValue(actualServo, value, null); }
                }

                private PropertyInfo PositionProperty;
                public float Position
                {
                    get { return (float)PositionProperty.GetValue(actualServoMechanism, null); }
                }

                private PropertyInfo MinConfigPositionProperty;
                public float MinConfigPosition
                {
                    get { return (float)MinConfigPositionProperty.GetValue(actualServoMechanism, null); }
                }

                private PropertyInfo MaxConfigPositionProperty;
                public float MaxConfigPosition
                {
                    get { return (float)MaxConfigPositionProperty.GetValue(actualServoMechanism, null); }
                }

                private PropertyInfo MinPositionProperty;
                public float MinPosition
                {
                    get { return (float)MinPositionProperty.GetValue(actualServoMechanism, null); }
                    set { MinPositionProperty.SetValue(actualServoMechanism, value, null); }
                }

                private PropertyInfo MaxPositionProperty;
                public float MaxPosition
                {
                    get { return (float)MaxPositionProperty.GetValue(actualServoMechanism, null); }
                    set { MaxPositionProperty.SetValue(actualServoMechanism, value, null); }
                }

                private PropertyInfo ConfigSpeedProperty;
                public float ConfigSpeed
                {
                    get { return (float)ConfigSpeedProperty.GetValue(actualServoMechanism, null); }
                }

                private PropertyInfo SpeedProperty;
                public float Speed
                {
                    get { return (float)SpeedProperty.GetValue(actualServoMechanism, null); }
                    set { SpeedProperty.SetValue(actualServoMechanism, value, null); }
                }

                private PropertyInfo CurrentSpeedProperty;
                public float CurrentSpeed
                {
                    get { return (float)CurrentSpeedProperty.GetValue(actualServoMechanism, null); }
                    set { CurrentSpeedProperty.SetValue(actualServoMechanism, value, null); }
                }

                private PropertyInfo AccelerationProperty;
                public float Acceleration
                {
                    get { return (float)AccelerationProperty.GetValue(actualServoMechanism, null); }
                    set { AccelerationProperty.SetValue(actualServoMechanism, value, null); }
                }

                private PropertyInfo IsMovingProperty;
                public bool IsMoving
                {
                    get { return (bool)IsMovingProperty.GetValue(actualServoMechanism, null); }
                }

                private PropertyInfo IsFreeMovingProperty;
                public bool IsFreeMoving
                {
                    get { return (bool)IsFreeMovingProperty.GetValue(actualServoMechanism, null); }
                }

                private PropertyInfo IsLockedProperty;
                public bool IsLocked
                {
                    get { return (bool)IsLockedProperty.GetValue(actualServoMechanism, null); }
                    set { IsLockedProperty.SetValue(actualServoMechanism, value, null); }
                }

                private PropertyInfo IsAxisInvertedProperty;
                public bool IsAxisInverted
                {
                    get { return (bool)IsAxisInvertedProperty.GetValue(actualServoMechanism, null); }
                    set { IsAxisInvertedProperty.SetValue(actualServoMechanism, value, null); }
                }

                private MethodInfo MoveRightMethod;
                internal void MoveRight()
                {
                    MoveRightMethod.Invoke(actualServoMechanism, new System.Object[] { });
                }

                private MethodInfo MoveLeftMethod;
                internal void MoveLeft()
                {
                    MoveLeftMethod.Invoke(actualServoMechanism, new System.Object[] { });
                }

                private MethodInfo MoveCenterMethod;
                internal void MoveCenter()
                {
                    MoveCenterMethod.Invoke(actualServoMechanism, new System.Object[] { });
                }

                private MethodInfo MoveNextPresetMethod;
                internal void MoveNextPreset()
                {
                    MoveNextPresetMethod.Invoke(actualServoMechanism, new System.Object[] { });
                }

                private MethodInfo MovePrevPresetMethod;
                internal void MovePrevPreset()
                {
                    MovePrevPresetMethod.Invoke(actualServoMechanism, new System.Object[] { });
                }

                private MethodInfo MoveToMethod;
                internal void MoveTo(float position, float speed)
                {
                    MoveToMethod.Invoke(actualServoMechanism, new System.Object[] {position, speed });
                }

                private MethodInfo StopMethod;
                internal void Stop()
                {
                    StopMethod.Invoke(actualServoMechanism, new System.Object[] { });
                }

                public override bool Equals(object o)
                {
                    var servo = o as IRServo;
                    return servo != null && actualServo.Equals(servo.actualServo);
                }

                public override int GetHashCode()
                {
                    return (actualServo != null ? actualServo.GetHashCode() : 0);
                }

                public static bool operator ==(IRServo left, IRServo right)
                {
                    return Equals(left, right);
                }

                public static bool operator !=(IRServo left, IRServo right)
                {
                    return !Equals(left, right);
                }

                protected bool Equals(IRServo other)
                {
                    return Equals(actualServo, other.actualServo);
                }
            }

            public class IRServoGroupsList : List<IRControlGroup>
            {

            }

            public class IRServosList : List<IRServo>
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