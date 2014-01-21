using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Binding;
using kOS.Debug;
using kOS.Expression;
using kOS.Interpreter;
using kOS.Module;
using kOS.Persistance;

namespace kOS.Context
{
    public sealed class CPU : ExecutionContext
    {
        public enum Modes { READY, STARVED, OFF };
        public enum KOSRunType { KSP, WINFORMS };

        public Archive Archive { get; private set; }
        public float SessionTime { get; private set; }
        public override Vessel Vessel { get { return ((kOSProcessor)parent).vessel; } }
        public override Dictionary<String, Variable> Variables { get { return variables; } }
        public override List<Volume> Volumes { get  { return volumes; } }
        public override List<KOSExternalFunction> ExternalFunctions { get { return externalFunctions; } }


        private const int CLOCK_SPEED = 5;
        private readonly string context;
        private readonly BindingManager bindingManager;
        private readonly object parent;
        private readonly Dictionary<String, Variable> variables = new Dictionary<String, Variable>();
        private Volume selectedVolume;
        private readonly List<Volume> volumes = new List<Volume>();
        private readonly List<KOSExternalFunction> externalFunctions = new List<KOSExternalFunction>();
        
        public static KOSRunType RunType = KOSRunType.KSP;
        
        public override Volume SelectedVolume
        {
            get { return selectedVolume; }
            set { selectedVolume = value; }
        }

        public Modes Mode { get; set; }

        public CPU(object parent, string context)
        {
            Mode = Modes.READY;
            this.parent = parent;
            this.context = context;
            
            bindingManager = new BindingManager(this, this.context);

            if (context == "ksp")
            {
                RunType = KOSRunType.KSP;

                if (Vessel != null) Archive = new Archive(Vessel);
                Volumes.Add(Archive);
            }
            else
            {
                RunType = KOSRunType.WINFORMS;
            }

            RegisterkOSExternalFunction(new object[] { "test2", this, "testFunction", 2 });
        }

        public double TestFunction(double x, double y) { return x * y; }

        public void RegisterkOSExternalFunction(object[] parameters)
        {
            if (parameters.Count() != 4) return;

            var name = (String)parameters[0];
            var externalParent = parameters[1];
            var methodName = (String)parameters[2];
            var parameterCount = (int)parameters[3];

            RegisterkOSExternalFunction(name, externalParent, methodName, parameterCount);
        }

        public void RegisterkOSExternalFunction(String name, object externalParent, String methodName, int parameterCount)
        {
            externalFunctions.Add(new KOSExternalFunction(name.ToUpper(), externalParent, methodName, parameterCount));
        }

        public override object CallExternalFunction(string name, string[] parameters)
        {
            var callFound = false;
            var callAndParamCountFound = false;

            foreach (var function in ExternalFunctions)
            {
                if (function.Name != name.ToUpper()) continue;

                callFound = true;

                if (function.ParameterCount != parameters.Count()) continue;

                callAndParamCountFound = true;

                var t = function.Parent.GetType();
                var method = t.GetMethod(function.MethodName);

                // Attempt to cast the strings to types that the target method is expecting
                var parameterInfoArray = method.GetParameters();
                var convertedParams = new object[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameterInfoArray[i].ParameterType;
                    var value = parameters[i];
                    object converted = null;
                            
                    if (paramType == typeof(String))
                    {
                        converted = parameters[i];
                    }
                    else if (paramType == typeof(float))
                    {
                        float flt;
                        if (float.TryParse(value, out flt)) converted = flt;
                    }
                    else if (paramType == typeof(double))
                    {
                        double dbl;
                        if (double.TryParse(value, out dbl)) converted = dbl;
                    }
                    else if (paramType == typeof(int))
                    {
                        int itgr;
                        if (int.TryParse(value, out itgr)) converted = itgr;
                    }
                    else if (paramType == typeof(long))
                    {
                        long lng;
                        if (long.TryParse(value, out lng)) converted = lng;
                    }
                    else if (paramType == typeof(bool))
                    {
                        bool bln;
                        if (bool.TryParse(value, out bln)) converted = bln;
                    }

                    if (converted == null) throw new KOSException("Parameter types don't match");
                    convertedParams[i] = converted;
                }

                return method.Invoke(function.Parent, convertedParams);
            }

            if (!callFound) throw new KOSException("External function '" + name + "' not found");
            else if (!callAndParamCountFound) throw new KOSException("Wrong number of arguments for '" + name + "'");

            return null;
        }

        public override bool FindExternalFunction(string name)
        {
            return ExternalFunctions.Any(function => function.Name == name.ToUpper());
        }

        public void Boot()
        {
            Mode = Modes.READY;

            Push(new InterpreterBootup(this));

            SelectedVolume = Volumes.Count > 1 ? Volumes[1] : Volumes[0];
        }

        public bool IsAlive()
        {
            var partState = ((kOSProcessor)parent).part.State;

            if (partState == PartStates.DEAD)
            {
                Mode = Modes.OFF;
                return false;
            }

            return true;
        }

        public void AttachHardDisk(Harddisk hardDisk)
        {
            Volumes.Add(hardDisk);
            SelectedVolume = hardDisk;
        }

        public override void VerifyMount()
        {
            selectedVolume.CheckRange();
        }

        internal void ProcessElectricity(Part part, float time)
        {
            if (Mode == Modes.OFF) return;

            var electricReq = 0.01f * time;
            var result = part.RequestResource("ElectricCharge", electricReq) / electricReq;

            var newMode = (result < 0.5f) ? Modes.STARVED : Modes.READY;

            if (newMode == Modes.READY && Mode == Modes.STARVED)
            {
                Boot();
            }

            Mode = newMode;
        }

        public override bool SwitchToVolume(int volID)
        {
            if (Volumes.Count > volID)
            {
                var newVolume = Volumes[volID];

                if (newVolume.CheckRange())
                {
                    SelectedVolume = newVolume;
                    return true;
                }
                throw new KOSException("Volume disconnected - out of range");
            }

            return false;
        }

        public override bool SwitchToVolume(string targetVolume)
        {
            foreach (var volume in Volumes.Where(volume => volume.Name.ToUpper() == targetVolume.ToUpper()))
            {
                if (volume.CheckRange())
                {
                    SelectedVolume = volume;
                    return true;
                }
                throw new KOSException("Volume disconnected - out of range");
            }

            return false;
        }

        public override BoundVariable CreateBoundVariable(string varName)
        {
            varName = varName.ToLower();

            if (FindVariable(varName) == null)
            {
                variables.Add(varName, new BoundVariable());
                return (BoundVariable)variables[varName];
            }
            throw new KOSException("Cannot bind " + varName + "; name already taken.");
        }

        public override void Update(float time)
        {
            bindingManager.Update(time);

            SessionTime += time;

            for (var i = 0; i < CLOCK_SPEED; i++)
            {
                base.Update(time / CLOCK_SPEED);
            }

            switch (Mode)
            {
                case Modes.STARVED:
                    ChildContext = null;
                    break;
                case Modes.OFF:
                    ChildContext = null;
                    break;
            }

            // After booting
            if (ChildContext == null)
            {
                Push(new ImmediateMode(this));
            }
        }

        public override void SendMessage(SystemMessage message)
        {
            switch (message)
            {
                case SystemMessage.SHUTDOWN:
                    ChildContext = null;
                    Mode = Modes.OFF;
                    break;

                case SystemMessage.RESTART:
                    ChildContext = null;
                    Boot();
                    break;

                default:
                    base.SendMessage(message);
                    break;
            }
        }

        internal void UpdateVolumeMounts(List<Volume> attachedVolumes)
        {
            // Remove volumes that are no longer attached
            foreach (Volume volume in new List<Volume>(volumes))
            {
                if (!(volume is Archive) && !attachedVolumes.Contains(volume))
                {
                    volumes.Remove(volume);
                }
            }

            // Add volumes that have become attached
            foreach (Volume volume in attachedVolumes)
            {
                if (!volumes.Contains(volume))
                {
                    volumes.Add(volume);
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            var contextNode = new ConfigNode("context");

            // Save variables
            if (Variables.Count > 0)
            {
                var varNode = new ConfigNode("variables");

                foreach (var kvp in Variables.Where(kvp => !(kvp.Value is BoundVariable)))
                {
                    varNode.AddValue(kvp.Key, File.EncodeLine(kvp.Value.Value.ToString()));
                }

                contextNode.AddNode(varNode);
            }

            if (ChildContext != null)
            {
                ChildContext.OnSave(contextNode);
            }

            node.AddNode(contextNode);
        }

        public override void OnLoad(ConfigNode node)
        {
            foreach (var contextNode in node.GetNodes("context"))
            {
                foreach (var varNode in contextNode.GetNodes("variables"))
                {
                    foreach (ConfigNode.Value value in varNode.values)
                    {
                        try
                        {
                            var newVar = CreateVariable(value.name);
                            newVar.Value = new Expression.Expression(File.DecodeLine(value.value), this).GetValue();
                        }
                        catch (KOSException ex)
                        {
                            UnityEngine.Debug.Log("kOS Exception Onload: " + ex.Message);
                        }
                    }
                }
            }
        }

        public override string GetVolumeBestIdentifier(Volume selected)
        {
            var localIndex = volumes.IndexOf(selected);

            if (!String.IsNullOrEmpty(selected.Name)) return "#" + localIndex + ": \"" + selected.Name + "\"";
            return "#" + localIndex;
        }

        public void UpdateUnitId(int unitID)
        {
            throw new NotImplementedException();
        }
    }
}
