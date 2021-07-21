using System;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Function;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;

namespace kOS.Safe.Encapsulation
{
    [kOS.Safe.Utilities.KOSNomenclature("PIDLoop")]
    public class PIDLoop : Structure
    {
        [Function("pidloop")]
        public class PIDLoopConstructor : SafeFunctionBase
        {
            public override void Execute(SafeSharedObjects shared)
            {
                int args = CountRemainingArgs(shared);
                double kd;
                double ki;
                double kp;
                double minoutput;
                double maxoutput;
                double epsilon;
                switch (args)
                {
                    case 0:
                        this.ReturnValue = new PIDLoop();
                        break;
                    case 1:
                        kp = GetDouble(PopValueAssert(shared));
                        this.ReturnValue = new PIDLoop(kp, 0, 0);
                        break;
                    case 3:
                        kd = GetDouble(PopValueAssert(shared));
                        ki = GetDouble(PopValueAssert(shared));
                        kp = GetDouble(PopValueAssert(shared));
                        this.ReturnValue = new PIDLoop(kp, ki, kd);
                        break;
                    case 5:
                        maxoutput = GetDouble(PopValueAssert(shared));
                        minoutput = GetDouble(PopValueAssert(shared));
                        kd = GetDouble(PopValueAssert(shared));
                        ki = GetDouble(PopValueAssert(shared));
                        kp = GetDouble(PopValueAssert(shared));
                        this.ReturnValue = new PIDLoop(kp, ki, kd, maxoutput, minoutput);
                        break;
                    case 6:
                        epsilon = GetDouble(PopValueAssert(shared));
                        maxoutput = GetDouble(PopValueAssert(shared));
                        minoutput = GetDouble(PopValueAssert(shared));
                        kd = GetDouble(PopValueAssert(shared));
                        ki = GetDouble(PopValueAssert(shared));
                        kp = GetDouble(PopValueAssert(shared));
                        this.ReturnValue = new PIDLoop(kp, ki, kd, maxoutput, minoutput, epsilon);
                        break;
                    default:
                        throw new KOSArgumentMismatchException(new[] { 0, 1, 3, 5 }, args);
                }
                AssertArgBottomAndConsume(shared);
            }
        }

        public static PIDLoop DeepCopy(PIDLoop source)
        {
            PIDLoop newLoop = new PIDLoop
            {
                LastSampleTime = source.LastSampleTime,
                Kp = source.Kp,
                Ki = source.Ki,
                Kd = source.Kd,
                Input = source.Input,
                Setpoint = source.Setpoint,
                Error = source.Error,
                Output = source.Output,
                MinOutput = source.MinOutput,
                MaxOutput = source.MaxOutput,
                ErrorSum = source.ErrorSum,
                PTerm = source.PTerm,
                ITerm = source.ITerm,
                DTerm = source.DTerm,
                ExtraUnwind = source.ExtraUnwind,
                ChangeRate = source.ChangeRate,
                unWinding = source.unWinding
            };
            return newLoop;
        }

        public double LastSampleTime { get; set; }

        public double Kp { get; set; }

        public double Ki { get; set; }

        public double Kd { get; set; }

        public double Input { get; set; }

        public double Setpoint { get; set; }

        public double Error { get; set; }

        public double Output { get; set; }

        public double MinOutput { get; set; }

        public double MaxOutput { get; set; }

        public double Epsilon { get; set; }

        public double ErrorSum { get; set; }

        public double PTerm { get; set; }

        public double ITerm { get; set; }

        public double DTerm { get; set; }

        public bool ExtraUnwind { get; set; }

        public double ChangeRate { get; set; }

        private bool unWinding;

        public PIDLoop()
            : this(1, 0, 0)
        {
        }

        public PIDLoop(double kp, double ki, double kd, double maxoutput = double.MaxValue, double minoutput = double.MinValue, double nullzone = 0, bool extraUnwind = false)
        {
            LastSampleTime = double.MaxValue;
            Kp = kp;
            Ki = ki;
            Kd = kd;
            Input = 0;
            Setpoint = 0;
            Error = 0;
            Output = 0;
            MaxOutput = maxoutput;
            MinOutput = minoutput;
            Epsilon = nullzone;
            ErrorSum = 0;
            PTerm = 0;
            ITerm = 0;
            DTerm = 0;
            ExtraUnwind = extraUnwind;
            InitializeSuffixes();
        }

        public void InitializeSuffixes()
        {
            AddSuffix("LASTSAMPLETIME", new Suffix<ScalarValue>(() => LastSampleTime));
            AddSuffix("KP", new SetSuffix<ScalarValue>(() => Kp, value => Kp = value));
            AddSuffix("KI", new SetSuffix<ScalarValue>(() => Ki, value => Ki = value));
            AddSuffix("KD", new SetSuffix<ScalarValue>(() => Kd, value => Kd = value));
            AddSuffix("INPUT", new Suffix<ScalarValue>(() => Input));
            AddSuffix("SETPOINT", new SetSuffix<ScalarValue>(() => Setpoint, value => Setpoint = value));
            AddSuffix("ERROR", new Suffix<ScalarValue>(() => Error));
            AddSuffix("OUTPUT", new Suffix<ScalarValue>(() => Output));
            AddSuffix("MAXOUTPUT", new SetSuffix<ScalarValue>(() => MaxOutput, value => MaxOutput = value));
            AddSuffix("MINOUTPUT", new SetSuffix<ScalarValue>(() => MinOutput, value => MinOutput = value));
            AddSuffix(new string[] { "IGNOREERROR", "EPSILON" }, new SetSuffix<ScalarValue>(() => Epsilon, value => Epsilon = value));
            AddSuffix("ERRORSUM", new Suffix<ScalarValue>(() => ErrorSum));
            AddSuffix("PTERM", new Suffix<ScalarValue>(() => PTerm));
            AddSuffix("ITERM", new Suffix<ScalarValue>(() => ITerm));
            AddSuffix("DTERM", new Suffix<ScalarValue>(() => DTerm));
            AddSuffix("CHANGERATE", new Suffix<ScalarValue>(() => ChangeRate));
            AddSuffix("RESET", new NoArgsVoidSuffix(ResetI));
            AddSuffix("UPDATE", new TwoArgsSuffix<ScalarValue, ScalarValue, ScalarValue>(Update));
        }

        public double Update(double sampleTime, double input, double setpoint, double minOutput, double maxOutput, double epsilon)
        {
            MaxOutput = maxOutput;
            MinOutput = minOutput;
            Epsilon = epsilon;
            Setpoint = setpoint;
            return Update(sampleTime, input);
        }

        public double Update(double sampleTime, double input, double setpoint, double maxOutput, double epsilon)
        {
            return Update(sampleTime, input, setpoint, -maxOutput, maxOutput, epsilon);
        }

        public ScalarValue Update(ScalarValue sampleTime, ScalarValue input)
        {
            double error = Setpoint - input;
            if (error > -Epsilon && error < Epsilon)
            {
                // Pretend there is no error (get everything to zero out)
                // because the error is within the epsilon:
                error = 0;
                input = Setpoint;
                Input = input;
                Error = error;
            }
            double pTerm = error * Kp;
            double iTerm = 0;
            double dTerm = 0;
            if (LastSampleTime < sampleTime)
            {
                double dt = sampleTime - LastSampleTime;
                if (Ki != 0)
                {
                    if (ExtraUnwind)
                    {
                        if (Math.Sign(error) != Math.Sign(ErrorSum))
                        {
                            if (!unWinding)
                            {
                                Ki *= 2;
                                unWinding = true;
                            }
                        }
                        else if (unWinding)
                        {
                            Ki /= 2;
                            unWinding = false;
                        }
                    }
                    iTerm = ITerm + error * dt * Ki;
                }
                ChangeRate = (input - Input) / dt;
                if (Kd != 0)
                {
                    dTerm = -ChangeRate * Kd;
                }
            }
            else
            {
                dTerm = DTerm;
                iTerm = ITerm;
            }
            Output = pTerm + iTerm + dTerm;
            if (Output > MaxOutput)
            {
                Output = MaxOutput;
                if (Ki != 0 && LastSampleTime < sampleTime)
                {
                    iTerm = Output - Math.Min(pTerm + dTerm, MaxOutput);
                }
            }
            if (Output < MinOutput)
            {
                Output = MinOutput;
                if (Ki != 0 && LastSampleTime < sampleTime)
                {
                    iTerm = Output - Math.Max(pTerm + dTerm, MinOutput);
                }
            }
            LastSampleTime = sampleTime;
            Input = input;
            Error = error;
            PTerm = pTerm;
            ITerm = iTerm;
            DTerm = dTerm;
            if (Ki != 0) ErrorSum = iTerm / Ki;
            else ErrorSum = 0;
            return Output;
        }

        public void ResetI()
        {
            ErrorSum = 0;
            ITerm = 0;
            LastSampleTime = double.MaxValue;
        }

        public override string ToString()
        {
            return string.Format("PIDLoop(Kp:{0}, Ki:{1}, Kd:{2}, Min:{3}, Max:{4}, Setpoint:{5}, Error:{6}, Output:{7})",
                Kp, Ki, Kd, MinOutput, MaxOutput, Setpoint, Error, Output);
        }

        public string ToCSVString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                LastSampleTime, Error, ErrorSum, Output, Kp, Ki, Kd, MinOutput, MaxOutput);
        }

        public string ConstrutorString()
        {
            return string.Format("pidloop({0}, {1}, {2}, {3}, {4})", Ki, Kp, Kd, MaxOutput, ExtraUnwind);
        }

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static PIDLoop CreateFromDump(SafeSharedObjects shared, Dump d)
        {
            var newObj = new PIDLoop();
            newObj.LoadDump(d);
            return newObj;
        }

        public override Dump Dump()
        {
            var result = new DumpWithHeader { Header = "PIDLoop" };
            result.Add("Kp", Kp);
            result.Add("Ki", Ki);
            result.Add("Kd", Kd);
            result.Add("Setpoint", Setpoint);
            result.Add("MaxOutput", MaxOutput);
            result.Add("MinOutput", MinOutput);
            result.Add("ExtraUnwind", ExtraUnwind);

            return result;
    }

    public override void LoadDump(Dump dump)
        {
            Kp = Convert.ToDouble(dump["Kp"]);
            Ki = Convert.ToDouble(dump["Ki"]);
            Kd = Convert.ToDouble(dump["Kd"]);
            Setpoint = Convert.ToDouble(dump["Setpoint"]);
            MinOutput = Convert.ToDouble(dump["MinOutput"]);
            MaxOutput = Convert.ToDouble(dump["MaxOutput"]);
            ExtraUnwind = Convert.ToBoolean(dump["ExtraUnwind"]);
        }
    }
}