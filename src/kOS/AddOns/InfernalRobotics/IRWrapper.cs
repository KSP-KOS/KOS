using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// TODO: Change this namespace to something specific to your plugin here.
namespace kOS.AddOns.InfernalRobotics
{
    public class IRWrapper
    {
        private static bool isWrapped;

        protected internal static Type IRServoControllerType { get; set; }
        protected internal static Type IRControlGroupType { get; set; }
        protected internal static Type IRServoType { get; set; }
        protected internal static Type IRServoPartType { get; set; }
        protected internal static Type IRServoMechanismType { get; set; }
        protected internal static object ActualServoController { get; set; }

        internal static IRAPI IRController { get; set; }
        internal static bool AssemblyExists { get { return (IRServoControllerType != null); } }
        internal static bool InstanceExists { get { return (IRController != null); } }
        internal static bool APIReady { get { return isWrapped && IRController.Ready; } }

        internal static bool InitWrapper()
        {
            isWrapped = false;
            ActualServoController = null;
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
                var propertyInfo = IRServoControllerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

                if (propertyInfo == null)
                    LogFormatted("[IR Wrapper] Cannot find Instance Property");
                else
                    ActualServoController = propertyInfo.GetValue(null, null);
            }
            catch (Exception e)
            {
                LogFormatted("No Instance found, " + e.Message);
            }

            if (ActualServoController == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            LogFormatted("Got Instance, Creating Wrapper Objects");
            IRController = new InfernalRoboticsAPI(ActualServoController);
            isWrapped = true;
            return true;
        }

        #region Private Implementation

        private class InfernalRoboticsAPI : IRAPI
        {
            private PropertyInfo apiReady;
            private object actualServoGroups;

            public InfernalRoboticsAPI(object irServoController)
            {
                DetermineReady();
                BuildServoGroups(irServoController);
            }

            private void BuildServoGroups(object irServoController)
            {
                LogFormatted("Getting ServoGroups Object");
                var servoGroupsField = IRServoControllerType.GetField("ServoGroups");
                if (servoGroupsField == null)
                    LogFormatted("Failed Getting ServoGroups fieldinfo");
                else
                {
                    actualServoGroups = servoGroupsField.GetValue(irServoController);
                    LogFormatted("Success: " + (actualServoGroups != null));
                }
            }

            private void DetermineReady()
            {
                LogFormatted("Getting APIReady Object");
                apiReady = IRServoControllerType.GetProperty("APIReady", BindingFlags.Public | BindingFlags.Static);
                LogFormatted("Success: " + (apiReady != null));
            }

            public bool Ready
            {
                get
                {
                    if (apiReady == null)
                        return false;

                    return (bool)apiReady.GetValue(null, null);
                }
            }

            public IList<IControlGroup> ServoGroups
            {
                get
                {
                    return ExtractServoGroups(actualServoGroups);
                }
            }

            private IList<IControlGroup> ExtractServoGroups(object servoGroups)
            {
                var listToReturn = new List<IControlGroup>();

                if (servoGroups == null)
                    return listToReturn;

                try
                {
                    //iterate each "value" in the dictionary
                    foreach (var item in (IList)servoGroups)
                    {
                        listToReturn.Add(new IRControlGroup(item));
                    }
                }
                catch (Exception ex)
                {
                    LogFormatted("Cannot list ServoGroups: {0}", ex.Message);
                }
                return listToReturn;
            }
        }

        private class IRControlGroup : IControlGroup
        {
            private readonly object actualControlGroup;

            private PropertyInfo nameProperty;
            private PropertyInfo forwardKeyProperty;
            private PropertyInfo expandedProperty;
            private PropertyInfo speedProperty;
            private PropertyInfo reverseKeyProperty;

            private MethodInfo moveRightMethod;
            private MethodInfo moveLeftMethod;
            private MethodInfo moveCenterMethod;
            private MethodInfo moveNextPresetMethod;
            private MethodInfo movePrevPresetMethod;
            private MethodInfo stopMethod;

            public IRControlGroup(object cg)
            {
                actualControlGroup = cg;
                FindProperties();
                FindMethods();
            }

            private void FindProperties()
            {
                nameProperty = IRControlGroupType.GetProperty("Name");
                forwardKeyProperty = IRControlGroupType.GetProperty("ForwardKey");
                reverseKeyProperty = IRControlGroupType.GetProperty("ReverseKey");
                speedProperty = IRControlGroupType.GetProperty("Speed");
                expandedProperty = IRControlGroupType.GetProperty("Expanded");

                var servosProperty = IRControlGroupType.GetProperty("Servos");
                ActualServos = servosProperty.GetValue(actualControlGroup, null);
            }

            private void FindMethods()
            {
                moveRightMethod = IRControlGroupType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                moveLeftMethod = IRControlGroupType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                moveCenterMethod = IRControlGroupType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                moveNextPresetMethod = IRControlGroupType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                movePrevPresetMethod = IRControlGroupType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                stopMethod = IRControlGroupType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
            }

            public string Name
            {
                get { return (string)nameProperty.GetValue(actualControlGroup, null); }
                set { nameProperty.SetValue(actualControlGroup, value, null); }
            }

            public string ForwardKey
            {
                get { return (string)forwardKeyProperty.GetValue(actualControlGroup, null); }
                set { forwardKeyProperty.SetValue(actualControlGroup, value, null); }
            }

            public string ReverseKey
            {
                get { return (string)reverseKeyProperty.GetValue(actualControlGroup, null); }
                set { reverseKeyProperty.SetValue(actualControlGroup, value, null); }
            }

            public float Speed
            {
                get { return (float)speedProperty.GetValue(actualControlGroup, null); }
                set { speedProperty.SetValue(actualControlGroup, value, null); }
            }

            public bool Expanded
            {
                get { return (bool)expandedProperty.GetValue(actualControlGroup, null); }
                set { expandedProperty.SetValue(actualControlGroup, value, null); }
            }

            private object ActualServos { get; set; }

            public IList<IServo> Servos
            {
                get
                {
                    return ExtractServos(ActualServos);
                }
            }

            public void MoveRight()
            {
                moveRightMethod.Invoke(actualControlGroup, new object[] { });
            }

            public void MoveLeft()
            {
                moveLeftMethod.Invoke(actualControlGroup, new object[] { });
            }

            public void MoveCenter()
            {
                moveCenterMethod.Invoke(actualControlGroup, new object[] { });
            }

            public void MoveNextPreset()
            {
                moveNextPresetMethod.Invoke(actualControlGroup, new object[] { });
            }

            public void MovePrevPreset()
            {
                movePrevPresetMethod.Invoke(actualControlGroup, new object[] { });
            }

            public void Stop()
            {
                stopMethod.Invoke(actualControlGroup, new object[] { });
            }

            private IList<IServo> ExtractServos(object actualServos)
            {
                var listToReturn = new List<IServo>();
                
                if (actualServos == null)
                    return listToReturn;

                try
                {
                    //iterate each "value" in the dictionary
                    foreach (var item in (IList)actualServos)
                    {
                        listToReturn.Add(new IRServo(item));
                    }
                }
                catch (Exception ex)
                {
                    LogFormatted("Error extracting from actualServos: {0}", ex.Message);
                }
                return listToReturn;
            }

            public bool Equals(IControlGroup other)
            {
                var controlGroup = other as IRControlGroup;
                return controlGroup != null && Equals(controlGroup);
            }
        }

        public class IRServo : IServo
        {
            private object actualServoMechanism;

            private PropertyInfo maxConfigPositionProperty;
            private PropertyInfo minPositionProperty;
            private PropertyInfo maxPositionProperty;
            private PropertyInfo configSpeedProperty;
            private PropertyInfo speedProperty;
            private PropertyInfo currentSpeedProperty;
            private PropertyInfo accelerationProperty;
            private PropertyInfo isMovingProperty;
            private PropertyInfo isFreeMovingProperty;
            private PropertyInfo isLockedProperty;
            private PropertyInfo isAxisInvertedProperty;
            private PropertyInfo nameProperty;
            private PropertyInfo highlightProperty;
            private PropertyInfo positionProperty;
            private PropertyInfo minConfigPositionProperty;

            private PropertyInfo UIDProperty;

            private MethodInfo moveRightMethod;
            private MethodInfo moveLeftMethod;
            private MethodInfo moveCenterMethod;
            private MethodInfo moveNextPresetMethod;
            private MethodInfo movePrevPresetMethod;
            private MethodInfo moveToMethod;
            private MethodInfo stopMethod;

            public IRServo(object s)
            {
                actualServo = s;

                FindProperties();
                FindMethods();
            }

            private void FindProperties()
            {
                nameProperty = IRServoPartType.GetProperty("Name");
                highlightProperty = IRServoPartType.GetProperty("Highlight");
                UIDProperty = IRServoPartType.GetProperty ("UID");

                var mechanismProperty = IRServoType.GetProperty("Mechanism");
                actualServoMechanism = mechanismProperty.GetValue(actualServo, null);

                positionProperty = IRServoMechanismType.GetProperty("Position");
                minPositionProperty = IRServoMechanismType.GetProperty("MinPositionLimit");
                maxPositionProperty = IRServoMechanismType.GetProperty("MaxPositionLimit");

                minConfigPositionProperty = IRServoMechanismType.GetProperty("MinPosition");
                maxConfigPositionProperty = IRServoMechanismType.GetProperty("MaxPosition");

                speedProperty = IRServoMechanismType.GetProperty("SpeedLimit");
                configSpeedProperty = IRServoMechanismType.GetProperty("DefaultSpeed");
                currentSpeedProperty = IRServoMechanismType.GetProperty("CurrentSpeed");
                accelerationProperty = IRServoMechanismType.GetProperty("AccelerationLimit");
                isMovingProperty = IRServoMechanismType.GetProperty("IsMoving");
                isFreeMovingProperty = IRServoMechanismType.GetProperty("IsFreeMoving");
                isLockedProperty = IRServoMechanismType.GetProperty("IsLocked");
                isAxisInvertedProperty = IRServoMechanismType.GetProperty("IsAxisInverted");
            }

            private void FindMethods()
            {
                moveRightMethod = IRServoMechanismType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                moveLeftMethod = IRServoMechanismType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                moveCenterMethod = IRServoMechanismType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                moveNextPresetMethod = IRServoMechanismType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                movePrevPresetMethod = IRServoMechanismType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                stopMethod = IRServoMechanismType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
                moveToMethod = IRServoMechanismType.GetMethod("MoveTo", new[] { typeof(float), typeof(float) });
            }

            private readonly object actualServo;

            public uint UID
            {
                get { return (uint)UIDProperty.GetValue(actualServo, null); }
            }

            public string Name
            {
                get { return (string)nameProperty.GetValue(actualServo, null); }
                set { nameProperty.SetValue(actualServo, value, null); }
            }

            public bool Highlight
            {
                //get { return (bool)HighlightProperty.GetValue(actualServo, null); }
                set { highlightProperty.SetValue(actualServo, value, null); }
            }

            public float Position
            {
                get { return (float)positionProperty.GetValue(actualServoMechanism, null); }
            }

            public float MinConfigPosition
            {
                get { return (float)minConfigPositionProperty.GetValue(actualServoMechanism, null); }
            }

            public float MaxConfigPosition
            {
                get { return (float)maxConfigPositionProperty.GetValue(actualServoMechanism, null); }
            }

            public float MinPosition
            {
                get { return (float)minPositionProperty.GetValue(actualServoMechanism, null); }
                set { minPositionProperty.SetValue(actualServoMechanism, value, null); }
            }

            public float MaxPosition
            {
                get { return (float)maxPositionProperty.GetValue(actualServoMechanism, null); }
                set { maxPositionProperty.SetValue(actualServoMechanism, value, null); }
            }

            public float ConfigSpeed
            {
                get { return (float)configSpeedProperty.GetValue(actualServoMechanism, null); }
            }

            public float Speed
            {
                get { return (float)speedProperty.GetValue(actualServoMechanism, null); }
                set { speedProperty.SetValue(actualServoMechanism, value, null); }
            }

            public float CurrentSpeed
            {
                get { return (float)currentSpeedProperty.GetValue(actualServoMechanism, null); }
                set { currentSpeedProperty.SetValue(actualServoMechanism, value, null); }
            }

            public float Acceleration
            {
                get { return (float)accelerationProperty.GetValue(actualServoMechanism, null); }
                set { accelerationProperty.SetValue(actualServoMechanism, value, null); }
            }

            public bool IsMoving
            {
                get { return (bool)isMovingProperty.GetValue(actualServoMechanism, null); }
            }

            public bool IsFreeMoving
            {
                get { return (bool)isFreeMovingProperty.GetValue(actualServoMechanism, null); }
            }

            public bool IsLocked
            {
                get { return (bool)isLockedProperty.GetValue(actualServoMechanism, null); }
                set { isLockedProperty.SetValue(actualServoMechanism, value, null); }
            }

            public bool IsAxisInverted
            {
                get { return (bool)isAxisInvertedProperty.GetValue(actualServoMechanism, null); }
                set { isAxisInvertedProperty.SetValue(actualServoMechanism, value, null); }
            }

            public void MoveRight()
            {
                moveRightMethod.Invoke(actualServoMechanism, new object[] { });
            }

            public void MoveLeft()
            {
                moveLeftMethod.Invoke(actualServoMechanism, new object[] { });
            }

            public void MoveCenter()
            {
                moveCenterMethod.Invoke(actualServoMechanism, new object[] { });
            }

            public void MoveNextPreset()
            {
                moveNextPresetMethod.Invoke(actualServoMechanism, new object[] { });
            }

            public void MovePrevPreset()
            {
                movePrevPresetMethod.Invoke(actualServoMechanism, new object[] { });
            }

            public void MoveTo(float position, float speed)
            {
                moveToMethod.Invoke(actualServoMechanism, new object[] { position, speed });
            }

            public void Stop()
            {
                stopMethod.Invoke(actualServoMechanism, new object[] { });
            }

            public bool Equals(IServo other)
            {
                var servo = other as IRServo;
                return servo != null && Equals(servo);
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

        #endregion Private Implementation

        #region API Contract

        public interface IRAPI
        {
            bool Ready { get; }

            IList<IControlGroup> ServoGroups { get; }
        }

        public interface IControlGroup : IEquatable<IControlGroup>
        {
            string Name { get; set; }

            string ForwardKey { get; set; }

            string ReverseKey { get; set; }

            float Speed { get; set; }

            bool Expanded { get; set; }

            IList<IServo> Servos { get; }

            void MoveRight();

            void MoveLeft();

            void MoveCenter();

            void MoveNextPreset();

            void MovePrevPreset();

            void Stop();
        }

        public interface IServo : IEquatable<IServo>
        {
            uint UID { get; }

            string Name { get; set; }

            bool Highlight { set; }

            float Position { get; }

            float MinConfigPosition { get; }

            float MaxConfigPosition { get; }

            float MinPosition { get; set; }

            float MaxPosition { get; set; }

            float ConfigSpeed { get; }

            float Speed { get; set; }

            float CurrentSpeed { get; set; }

            float Acceleration { get; set; }

            bool IsMoving { get; }

            bool IsFreeMoving { get; }

            bool IsLocked { get; set; }

            bool IsAxisInverted { get; set; }

            void MoveRight();

            void MoveLeft();

            void MoveCenter();

            void MoveNextPreset();

            void MovePrevPreset();

            void MoveTo(float position, float speed);

            void Stop();

            bool Equals(object o);

            int GetHashCode();
        }

        #endregion API Contract

        #region Logging Stuff

        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="message">Text to be printed - can be formatted as per string.format</param>
        /// <param name="strParams">Objects to feed into a string.format</param>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(string message, params object[] strParams)
        {
            LogFormatted(message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="message">Text to be printed - can be formatted as per string.format</param>
        /// <param name="strParams">Objects to feed into a string.format</param>
        internal static void LogFormatted(string message, params object[] strParams)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var declaringType = MethodBase.GetCurrentMethod().DeclaringType;
            message = string.Format(message, strParams);

            string strMessageLine = declaringType != null ?
                string.Format("{0},{2}-{3},{1}", DateTime.Now, message, assemblyName, declaringType.Name) :
                string.Format("{0},{2}-NO-DECLARE,{1}", DateTime.Now, message, assemblyName);

            UnityEngine.Debug.Log(strMessageLine);
        }

        #endregion Logging Stuff
    }
}