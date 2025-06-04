using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAT_TestProgram.Data
{
    /// <summary>
    /// 알고리즘 처리 결과를 저장하는 데이터 클래스
    /// </summary>
    public class AlgorithmDatas
    {
        // 알고리즘 이름
        public string Algorithm { get; set; }

        // 처리된 데이터 배열
        public double[] ProcessData { get; set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public AlgorithmDatas()
        {
            Algorithm = string.Empty;
            ProcessData = Array.Empty<double>();
        }

        /// <summary>
        /// 데이터 초기화를 위한 생성자
        /// </summary>
        /// <param name="algorithm">알고리즘 이름</param>
        /// <param name="dataLength">데이터 배열 길이</param>
        public AlgorithmDatas(string algorithm, int dataLength)
        {
            Algorithm = algorithm;
            ProcessData = new double[dataLength];
        }

        /// <summary>
        /// 데이터 복사를 위한 복사 생성자
        /// </summary>
        /// <param name="other">복사할 AlgorithmDatas 객체</param>
        public AlgorithmDatas(AlgorithmDatas other)
        {
            if (other != null)
            {
                Algorithm = other.Algorithm;
                ProcessData = other.ProcessData?.ToArray() ?? Array.Empty<double>();
            }
            else
            {
                Algorithm = string.Empty;
                ProcessData = Array.Empty<double>();
            }
        }

        /// <summary>
        /// 데이터 유효성 검사
        /// </summary>
        /// <returns>데이터가 유효한지 여부</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Algorithm) &&
                   ProcessData != null &&
                   ProcessData.Length > 0;
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Clear()
        {
            Algorithm = string.Empty;
            ProcessData = Array.Empty<double>();
        }
    }
}
