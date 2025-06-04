using System;
using System.Collections.Generic;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace SAT_TestProgram.Data
{
    public class SignalProcessor
    {
        #region Gate Class
        public class Gate
        {
            public int StartIndex { get; private set; }
            public int EndIndex { get; private set; }

            public Gate(int start, int end)
            {
                StartIndex = start;
                EndIndex = end;
            }
        }
        #endregion

        #region Gate Calculation
        public List<(float maxValue, int maxIndex)> GetMaxValuesWithIndex(float[] data, List<Gate> gates, int offset)
        {
            var maxValues = new List<(float maxValue, int maxIndex)>();

            foreach (var gate in gates)
            {
                float max = float.MinValue;
                int maxIndex = -1;

                for (int i = gate.StartIndex + offset; i <= gate.EndIndex + offset && i < data.Length; i++)
                {
                    if (i < 0) continue;

                    if (data[i] > max)
                    {
                        max = data[i];
                        maxIndex = i;
                    }
                }

                maxValues.Add((max, maxIndex));
            }

            return maxValues;
        }
        #endregion

        #region Surface Match
        public (float maxValue, int maxIndex) GetFirstMax(float[] data, double ratio = 0.4)
        {
            int length = data.Length;
            int endIndex = (int)(length * ratio);
            if (endIndex > length) endIndex = length;

            float maxValue = float.MinValue;
            int maxIndex = -1;

            for (int i = 0; i < endIndex; i++)
            {
                if (data[i] > maxValue)
                {
                    maxValue = data[i];
                    maxIndex = i;
                }
            }

            return (maxValue, maxIndex);
        }
        #endregion

        #region Filter Methods
        public float[] FDomainFilter(float[] inputSignal, double middleCutOffRatio = 0.3, double sideCutoffRatio = 0.03)
        {
            int n = inputSignal.Length;
            Complex[] complexSignal = new Complex[n];
            for (int i = 0; i < n; i++) complexSignal[i] = new Complex(inputSignal[i], 0);

            Fourier.Forward(complexSignal, FourierOptions.Matlab);

            int middleCutOffIndex = (int)(n * middleCutOffRatio / 2);
            int sideCutOffIndex = (int)(n * sideCutoffRatio / 2);

            for (int i = 0; i < n; i++)
            {
                if (!(sideCutOffIndex < i && i < middleCutOffIndex) &&
                    !(n - middleCutOffIndex < i && i < n - sideCutOffIndex))
                {
                    complexSignal[i] = Complex.Zero;
                }
            }

            Fourier.Inverse(complexSignal, FourierOptions.Matlab);

            float[] outputSignal = new float[n];
            for (int i = 0; i < n; i++) outputSignal[i] = (float)complexSignal[i].Real;

            return outputSignal;
        }

        public float[] ExtractEnvelope(float[] inputSignal)
        {
            int n = inputSignal.Length;
            Complex[] analyticSignal = new Complex[n];
            for (int i = 0; i < n; i++) analyticSignal[i] = new Complex(inputSignal[i], 0);

            Fourier.Forward(analyticSignal, FourierOptions.Matlab);
            for (int i = 1; i < n / 2; i++) analyticSignal[i] *= 2.0;
            for (int i = n / 2 + 1; i < n; i++) analyticSignal[i] = Complex.Zero;

            Fourier.Inverse(analyticSignal, FourierOptions.Matlab);

            float[] envelope = new float[n];
            for (int i = 0; i < n; i++) envelope[i] = (float)analyticSignal[i].Magnitude;

            return envelope;
        }

        public float[] FDomainFilterWithEnvelope(float[] inputSignal, double middleCutOffRatio = 0.3, double sideCutoffRatio = 0.03)
        {
            int n = inputSignal.Length;
            Complex[] complexSignal = new Complex[n];
            for (int i = 0; i < n; i++) complexSignal[i] = new Complex(inputSignal[i], 0);

            Fourier.Forward(complexSignal, FourierOptions.Matlab);

            int middleCutOffIndex = (int)(n * middleCutOffRatio / 2);
            int sideCutOffIndex = (int)(n * sideCutoffRatio / 2);

            for (int i = 0; i < n; i++)
            {
                if (!(sideCutoffRatio < i && i < middleCutOffIndex) && !(n - middleCutOffIndex < i && i < n - sideCutOffIndex))
                {
                    complexSignal[i] = Complex.Zero;
                }
            }

            for (int i = 1; i < n / 2; i++) complexSignal[i] *= 2.0;
            for (int i = n / 2 + 1; i < n; i++) complexSignal[i] = Complex.Zero;

            Fourier.Inverse(complexSignal, FourierOptions.Matlab);

            float[] envelope = new float[n];
            for (int i = 0; i < n; i++) envelope[i] = (float)complexSignal[i].Magnitude;

            return envelope;
        }
        #endregion

        #region Normalization Methods
        public float[] BScanNormalization(float[] input, Gate gate, float thresholdmaxratio)
        {
            int length = input.Length;
            float[] output = new float[length];

            int start = Math.Max(gate.StartIndex, 0);
            int end = Math.Min(gate.EndIndex, length - 1);

            float absMax = float.MinValue;
            for (int i = start; i <= end; i++)
            {
                float absVal = Math.Abs(input[i]);
                if (absVal > absMax) absMax = absVal;
            }

            float threshold = absMax * thresholdmaxratio;

            for (int i = 0; i < length; i++)
            {
                if (i >= start && i <= end)
                {
                    output[i] = Math.Min(Math.Abs(input[i]) / threshold, 1);
                }
                else
                {
                    output[i] = 0f;
                }
            }

            return output;
        }

        public List<float> CScanNormalization(float[] input, List<Gate> Gates, int originFirstMaxIndex)
        {
            var firstMax = GetFirstMax(input);
            int firstMaxIndex = firstMax.maxIndex;

            int offset = firstMaxIndex - originFirstMaxIndex;

            var values = GetMaxValuesWithIndex(input, Gates, offset);

            var result = new List<float>();
            for (int i = 0; i < Gates.Count; i++)
            {
                int start = Gates[i].StartIndex + offset;
                int end = Gates[i].EndIndex + offset;
                int maxIndex = values[i].maxIndex;

                float normalized;
                if (end != start)
                {
                    normalized = 1.0f - ((float)(maxIndex - start) / (end - start));
                }
                else
                {
                    normalized = (maxIndex == start) ? 1.0f : 0.0f;
                }

                result.Add(normalized);
            }

            return result;
        }
        #endregion

        // Helper method to convert double array to float array
        public float[] ConvertToFloat(double[] input)
        {
            float[] result = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = (float)input[i];
            }
            return result;
        }

        // Helper method to convert float array to double array
        public double[] ConvertToDouble(float[] input)
        {
            double[] result = new double[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = input[i];
            }
            return result;
        }
    }
} 