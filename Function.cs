using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace kOS
{
    public class FunctionAttribute : Attribute
    {
        public string functionName { get; set; }
        public FunctionAttribute(string functionName)
        {
            this.functionName = functionName;
        }
    }

    public class Function
    {
        public virtual void Execute(SharedObjects shared)
        {
        }

        protected double GetDouble(object argument)
        {
            if (argument is int)
                return (double)(int)argument;
            else if (argument is double)
                return (double)argument;
            else
                throw new ArgumentException(string.Format("Can't cast {0} to double.", argument));
        }

        protected int GetInt(object argument)
        {
            if (argument is int)
                return (int)argument;
            else if (argument is double)
                return (int)(double)argument;
            else
                throw new ArgumentException(string.Format("Can't cast {0} to int.", argument));
        }

        protected double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        protected double RadiansToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }
    }

    #region Math

    [FunctionAttribute("abs")]
    public class FunctionAbs : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Abs(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("mod")]
    public class FunctionMod : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double dividend = GetDouble(shared.Cpu.PopValue());
            double divisor = GetDouble(shared.Cpu.PopValue());
            double result = dividend % divisor;
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("floor")]
    public class FunctionFloor : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Floor(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("ceiling")]
    public class FunctionCeiling : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Ceiling(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("roundnearest")]
    public class FunctionRoundNearest : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Round(argument);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("round")]
    public class FunctionRound : Function
    {
        public override void Execute(SharedObjects shared)
        {
            int decimals = GetInt(shared.Cpu.PopValue());
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Round(argument, decimals);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("sqrt")]
    public class FunctionSqrt : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = Math.Sqrt(argument);
            shared.Cpu.PushStack(result);
        }
    }

    #endregion

    #region Trig

    [FunctionAttribute("sin")]
    public class FunctionSin : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double degrees = GetDouble(shared.Cpu.PopValue());
            double radians = DegreesToRadians(degrees);
            double result = Math.Sin(radians);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("cos")]
    public class FunctionCos : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double degrees = GetDouble(shared.Cpu.PopValue());
            double radians = DegreesToRadians(degrees);
            double result = Math.Cos(radians);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("tan")]
    public class FunctionTan : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double degrees = GetDouble(shared.Cpu.PopValue());
            double radians = DegreesToRadians(degrees);
            double result = Math.Tan(radians);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("arcsin")]
    public class FunctionArcSin : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = RadiansToDegrees(Math.Asin(argument));
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("arccos")]
    public class FunctionArcCos : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = RadiansToDegrees(Math.Acos(argument));
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("arctan")]
    public class FunctionArcTan : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double argument = GetDouble(shared.Cpu.PopValue());
            double result = RadiansToDegrees(Math.Atan(argument));
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("arctan2")]
    public class FunctionArcTan2 : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double x = GetDouble(shared.Cpu.PopValue());
            double y = GetDouble(shared.Cpu.PopValue());
            double result = RadiansToDegrees(Math.Atan2(y, x));
            shared.Cpu.PushStack(result);
        }
    }

    #endregion

    #region Special

    [FunctionAttribute("node")]
    public class FunctionNode : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double prograde = GetDouble(shared.Cpu.PopValue());
            double normal = GetDouble(shared.Cpu.PopValue());
            double radial = GetDouble(shared.Cpu.PopValue());
            double time = GetDouble(shared.Cpu.PopValue());

            Node result = new Node(time, radial, normal, prograde);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("v")]
    public class FunctionVector : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double z = GetDouble(shared.Cpu.PopValue());
            double y = GetDouble(shared.Cpu.PopValue());
            double x = GetDouble(shared.Cpu.PopValue());

            Vector result = new Vector(x, y, z);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("r")]
    public class FunctionRotation : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double roll = GetDouble(shared.Cpu.PopValue());
            double yaw = GetDouble(shared.Cpu.PopValue());
            double pitch = GetDouble(shared.Cpu.PopValue());

            Direction result = new Direction(new Vector3d(pitch, yaw, roll), true);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("q")]
    public class FunctionQuaternion : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double angle = GetDouble(shared.Cpu.PopValue());
            double roll = GetDouble(shared.Cpu.PopValue());
            double yaw = GetDouble(shared.Cpu.PopValue());
            double pitch = GetDouble(shared.Cpu.PopValue());

            Direction result = new Direction(new UnityEngine.Quaternion((float)pitch, (float)yaw, (float)roll, (float)angle));
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("latlng")]
    public class FunctionLatLng : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double longitude = GetDouble(shared.Cpu.PopValue());
            double latitude = GetDouble(shared.Cpu.PopValue());

            GeoCoordinates result = new GeoCoordinates(shared.Vessel, latitude, longitude);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("vessel")]
    public class FunctionVessel : Function
    {
        public override void Execute(SharedObjects shared)
        {
            string vesselName = shared.Cpu.PopValue().ToString();            
            VesselTarget result = new VesselTarget(VesselUtils.GetVesselByName(vesselName, shared.Vessel), null);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("body")]
    public class FunctionBody : Function
    {
        public override void Execute(SharedObjects shared)
        {
            string bodyName = shared.Cpu.PopValue().ToString();
            BodyTarget result = new BodyTarget(bodyName, shared.Vessel);
            shared.Cpu.PushStack(result);
        }
    }

    [FunctionAttribute("heading")]
    public class FunctionHeading : Function
    {
        public override void Execute(SharedObjects shared)
        {
            double pitchAboveHorizon = GetDouble(shared.Cpu.PopValue());
            double degreesFromNorth = GetDouble(shared.Cpu.PopValue());

            Vessel currentVessel = shared.Vessel;
            var q = UnityEngine.Quaternion.LookRotation(VesselUtils.GetNorthVector(currentVessel), currentVessel.upAxis);
            q *= UnityEngine.Quaternion.Euler(new UnityEngine.Vector3((float)-pitchAboveHorizon, (float)degreesFromNorth, 0));

            Direction result = new Direction(q);
            shared.Cpu.PushStack(result);
        }
    }

    #endregion

    #region Misc

    [FunctionAttribute("clearscreen")]
    public class FunctionClearScreen : Function
    {
        public override void Execute(SharedObjects shared)
        {
            shared.Screen.ClearScreen();
        }
    }

    [FunctionAttribute("print")]
    public class FunctionPrint : Function
    {
        public override void Execute(SharedObjects shared)
        {
            string textToPrint = shared.Cpu.PopValue().ToString();
            shared.Screen.Print(textToPrint);
        }
    }

    [FunctionAttribute("printat")]
    public class FunctionPrintAt : Function
    {
        public override void Execute(SharedObjects shared)
        {
            int row = Convert.ToInt32(shared.Cpu.PopValue());
            int column = Convert.ToInt32(shared.Cpu.PopValue());
            string textToPrint = shared.Cpu.PopValue().ToString();
            shared.Screen.PrintAt(textToPrint, row, column);
        }
    }

    [FunctionAttribute("toggleflybywire")]
    public class FunctionToggleFlyByWire : Function
    {
        public override void Execute(SharedObjects shared)
        {
            bool enabled = Convert.ToBoolean(shared.Cpu.PopValue());
            string paramName = shared.Cpu.PopValue().ToString();
            shared.Cpu.ToggleFlyByWire(paramName, enabled);
        }
    }

    [FunctionAttribute("stage")]
    public class FunctionStage : Function
    {
        public override void Execute(SharedObjects shared)
        {
            Staging.ActivateNextStage();
        }
    }

    [FunctionAttribute("run")]
    public class FunctionRun : Function
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = shared.Cpu.PopValue().ToString();

            if (shared.VolumeMgr != null)
            {
                if (shared.VolumeMgr.CurrentVolume != null)
                {
                    ProgramFile file = shared.VolumeMgr.CurrentVolume.GetByName(fileName);
                    if (file != null)
                    {
                        if (shared.ScriptHandler != null)
                        {
                            Stopwatch compileWatch = null;
                            bool showStatistics = Config.GetInstance().ShowStatistics;
                            if (showStatistics) compileWatch = Stopwatch.StartNew();

                            List<CodePart> parts = shared.ScriptHandler.Compile(file.Content);
                            ProgramBuilder builder = new ProgramBuilder();
                            builder.AddRange(parts);
                            List<Opcode> program = builder.BuildProgram(false);

                            if (showStatistics)
                            {
                                compileWatch.Stop();
                                shared.Cpu.TotalCompileTime += compileWatch.ElapsedMilliseconds;
                            }
                            
                            shared.Cpu.RunProgram(program);
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("File '{0}' not found", fileName));
                    }
                }
                else
                {
                    throw new Exception("Volume not found");
                }
            }
        }
    }

    [FunctionAttribute("add")]
    public class FunctionAddNode : Function
    {
        public override void Execute(SharedObjects shared)
        {
            Node node = (Node)shared.Cpu.PopValue();
            node.AddToVessel(shared.Vessel);
        }
    }

    [FunctionAttribute("remove")]
    public class FunctionRemoveNode : Function
    {
        public override void Execute(SharedObjects shared)
        {
            Node node = (Node)shared.Cpu.PopValue();
            node.Remove();
        }
    }

    [FunctionAttribute("log")]
    public class FunctionLog : Function
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = shared.Cpu.PopValue().ToString();
            string expressionResult = shared.Cpu.PopValue().ToString();

            if (shared.VolumeMgr != null)
            {
                Volume volume = shared.VolumeMgr.CurrentVolume;
                if (volume != null)
                {
                    volume.AppendToFile(fileName, expressionResult);
                }
                else
                {
                    throw new Exception("Volume not found");
                }
            }
        }
    }

    [FunctionAttribute("reboot")]
    public class FunctionReboot : Function
    {
        public override void Execute(SharedObjects shared)
        {
            if (shared.Cpu != null) shared.Cpu.Boot();
        }
    }

    [FunctionAttribute("shutdown")]
    public class FunctionShutdown : Function
    {
        public override void Execute(SharedObjects shared)
        {
            if (shared.Processor != null) shared.Processor.SetMode(kOSProcessor.Modes.OFF);
        }
    }

    #endregion

    #region FileSystem

    [FunctionAttribute("switch")]
    public class FunctionSwitch : Function
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue();

            if (shared.VolumeMgr != null)
            {
                Volume volume = shared.VolumeMgr.GetVolume(volumeId);
                if (volume != null)
                {
                    shared.VolumeMgr.SwitchTo(volume);
                }
                else
                {
                    throw new Exception("Volume not found");
                }
            }
        }
    }

    [FunctionAttribute("copy")]
    public class FunctionCopy : Function
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue();
            string direction = shared.Cpu.PopValue().ToString();
            string fileName = shared.Cpu.PopValue().ToString();

            if (shared.VolumeMgr != null)
            {
                Volume origin;
                Volume destination;

                if (direction == "from")
                {
                    origin = shared.VolumeMgr.GetVolume(volumeId);
                    destination = shared.VolumeMgr.CurrentVolume;
                }
                else
                {
                    origin = shared.VolumeMgr.CurrentVolume;
                    destination = shared.VolumeMgr.GetVolume(volumeId);
                }

                if (origin != null && destination != null)
                {
                    if (origin != destination)
                    {
                        ProgramFile file = origin.GetByName(fileName);
                        if (file != null)
                        {
                            if (!destination.SaveFile(new ProgramFile(file)))
                            {
                                throw new Exception("File copy failed");
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("File '{0}' not found", fileName));
                        }
                    }
                }
                else
                {
                    throw new Exception("Volume not found");
                } 
            }
        }
    }

    [FunctionAttribute("rename")]
    public class FunctionRename : Function
    {
        public override void Execute(SharedObjects shared)
        {
            string newName = shared.Cpu.PopValue().ToString();
            object oldName = shared.Cpu.PopValue();
            string objectToRename = shared.Cpu.PopValue().ToString();

            if (shared.VolumeMgr != null)
            {
                if (objectToRename == "file")
                {
                    Volume volume = shared.VolumeMgr.CurrentVolume;
                    if (volume != null)
                    {
                        if (volume.GetByName(newName) == null)
                        {
                            if (!volume.RenameFile(oldName.ToString(), newName))
                            {
                                throw new Exception(string.Format("File '{0}' not found", oldName.ToString()));
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("File '{0}' already exists.", newName));
                        } 
                    }
                    else
                    {
                        throw new Exception("Volume not found");
                    }
                }
                else
                {
                    Volume volume = shared.VolumeMgr.GetVolume(oldName);
                    if (volume != null)
                    {
                        if (volume.Renameable)
                        {
                            volume.Name = newName;
                        }
                        else
                        {
                            throw new Exception("Volume cannot be renamed");
                        }
                    }
                    else
                    {
                        throw new Exception("Volume not found");
                    }
                }
            }
        }
    }

    [FunctionAttribute("delete")]
    public class FunctionDelete : Function
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue();
            string fileName = shared.Cpu.PopValue().ToString();

            if (shared.VolumeMgr != null)
            {
                Volume volume;
                
                if (volumeId != null)
                {
                    volume = shared.VolumeMgr.GetVolume(volumeId);
                }
                else
                {
                    volume = shared.VolumeMgr.CurrentVolume;
                }

                if (volume != null)
                {
                    if (!volume.DeleteByName(fileName))
                    {
                        throw new Exception(string.Format("File '{0}' not found", fileName));
                    }
                }
                else
                {
                    throw new Exception("Volume not found");
                }
            }
        }
    }

    #endregion
}
