using System;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Safe.Encapsulation
{
    public class PIDLoop : Structure
    {
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

        public double MaxOutput { get; set; }

        public double MinOutput { get; set; }

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

        public PIDLoop(double kp, double ki, double kd, double maxoutput = double.MaxValue, double minoutput = double.MinValue, bool extraUnwind = false)
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
            ErrorSum = 0;
            PTerm = 0;
            ITerm = 0;
            DTerm = 0;
            ExtraUnwind = extraUnwind;
            InitializeSuffixes();
        }

        public void InitializeSuffixes()
        {
            AddSuffix("LASTSAMPLETIME", new Suffix<ScalarDoubleValue>(() => LastSampleTime));
            AddSuffix("KP", new SetSuffix<ScalarDoubleValue>(() => Kp, value => Kp = value));
            AddSuffix("KI", new SetSuffix<ScalarDoubleValue>(() => Ki, value => Ki = value));
            AddSuffix("KD", new SetSuffix<ScalarDoubleValue>(() => Kd, value => Kd = value));
            AddSuffix("INPUT", new Suffix<ScalarDoubleValue>(() => Input));
            AddSuffix("SETPOINT", new SetSuffix<ScalarDoubleValue>(() => Setpoint, value => Setpoint = value));
            AddSuffix("ERROR", new Suffix<ScalarDoubleValue>(() => Error));
            AddSuffix("OUTPUT", new Suffix<ScalarDoubleValue>(() => Output));
            AddSuffix("MAXOUTPUT", new SetSuffix<ScalarDoubleValue>(() => MaxOutput, value => MaxOutput = value));
            AddSuffix("MINOUTPUT", new SetSuffix<ScalarDoubleValue>(() => MinOutput, value => MinOutput = value));
            AddSuffix("ERRORSUM", new Suffix<ScalarDoubleValue>(() => ErrorSum));
            AddSuffix("PTERM", new Suffix<ScalarDoubleValue>(() => PTerm));
            AddSuffix("ITERM", new Suffix<ScalarDoubleValue>(() => ITerm));
            AddSuffix("DTERM", new Suffix<ScalarDoubleValue>(() => DTerm));
            AddSuffix("CHANGERATE", new Suffix<ScalarDoubleValue>(() => ChangeRate));
            AddSuffix("RESET", new NoArgsSuffix(ResetI));
            AddSuffix("UPDATE", new TwoArgsSuffix<ScalarDoubleValue, ScalarDoubleValue, ScalarDoubleValue>(Update));
        }

        public double Update(double sampleTime, double input, double setpoint, double minOutput, double maxOutput)
        {
            MaxOutput = maxOutput;
            MinOutput = minOutput;
            Setpoint = setpoint;
            return Update(sampleTime, input);
        }

        public double Update(double sampleTime, double input, double setpoint, double maxOutput)
        {
            return Update(sampleTime, input, setpoint, -maxOutput, maxOutput);
        }

        public ScalarDoubleValue Update(ScalarDoubleValue sampleTime, ScalarDoubleValue input)
        {
            double error = Setpoint - input;
            double pTerm = error * Kp;
            double iTerm = 0;
            double dTerm = 0;
            if (LastSampleTime < sampleTime)
            {
                double dt = sampleTime - LastSampleTime;
                if (dt < 1)
                {
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
            LastSampleTime = double.MaxValue;
        }

        public override string ToString()
        {
            return string.Format("PIDLoop(Kp:{0}, Ki:{1}, Kd:{2}, Setpoint:{3}, Error:{4}, Output:{5})",
                Kp, Ki, Kd, Setpoint, Error, Output);
        }

        public string ToCSVString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7}",
                LastSampleTime, Error, ErrorSum, Output, Kp, Ki, Kd, MaxOutput);
        }

        public string ConstrutorString()
        {
            return string.Format("pidloop({0}, {1}, {2}, {3}, {4})", Ki, Kp, Kd, MaxOutput, ExtraUnwind);
        }
    }
}