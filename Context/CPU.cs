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
    public enum CPUMode
    {
        READY,
        STARVED,
        OFF
    };

    public enum KOSRunType
    {
        KSP,
        WINFORMS
    };

    public sealed class CPU : ExecutionContext, ICPU
    {
        private const int CLOCK_SPEED = 5;
        private readonly IBindingManager bindingManager;
        private readonly string context;
        private readonly List<KOSExternalFunction> externalFunctions = new List<KOSExternalFunction>();
        private readonly object parent;
        private readonly Dictionary<string, Variable> variables = new Dictionary<string, Variable>();
        private readonly List<IVolume> volumes = new List<IVolume>();
        private IVolume selectedVolume;

        static CPU()
        {
            RunType = KOSRunType.KSP;
        }

        public CPU(object parent, string context)
        {
            Mode = CPUMode.READY;
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

            RegisterkOSExternalFunction(new object[] {"test2", this, "testFunction", 2});
        }

        public static KOSRunType RunType { get; private set; }

        public IVolume Archive { get; private set; }
        public float SessionTime { get; private set; }

        public override Vessel Vessel
        {
            get { return ((IProcessorModule) parent).vessel; }
        }

        public override IDictionary<string, Variable> Variables
        {
            get { return variables; }
        }

        public override List<IVolume> Volumes
        {
            get { return volumes; }
        }

        public override List<KOSExternalFunction> ExternalFunctions
        {
            get { return externalFunctions; }
        }


        public override IVolume SelectedVolume
        {
            get { return selectedVolume; }
            set { selectedVolume = value; }
        }

        public CPUMode Mode { get; set; }

        public double TestFunction(double x, double y)
        {
            return x*y;
        }

        public void RegisterkOSExternalFunction(object[] parameters)
        {
            if (parameters.Count() != 4) return;

            var name = (string) parameters[0];
            var externalParent = parameters[1];
            var methodName = (string) parameters[2];
            var parameterCount = (int) parameters[3];

            RegisterkOSExternalFunction(name, externalParent, methodName, parameterCount);
        }

        public void RegisterkOSExternalFunction(string name, object externalParent, string methodName,
                                                int parameterCount)
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

                    if (paramType == typeof (string))
                    {
                        converted = parameters[i];
                    }
                    else if (paramType == typeof (float))
                    {
                        float flt;
                        if (float.TryParse(value, out flt)) converted = flt;
                    }
                    else if (paramType == typeof (double))
                    {
                        double dbl;
                        if (double.TryParse(value, out dbl)) converted = dbl;
                    }
                    else if (paramType == typeof (int))
                    {
                        int itgr;
                        if (int.TryParse(value, out itgr)) converted = itgr;
                    }
                    else if (paramType == typeof (long))
                    {
                        long lng;
                        if (long.TryParse(value, out lng)) converted = lng;
                    }
                    else if (paramType == typeof (bool))
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
            Mode = CPUMode.READY;

            Push(new InterpreterBootup(this));

            SelectedVolume = Volumes.Count > 1 ? Volumes[1] : Volumes[0];
        }

        public bool IsAlive()
        {
            var partState = ((IProcessorModule) parent).part.State;

            if (partState == PartStates.DEAD)
            {
                Mode = CPUMode.OFF;
                return false;
            }

            return true;
        }

        public void AttachVolume(IVolume hardDisk)
        {
            Volumes.Add(hardDisk);
            SelectedVolume = hardDisk;
        }

        public override void VerifyMount()
        {
            selectedVolume.CheckRange();
        }

        public void ProcessElectricity(Part part, float time)
        {
            if (Mode == CPUMode.OFF) return;

            var electricReq = time*selectedVolume.RequiredPower();
            var result = part.RequestResource("ElectricCharge", electricReq)/electricReq;

            var newMode = (result < 0.5f) ? CPUMode.STARVED : CPUMode.READY;

            if (newMode == CPUMode.READY && Mode == CPUMode.STARVED)
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
                return (BoundVariable) variables[varName];
            }
            throw new KOSException("Cannot bind " + varName + "; name already taken.");
        }

        public override void Update(float time)
        {
            bindingManager.Update(time);

            SessionTime += time;

            for (var i = 0; i < CLOCK_SPEED; i++)
            {
                base.Update(time/CLOCK_SPEED);
            }

            switch (Mode)
            {
                case CPUMode.STARVED:
                    ChildContext = null;
                    break;
                case CPUMode.OFF:
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
                    Mode = CPUMode.OFF;
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

        public void UpdateVolumeMounts(IList<IVolume> attachedVolumes)
        {
            // Remove volumes that are no longer attached
            foreach (var volume in new List<IVolume>(volumes))
            {
                if (!(volume is Archive) && !attachedVolumes.Contains(volume))
                {
                    volumes.Remove(volume);
                }
            }

            // Add volumes that have become attached
            foreach (var volume in attachedVolumes)
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

        public override string GetVolumeBestIdentifier(IVolume selected)
        {
            var localIndex = volumes.IndexOf(selected);

            if (!string.IsNullOrEmpty(selected.Name)) return "#" + localIndex + ": \"" + selected.Name + "\"";
            return "#" + localIndex;
        }

        public void UpdateUnitId(int unitID)
        {
            throw new NotImplementedException();
        }
    }
}