using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kOS
{
    public class CPU : ExecutionContext
    {
        public object Parent;
        public enum Modes { READY, STARVED, OFF };
        public Modes Mode = Modes.READY;
        public String Context;
        public Archive archive;
        public BindingManager bindingManager;
        public float SessionTime;

        private Dictionary<String, Variable> variables = new Dictionary<String, Variable>();
        private Volume selectedVolume = null;
        private List<Volume> volumes = new List<Volume>();
            
        public override Vessel Vessel { get { return ((kOSProcessor)Parent).vessel; } }
        public override Dictionary<String, Variable> Variables { get { return variables; } }
        public override List<Volume> Volumes { get  { return volumes;} }
                
        public override Volume SelectedVolume
        {
            get { return selectedVolume; }
            set { selectedVolume = value; }
        }

        public CPU(object parent, string context)
        {
            this.Parent = parent;
            this.Context = context;

            bindingManager = new BindingManager(this, Context);
            
            if (context == "ksp")
            {
                archive = new Archive(Vessel);

                Volumes.Add(archive);

                
            }
        }

        public void Boot()
        {
            Mode = Modes.READY;

            Push(new InterpreterBootup(this));

            if (Volumes.Count > 1) 
                SelectedVolume = Volumes[1];
            else
                SelectedVolume = Volumes[0];
        }

        public bool IsAlive()
        {
            var partState = ((kOSProcessor)this.Parent).part.State;

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

            var electricReq = 0.05f * time;
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
                else
                {
                    throw new kOSException("Volume disconnected - out of range");
                }
            }

            return false;
        }

        public override bool SwitchToVolume(string targetVolume)
        {
            foreach (Volume volume in Volumes)
            {
                if (volume.Name.ToUpper() == targetVolume.ToUpper())
                {
                    if (volume.CheckRange())
                    {
                        SelectedVolume = volume;
                        return true;
                    }
                    else
                    {
                        throw new kOSException("Volume disconnected - out of range");
                    }
                }
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
            else
            {
                throw new kOSException("Cannot bind " + varName + "; name already taken.");
            }
        }

        public override void Update(float time)
        {
            bindingManager.Update(time);

            SessionTime += time;

            base.Update(time);

            if (Mode == Modes.STARVED)
            {
                ChildContext = null;
            }
            else if (Mode == Modes.OFF)
            {
                ChildContext = null;
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
    }
}
