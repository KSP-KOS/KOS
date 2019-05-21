using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// Infernal Robotics Next is the only version of IR that is supported
// This wrapper was taken from https://github.com/meirumeiru/IR-Sequencer/blob/master/IRSequencer/IRSequencer/API/IRWrapper.cs
// And represents the most recent recommended wrapper as of April 2019
namespace kOS.AddOns.InfernalRobotics
{
    public class IRWrapper
    {
        private static bool isWrapped;

        protected internal static Type IRControllerType { get; set; }
        protected internal static Type IRControlGroupType { get; set; }

        protected internal static Type IRServoType { get; set; }
        protected internal static Type IRMotorType { get; set; }

        protected internal static object ActualController { get; set; }

        internal static IRAPI IRController { get; set; }
        internal static bool AssemblyExists { get { return (IRControllerType != null); } }
        internal static bool InstanceExists { get { return (IRController != null); } }
        internal static bool APIReady { get { return isWrapped && IRController.Ready; } }

        internal static bool InitWrapper()
        {
            isWrapped = false;
            ActualController = null;
            IRController = null;
            LogFormatted("Attempting to Grab IR Types...");

            IRControllerType = null;

            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName == "InfernalRobotics_v3.Command.Controller") { IRControllerType = t; }
            });

            if (IRControllerType == null)
                return false;

            LogFormatted("IR Version:{0}", IRControllerType.Assembly.GetName().Version.ToString());

            IRMotorType = null;
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName == "InfernalRobotics_v3.Interfaces.IMotor") { IRMotorType = t; }
            });

            if (IRMotorType == null)
            {
                LogFormatted("[IR Wrapper] Failed to grab Motor Type");
                return false;
            }

            IRServoType = null;
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName == "InfernalRobotics_v3.Interfaces.IServo") { IRServoType = t; }
            });

            if (IRServoType == null)
            {
                LogFormatted("[IR Wrapper] Failed to grab Servo Type");
                return false;
            }

            IRControlGroupType = null;
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName == "InfernalRobotics_v3.Command.ControlGroup") { IRControlGroupType = t; }
            });

            if (IRControlGroupType == null)
            {
                LogFormatted("[IR Wrapper] Failed to grab ControlGroup Type");
                return false;
            }

            LogFormatted("Got Assembly Types, grabbing Instance");

            try
            {
                var propertyInfo = IRControllerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

                if (propertyInfo == null)
                    LogFormatted("[IR Wrapper] Cannot find Instance Property");
                else
                    ActualController = propertyInfo.GetValue(null, null);
            }
            catch (Exception e)
            {
                LogFormatted("No Instance found, " + e.Message);
            }

            if (ActualController == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            LogFormatted("Got Instance, Creating Wrapper Objects");
            IRController = new InfernalRoboticsAPI();
            isWrapped = true;
            return true;
        }

        #region Private Implementation

        private class InfernalRoboticsAPI : IRAPI
        {
            private PropertyInfo apiReady;
            private object actualServoGroups;

            public InfernalRoboticsAPI()
            {
                DetermineReady();
                BuildServoGroups();
            }

            private void BuildServoGroups()
            {
                var servoGroupsField = IRControllerType.GetField("ServoGroups");
                if (servoGroupsField == null)
                    LogFormatted("Failed Getting ServoGroups fieldinfo");
                else if (IRWrapper.ActualController == null)
                    LogFormatted("ServoController Instance not found");
                else
                    actualServoGroups = servoGroupsField.GetValue(IRWrapper.ActualController);
            }

            private void DetermineReady()
            {
                LogFormatted("Getting APIReady Object");
                apiReady = IRControllerType.GetProperty("APIReady", BindingFlags.Public | BindingFlags.Static);
                LogFormatted("Success: " + (apiReady != null));
            }

            public bool Ready
            {
                get
                {
                    if (apiReady == null || actualServoGroups == null)
                        return false;

                    return (bool)apiReady.GetValue(null, null);
                }
            }

            public IList<IControlGroup> ServoGroups
            {
                get
                {
                    BuildServoGroups();
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
                        listToReturn.Add(new IRControlGroup(item));
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
            private PropertyInfo vesselProperty;
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
                vesselProperty = IRControlGroupType.GetProperty("Vessel");
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

            public Vessel Vessel
            {
                get { return vesselProperty != null ? (Vessel)vesselProperty.GetValue(actualControlGroup, null) : null; }
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
                get { return ExtractServos(ActualServos); }
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
            private object actualServoMotor;

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
            private PropertyInfo HostPartProperty;

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
                nameProperty = IRServoType.GetProperty("Name");
                highlightProperty = IRServoType.GetProperty("Highlight");
                UIDProperty = IRServoType.GetProperty("UID");
                HostPartProperty = IRServoType.GetProperty("HostPart");

                positionProperty = IRServoType.GetProperty("Position");
                minPositionProperty = IRServoType.GetProperty("MinPositionLimit");
                maxPositionProperty = IRServoType.GetProperty("MaxPositionLimit");

                minConfigPositionProperty = IRServoType.GetProperty("MinPosition");
                maxConfigPositionProperty = IRServoType.GetProperty("MaxPosition");

                isMovingProperty = IRServoType.GetProperty("IsMoving");
                isFreeMovingProperty = IRServoType.GetProperty("IsFreeMoving");
                isLockedProperty = IRServoType.GetProperty("IsLocked");

                var motorProperty = IRServoType.GetProperty("Motor");
                actualServoMotor = motorProperty.GetValue(actualServo, null);

                speedProperty = IRMotorType.GetProperty("SpeedLimit");
                configSpeedProperty = IRMotorType.GetProperty("DefaultSpeed");
                currentSpeedProperty = IRMotorType.GetProperty("CurrentSpeed");
                accelerationProperty = IRMotorType.GetProperty("AccelerationLimit");
                isAxisInvertedProperty = IRMotorType.GetProperty("IsAxisInverted");
            }

            private void FindMethods()
            {
                moveRightMethod = IRMotorType.GetMethod("MoveRight", BindingFlags.Public | BindingFlags.Instance);
                moveLeftMethod = IRMotorType.GetMethod("MoveLeft", BindingFlags.Public | BindingFlags.Instance);
                moveCenterMethod = IRMotorType.GetMethod("MoveCenter", BindingFlags.Public | BindingFlags.Instance);
                moveNextPresetMethod = IRMotorType.GetMethod("MoveNextPreset", BindingFlags.Public | BindingFlags.Instance);
                movePrevPresetMethod = IRMotorType.GetMethod("MovePrevPreset", BindingFlags.Public | BindingFlags.Instance);
                stopMethod = IRMotorType.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
                moveToMethod = IRMotorType.GetMethod("MoveTo", new[] { typeof(float), typeof(float) });
            }

            private readonly object actualServo;

            public string Name
            {
                get { return (string)nameProperty.GetValue(actualServo, null); }
                set { nameProperty.SetValue(actualServo, value, null); }
            }

            public uint UID
            {
                get { return (uint)UIDProperty.GetValue(actualServo, null); }
            }

            public Part HostPart
            {
                get { return (Part)HostPartProperty.GetValue(actualServo, null); }
            }

            public bool Highlight
            {
                //get { return (bool)HighlightProperty.GetValue(actualServo, null); }
                set { highlightProperty.SetValue(actualServo, value, null); }
            }

            public float Position
            {
                get { return (float)positionProperty.GetValue(actualServo, null); }
            }

            public float MinConfigPosition
            {
                get { return (float)minConfigPositionProperty.GetValue(actualServo, null); }
            }

            public float MaxConfigPosition
            {
                get { return (float)maxConfigPositionProperty.GetValue(actualServo, null); }
            }

            public float MinPosition
            {
                get { return (float)minPositionProperty.GetValue(actualServo, null); }
                set { minPositionProperty.SetValue(actualServo, value, null); }
            }

            public float MaxPosition
            {
                get { return (float)maxPositionProperty.GetValue(actualServo, null); }
                set { maxPositionProperty.SetValue(actualServo, value, null); }
            }

            public float ConfigSpeed
            {
                get { return (float)configSpeedProperty.GetValue(actualServoMotor, null); }
            }

            public float Speed
            {
                get { return (float)speedProperty.GetValue(actualServoMotor, null); }
                set { speedProperty.SetValue(actualServoMotor, value, null); }
            }

            public float CurrentSpeed
            {
                get { return (float)currentSpeedProperty.GetValue(actualServoMotor, null); }
                set { currentSpeedProperty.SetValue(actualServoMotor, value, null); }
            }

            public float Acceleration
            {
                get { return (float)accelerationProperty.GetValue(actualServoMotor, null); }
                set { accelerationProperty.SetValue(actualServoMotor, value, null); }
            }

            public bool IsMoving
            {
                get { return (bool)isMovingProperty.GetValue(actualServo, null); }
            }

            public bool IsFreeMoving
            {
                get { return (bool)isFreeMovingProperty.GetValue(actualServo, null); }
            }

            public bool IsLocked
            {
                get { return (bool)isLockedProperty.GetValue(actualServo, null); }
                set { isLockedProperty.SetValue(actualServo, value, null); }
            }

            public bool IsAxisInverted
            {
                get { return (bool)isAxisInvertedProperty.GetValue(actualServoMotor, null); }
                set { isAxisInvertedProperty.SetValue(actualServoMotor, value, null); }
            }

            public void MoveRight()
            {
                moveRightMethod.Invoke(actualServoMotor, new object[] { });
            }

            public void MoveLeft()
            {
                moveLeftMethod.Invoke(actualServoMotor, new object[] { });
            }

            public void MoveCenter()
            {
                moveCenterMethod.Invoke(actualServoMotor, new object[] { });
            }

            public void MoveNextPreset()
            {
                moveNextPresetMethod.Invoke(actualServoMotor, new object[] { });
            }

            public void MovePrevPreset()
            {
                movePrevPresetMethod.Invoke(actualServoMotor, new object[] { });
            }

            public void MoveTo(float position, float speed)
            {
                moveToMethod.Invoke(actualServoMotor, new object[] { position, speed });
            }

            public void Stop()
            {
                stopMethod.Invoke(actualServoMotor, new object[] { });
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

            //can only be used in Flight, null checking is mandatory
            Vessel Vessel { get; }

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
            string Name { get; set; }

            uint UID { get; }

            Part HostPart { get; }

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