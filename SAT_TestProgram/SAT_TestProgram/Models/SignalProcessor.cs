using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace SAT_TestProgram.Models
{
    /// <summary>
    /// 신호 처리 알고리즘을 관리하고 실행하는 프로세서 클래스
    /// 여러 알고리즘을 등록하고 관리하며, 요청에 따라 실행
    /// 이벤트 기반으로 처리 완료를 통지
    /// </summary>
    public class SignalProcessor
    {
        /// <summary>
        /// 등록된 알고리즘들을 저장하는 딕셔너리
        /// Key: 알고리즘 이름, Value: 알고리즘 구현체
        /// </summary>
        private Dictionary<string, ISignalAlgorithm> _algorithms;

        /// <summary>
        /// 신호 처리가 완료되었을 때 발생하는 이벤트
        /// 알고리즘 이름과 처리 결과를 함께 전달
        /// </summary>
        public event EventHandler<(string AlgorithmName, double[] Result)> OnProcessingComplete;

        /// <summary>
        /// SignalProcessor 생성자
        /// 알고리즘 딕셔너리를 초기화
        /// </summary>
        public SignalProcessor()
        {
            _algorithms = new Dictionary<string, ISignalAlgorithm>();
        }

        /// <summary>
        /// 새로운 알고리즘을 등록
        /// </summary>
        /// <param name="name">알고리즘의 이름</param>
        /// <param name="algorithm">알고리즘 구현체</param>
        public void RegisterAlgorithm(string name, ISignalAlgorithm algorithm)
        {
            _algorithms[name] = algorithm;
        }

        /// <summary>
        /// 지정된 알고리즘으로 신호를 처리
        /// </summary>
        /// <param name="algorithmName">사용할 알고리즘의 이름</param>
        /// <param name="data">처리할 신호 데이터</param>
        /// <returns>처리된 신호 데이터</returns>
        /// <exception cref="KeyNotFoundException">지정된 알고리즘이 없을 경우 발생</exception>
        public async Task<double[]> ProcessSignal(string algorithmName, double[] data)
        {
            if (_algorithms.TryGetValue(algorithmName, out var algorithm))
            {
                var result = await algorithm.ProcessAsync(data);
                OnProcessingComplete?.Invoke(this, (algorithmName, result));
                return result;
            }
            throw new KeyNotFoundException($"Algorithm '{algorithmName}' not found");
        }

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
        public float[] FDomainFilter(float[] inputSignal, double middleCutOffRatio = 0.2, double sideCutoffRatio = 0.02)
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

                if (start < 0 || end >= input.Length || values[i].maxIndex < 0)
                {
                    result.Add(0f);
                }
                else
                {
                    result.Add(values[i].maxValue);
                }
            }

            return result;
        }
        #endregion

        #region Array Conversion Methods
        public float[] ConvertToFloat(double[] input)
        {
            if (input == null) return null;
            float[] result = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = (float)input[i];
            }
            return result;
        }

        public double[] ConvertToDouble(float[] input)
        {
            if (input == null) return null;
            double[] result = new double[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                result[i] = input[i];
            }
            return result;
        }
        #endregion
    }

    /// <summary>
    /// 신호 처리 알고리즘의 인터페이스
    /// 모든 신호 처리 알고리즘은 이 인터페이스를 구현해야 함
    /// </summary>
    public interface ISignalAlgorithm
    {
        /// <summary>
        /// 신호를 비동기적으로 처리
        /// </summary>
        /// <param name="data">처리할 신호 데이터</param>
        /// <returns>처리된 신호 데이터</returns>
        Task<double[]> ProcessAsync(double[] data);
    }

    /// <summary>
    /// 기본 필터 알고리즘 구현 예시
    /// </summary>
    public class FilterAlgorithm : ISignalAlgorithm
    {
        /// <summary>
        /// 신호에 필터를 적용하는 처리 구현
        /// </summary>
        /// <param name="data">필터링할 신호 데이터</param>
        /// <returns>필터링된 신호 데이터</returns>
        public async Task<double[]> ProcessAsync(double[] data)
        {
            // TODO: Implement actual filtering logic
            return await Task.FromResult(data);
        }
    }
} 