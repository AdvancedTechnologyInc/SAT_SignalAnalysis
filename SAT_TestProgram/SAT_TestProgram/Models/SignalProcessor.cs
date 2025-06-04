using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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