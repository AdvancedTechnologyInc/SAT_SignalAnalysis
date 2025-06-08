using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System.Linq;

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
        public (float[] magnitudeData, float[] frequencyAxis, Complex[] complexData) PerformFFT(float[] inputSignal, float samplingRate = 100f)
        {
            int n = inputSignal.Length;
            Complex[] complexSignal = new Complex[n];
            for (int i = 0; i < n; i++) complexSignal[i] = new Complex(inputSignal[i], 0);

            Fourier.Forward(complexSignal, FourierOptions.Matlab);

            // 주파수 축 계산
            float[] frequencyAxis = new float[n];
            float df = samplingRate / n; // 주파수 해상도
            for (int i = 0; i < n/2; i++)
            {
                frequencyAxis[i] = i * df;
            }
            for (int i = n/2; i < n; i++)
            {
                frequencyAxis[i] = (i - n) * df;
            }

            // Magnitude 스펙트럼 계산
            float[] magnitudeSpectrum = new float[n];
            for (int i = 0; i < n; i++)
            {
                magnitudeSpectrum[i] = (float)complexSignal[i].Magnitude;
            }

            return (magnitudeSpectrum, frequencyAxis, complexSignal);
        }

        public (float[] magnitudeSpectrum, float[] frequencyAxis, Complex[] complexSpectrum) ApplyFrequencyFilter(Complex[] inputSignal, float middleCutOff, float sideCutOff, float samplingRate)
        {
            int n = inputSignal.Length;
            float df = samplingRate / n;
            float[] frequencyAxis = Enumerable.Range(0, n).Select(i => i * df).ToArray();

            // Apply frequency filter to complex spectrum
            //Complex[] filteredSpectrum = new Complex[n];
            //for (int i = 0; i < n; i++)
            //{
            //    float frequency = frequencyAxis[i];
            //    if (frequency >= middleCutOff - sideCutOff && frequency <= middleCutOff + sideCutOff)
            //    {
            //        filteredSpectrum[i] = inputSignal[i];
            //    }
            //    else
            //    {
            //        filteredSpectrum[i] = Complex.Zero;
            //    }
            //}
            int middleCutOffIndex = (int)(n * middleCutOff / 2);
            int sideCutOffIndex = (int)(n * sideCutOff / 2);

            for (int i = 0; i < n; i++)
            {
                if (!(sideCutOffIndex < i && i < middleCutOffIndex) &&
                    !(n - middleCutOffIndex < i && i < n - sideCutOffIndex))
                {
                    inputSignal[i] = Complex.Zero;
                }

            }
            // Calculate magnitude spectrum
            float[] magnitudeSpectrum = inputSignal.Select(c => (float)c.Magnitude).ToArray();

            return (magnitudeSpectrum, frequencyAxis, inputSignal);
        }

        public (float[] magnitudeSpectrum, float[] frequencyAxis, Complex[] complexSpectrum) ApplyFrequencyFilter(float[] inputSignal, float middleCutOff, float sideCutOff, float samplingRate)
        {
            int n = inputSignal.Length;
            Complex[] complexSignal = new Complex[n];
            for (int i = 0; i < n; i++)
            {
                complexSignal[i] = new Complex(inputSignal[i], 0);
            }

            return ApplyFrequencyFilter(complexSignal, middleCutOff, sideCutOff, samplingRate);
        }

        public (float[] timeData, float[] timeAxis, Complex[] complexData) PerformIFFT(Complex[] inputSpectrum, float samplingRate)
        {
            int n = inputSpectrum.Length;
            float dt = 1.0f / samplingRate;
            float[] timeAxis = Enumerable.Range(0, n).Select(i => i * dt).ToArray();

            // Perform IFFT
            Complex[] complexTimeData = new Complex[n];
            Array.Copy(inputSpectrum, complexTimeData, n);
            Fourier.Inverse(complexTimeData, FourierOptions.Matlab);

            // Extract real part for time domain data
            float[] timeData = complexTimeData.Select(c => (float)c.Real).ToArray();

            return (timeData, timeAxis, complexTimeData);
        }

        public (float[] timeData, float[] timeAxis, Complex[] complexData) PerformIFFT(float[] inputSpectrum, float samplingRate)
        {
            int n = inputSpectrum.Length;
            Complex[] complexSpectrum = new Complex[n];
            for (int i = 0; i < n; i++)
            {
                complexSpectrum[i] = new Complex(inputSpectrum[i], 0);
            }

            return PerformIFFT(complexSpectrum, samplingRate);
        }

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

        /// <summary>
        /// 가우시안 필터를 적용하여 신호를 스무딩합니다.
        /// </summary>
        /// <param name="inputSignal">입력 신호</param>
        /// <param name="sigma">가우시안 표준편차 (기본값 1.0)</param>
        /// <param name="kernelSize">커널 크기 (기본값 5, 홀수여야 함)</param>
        /// <returns>필터링된 신호</returns>
        public float[] ApplyGaussianFilter(float[] inputSignal, double sigma = 1.0, int kernelSize = 5)
        {
            if (kernelSize % 2 == 0) kernelSize++; // 커널 크기는 홀수여야 함
            int radius = kernelSize / 2;

            // 가우시안 커널 생성
            double[] kernel = new double[kernelSize];
            double sum = 0;
            for (int i = 0; i < kernelSize; i++)
            {
                int x = i - radius;
                kernel[i] = Math.Exp(-(x * x) / (2 * sigma * sigma));
                sum += kernel[i];
            }

            // 커널 정규화
            for (int i = 0; i < kernelSize; i++)
            {
                kernel[i] /= sum;
            }

            // 필터링 적용
            float[] outputSignal = new float[inputSignal.Length];
            for (int i = 0; i < inputSignal.Length; i++)
            {
                double sum2 = 0;
                double weightSum = 0;

                for (int j = -radius; j <= radius; j++)
                {
                    int idx = i + j;
                    if (idx >= 0 && idx < inputSignal.Length)
                    {
                        sum2 += inputSignal[idx] * kernel[j + radius];
                        weightSum += kernel[j + radius];
                    }
                }

                outputSignal[i] = (float)(sum2 / weightSum);
            }

            return outputSignal;
        }

        /// <summary>
        /// 언샤프 마스킹을 적용하여 신호를 선명하게 만듭니다.
        /// </summary>
        /// <param name="inputSignal">입력 신호</param>
        /// <param name="amount">선명도 강도 (기본값 1.5)</param>
        /// <param name="sigma">가우시안 블러 표준편차 (기본값 1.0)</param>
        /// <param name="threshold">적용 임계값 (기본값 0)</param>
        /// <returns>선명화된 신호</returns>
        public float[] ApplyUnsharpMasking(float[] inputSignal, float amount = 1.5f, double sigma = 1.0, float threshold = 0)
        {
            // 가우시안 블러 적용
            float[] blurred = ApplyGaussianFilter(inputSignal, sigma);

            // 언샤프 마스킹 적용
            float[] outputSignal = new float[inputSignal.Length];
            for (int i = 0; i < inputSignal.Length; i++)
            {
                float difference = inputSignal[i] - blurred[i];
                
                // 임계값 이상인 경우에만 선명화 적용
                if (Math.Abs(difference) > threshold)
                {
                    outputSignal[i] = inputSignal[i] + difference * (amount - 1);
                }
                else
                {
                    outputSignal[i] = inputSignal[i];
                }
            }

            return outputSignal;
        }

        /// <summary>
        /// 신호의 Y값을 0을 기준으로 오프셋합니다.
        /// 전체 신호의 평균값을 계산하여 각 데이터 포인트에서 빼줍니다.
        /// </summary>
        /// <param name="inputSignal">입력 신호</param>
        /// <param name="applyAbsolute">절대값 적용 여부</param>
        /// <returns>오프셋이 조정된 신호</returns>
        public float[] ApplyZeroOffset(float[] inputSignal, bool applyAbsolute = false)
        {
            if (inputSignal == null || inputSignal.Length == 0)
                return inputSignal;

            // 평균값 계산
            float mean = 0;
            for (int i = 0; i < inputSignal.Length; i++)
            {
                mean += inputSignal[i];
            }
            mean /= inputSignal.Length;

            // 평균값을 빼서 0 기준으로 오프셋
            float[] outputSignal = new float[inputSignal.Length];
            for (int i = 0; i < inputSignal.Length; i++)
            {
                float offsetValue = inputSignal[i] - mean;
                outputSignal[i] = applyAbsolute ? Math.Abs(offsetValue) : offsetValue;
            }

            return outputSignal;
        }

        /// <summary>
        /// 입력 신호에 threshold 필터를 적용합니다.
        /// threshold 값보다 작은 모든 데이터를 0으로 설정합니다.
        /// </summary>
        /// <param name="inputSignal">입력 신호</param>
        /// <param name="threshold">기준값 (이 값 이하의 데이터는 0으로 설정)</param>
        /// <param name="useAbsoluteValue">절대값 기준 적용 여부 (true일 경우 절대값 기준으로 비교)</param>
        /// <returns>필터링된 신호</returns>
        public float[] ApplyThresholdFilter(float[] inputSignal, float threshold, bool useAbsoluteValue = false)
        {
            if (inputSignal == null || inputSignal.Length == 0)
                return inputSignal;

            float[] outputSignal = new float[inputSignal.Length];
            for (int i = 0; i < inputSignal.Length; i++)
            {
                float value = inputSignal[i];
                if (useAbsoluteValue)
                {
                    // 절대값 기준으로 비교
                    outputSignal[i] = Math.Abs(value) > threshold ? value : 0;
                }
                else
                {
                    // 실제 값 기준으로 비교
                    outputSignal[i] = value > threshold ? value : 0;
                }
            }

            return outputSignal;
        }

        /// <summary>
        /// 힐버트 변환을 수행하여 신호의 해석적 신호(analytic signal)를 구합니다.
        /// </summary>
        /// <param name="inputSignal">입력 신호</param>
        /// <param name="returnEnvelope">true면 포락선을 반환, false면 위상 변이된 신호를 반환</param>
        /// <returns>힐버트 변환된 신호 또는 포락선</returns>
        public float[] ApplyHilbertTransform(float[] inputSignal, bool returnEnvelope = true)
        {
            if (inputSignal == null || inputSignal.Length == 0)
                return inputSignal;

            int n = inputSignal.Length;
            Complex[] complexSignal = new Complex[n];
            
            // 입력 신호를 복소수로 변환
            for (int i = 0; i < n; i++)
            {
                complexSignal[i] = new Complex(inputSignal[i], 0);
            }

            // FFT 수행
            Fourier.Forward(complexSignal, FourierOptions.Matlab);

            // 음수 주파수 성분을 0으로 설정하고 양수 주파수 성분을 2배로
            for (int i = 1; i < n/2; i++)
            {
                complexSignal[i] *= 2.0;
            }
            for (int i = n/2 + 1; i < n; i++)
            {
                complexSignal[i] = Complex.Zero;
            }

            // IFFT 수행
            Fourier.Inverse(complexSignal, FourierOptions.Matlab);

            float[] outputSignal = new float[n];
            if (returnEnvelope)
            {
                // 포락선 계산 (magnitude)
                for (int i = 0; i < n; i++)
                {
                    outputSignal[i] = (float)complexSignal[i].Magnitude;
                }
            }
            else
            {
                // 위상이 90도 변이된 신호 (imaginary part)
                for (int i = 0; i < n; i++)
                {
                    outputSignal[i] = (float)complexSignal[i].Imaginary;
                }
            }

            return outputSignal;
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