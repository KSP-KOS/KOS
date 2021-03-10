using System;
using kOS.Safe.Encapsulation;
using NUnit.Framework;

namespace kOS.Safe.Test.Structure
{
    [TestFixture]
    public class PIDLoopTest
    {

        private double[] input;

        [SetUp]
        public void Setup()
        {
            input = new double[] {-10, -10, -10, -10, 6, 6, 6, 6, 6, 6};
        }
         
        [Test]
        public void CanCorrectlyReactToSineWaveInput()
        {
            var pidLoop = new PIDLoop(1, 1, 1);
            double[] sineWaveInput = {0, -4.207, -4.547, -0.706, 3.784, 4.795, 1.397, -3.285, -4.947, -2.060};
            double[] output = new double[10];
            double[] pTermOutput = new double[10];
            double[] iTermOutput = new double[10];
            double[] dTermOutput = new double[10];

            for (var i = 0; i < sineWaveInput.Length; i++)
            {
                output[i] = Math.Round((double) pidLoop.Update(i, sineWaveInput[i]), 2);
                pTermOutput[i] = Math.Round(pidLoop.PTerm, 2);
                iTermOutput[i] = Math.Round(pidLoop.ITerm, 2);
                dTermOutput[i] = Math.Round(pidLoop.DTerm, 2);
            }

            Assert.AreEqual(new double[] {0, 4.21, 4.55, 0.71, -3.78, -4.80, -1.40, 3.28, 4.95, 2.06}, pTermOutput, "Proportional part is incorrect");
            Assert.AreEqual(new double[] {0, 0, 4.21, 8.75, 9.46, 5.68, 0.88, -0.52, 2.77, 7.72}, iTermOutput, "Integral part is incorrect");
            Assert.AreEqual(new double[] {0, 4.21, 0.34, -3.84, -4.49, -1.01, 3.40, 4.68, 1.66, -2.89}, dTermOutput, "Derivative part is incorrect");
            Assert.AreEqual(new double[] {0, 8.41, 9.09, 5.62, 1.19, -0.13, 2.88, 7.45, 9.38, 6.89}, output, "PID output is incorrect");
        }

        [Test]
        public void CanAntiWindUp()
        {
            var pidLoop = new PIDLoop(0, 1, 0, 11, 0);
            double[] output = new double[10];

            for (var i = 0; i < input.Length; i++)
            {
                output[i] = pidLoop.Update(i, input[i]);
            }

            Assert.AreEqual(new double[] {0, 10, 11, 11, 11, 5, 0, 0, 0, 0}, output);
        }

        [Test]
        public void CanNoneAntiWindUp()
        {
            var pidLoop = new PIDLoop(0, 1, 0, 11, 0);
            pidLoop.AntiWindupMode = "NONE";
            double[] outputs = new double[10];

            for (var i = 0; i < input.Length; i++)
            {
                outputs[i] = pidLoop.Update(i, input[i]);
            }

            Assert.AreEqual(new double[] {0, 10, 11, 11, 11, 11, 11, 11, 11, 10}, outputs);
        }

        [Test]
        public void CanClampingAntiWindUp()
        {
            var pidLoop = new PIDLoop(0, 1, 0, 11, 0);
            pidLoop.AntiWindupMode = "CLAMPING";
            double[] outputs = new double[10];

            for (var i = 0; i < input.Length; i++)
            {
                outputs[i] = pidLoop.Update(i, input[i]);
            }

            Assert.AreEqual(new double[] {0, 10, 11, 11, 11, 11, 8, 2, 0, 0}, outputs);
        }

        [Test]
        public void CanBackCalculationAntiWindUp()
        {
            var pidLoop = new PIDLoop(0, 1, 0, 11, 0);
            pidLoop.AntiWindupMode = "BACK-CALC";
            double[] outputs = new double[10];

            for (var i = 0; i < input.Length; i++)
            {
                outputs[i] = pidLoop.Update(i, input[i]);
            }

            Assert.AreEqual(new double[] {0, 10, 11, 11, 11, 5, 0, 0, 0, 0}, outputs);
        }
        
        [Test]
        public void CanBackCalculationWithK2AntiWindUp()
        {
            var pidLoop = new PIDLoop(0, 1, 0, 11, 0);
            pidLoop.AntiWindupMode = "BACK-CALC";
            pidLoop.KBackCalc = 2;
            double[] outputs = new double[10];

            for (var i = 0; i < input.Length; i++)
            {
                outputs[i] = pidLoop.Update(i, input[i]);
            }

            Assert.AreEqual(new double[] {0, 10, 11, 11, 11, 0, 0, 0, 0, 0}, outputs);
        }
    }
}
