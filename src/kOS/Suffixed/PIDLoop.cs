using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System;

namespace kOS.Suffixed
{
    public class PIDLoop : Structure
    {
        public static PIDLoop DeepCopy(PIDLoop source)
        {
            PIDLoop newLoop = new PIDLoop()
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

        public double ErrorSum { get; set; }

        public double PTerm { get; set; }

        public double ITerm { get; set; }

        public double DTerm { get; set; }

        public bool ExtraUnwind { get; set; }

        public double ChangeRate { get; set; }

        private bool unWinding = false;

        public PIDLoop()
            : this(1, 0, 0)
        {
        }

        public PIDLoop(double kp, double ki, double kd, double maxoutput = double.MaxValue, bool extraUnwind = false)
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
            ErrorSum = 0;
            PTerm = 0;
            ITerm = 0;
            DTerm = 0;
            ExtraUnwind = extraUnwind;
            InitializeSuffixes();
        }

        public void InitializeSuffixes()
        {
            AddSuffix("LASTSAMPLETIME", new Suffix<double>(() => LastSampleTime));
            AddSuffix("KP", new SetSuffix<double>(() => Kp, value => Kp = value));
            AddSuffix("KI", new SetSuffix<double>(() => Ki, value => Ki = value));
            AddSuffix("KD", new SetSuffix<double>(() => Kd, value => Kd = value));
            AddSuffix("INPUT", new Suffix<double>(() => Input));
            AddSuffix("SETPOINT", new SetSuffix<double>(() => Setpoint, value => Setpoint = value));
            AddSuffix("ERROR", new Suffix<double>(() => Error));
            AddSuffix("OUTPUT", new Suffix<double>(() => Output));
            AddSuffix("MAXOUTPUT", new SetSuffix<double>(() => MaxOutput, value => MaxOutput = value));
            AddSuffix("ERRORSUM", new Suffix<double>(() => ErrorSum));
            AddSuffix("PTERM", new Suffix<double>(() => PTerm));
            AddSuffix("ITERM", new Suffix<double>(() => ITerm));
            AddSuffix("DTERM", new Suffix<double>(() => DTerm));
            AddSuffix("EXTRAUNWIND", new SetSuffix<bool>(() => ExtraUnwind, value => ExtraUnwind = value));
            AddSuffix("CHANGERATE", new Suffix<double>(() => ChangeRate));
            AddSuffix("RESET", new NoArgsSuffix(ResetI));
            AddSuffix("UPDATE", new TwoArgsSuffix<double, double, double>(Update));
        }

        public double Update(double sampleTime, double input, double setpoint, double maxOutput)
        {
            MaxOutput = maxOutput;
            Setpoint = setpoint;
            return Update(sampleTime, input);
        }

        public double Update(double sampleTime, double input)
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
                        dTerm = ChangeRate * Kd;
                    }
                }
            }
            Output = pTerm + iTerm + dTerm;
            if (Math.Abs(Output) > MaxOutput)
            {
                Output = Math.Sign(Output) * MaxOutput;
                if (Ki != 0 && LastSampleTime < sampleTime)
                {
                    iTerm = Output - Math.Max(Math.Min(pTerm + dTerm, MaxOutput), -MaxOutput);
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