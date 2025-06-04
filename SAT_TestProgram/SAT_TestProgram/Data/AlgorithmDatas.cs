using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAT_TestProgram.Models;

namespace SAT_TestProgram.Data
{
    /// <summary>
    /// 알고리즘 처리 결과를 저장하는 데이터 클래스
    /// </summary>
    public class AlgorithmDatas
    {
        // 알고리즘 이름
        public string Name { get; set; }

        // X축 데이터 (시간)
        public float[] XData { get; set; }

        // Y축 데이터 (전압)
        public float[] YData { get; set; }

        // 게이트 목록
        public List<SignalProcessor.Gate> Gates { get; set; }

        // 첫 번째 최대값의 인덱스
        public int FirstMaxIndex { get; set; }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public AlgorithmDatas()
        {
            Name = string.Empty;
            XData = Array.Empty<float>();
            YData = Array.Empty<float>();
            Gates = new List<SignalProcessor.Gate>();
            FirstMaxIndex = -1;
        }

        /// <summary>
        /// 데이터 초기화를 위한 생성자
        /// </summary>
        /// <param name="name">알고리즘 이름</param>
        /// <param name="dataLength">데이터 배열 길이</param>
        public AlgorithmDatas(string name, int dataLength)
        {
            Name = name;
            XData = new float[dataLength];
            YData = new float[dataLength];
            Gates = new List<SignalProcessor.Gate>();
            FirstMaxIndex = -1;
        }

        /// <summary>
        /// 데이터 복사를 위한 복사 생성자
        /// </summary>
        /// <param name="other">복사할 AlgorithmDatas 객체</param>
        public AlgorithmDatas(AlgorithmDatas other)
        {
            if (other != null)
            {
                Name = other.Name;
                XData = other.XData?.ToArray() ?? Array.Empty<float>();
                YData = other.YData?.ToArray() ?? Array.Empty<float>();
                Gates = other.Gates?.ToList() ?? new List<SignalProcessor.Gate>();
                FirstMaxIndex = other.FirstMaxIndex;
            }
            else
            {
                Name = string.Empty;
                XData = Array.Empty<float>();
                YData = Array.Empty<float>();
                Gates = new List<SignalProcessor.Gate>();
                FirstMaxIndex = -1;
            }
        }

        /// <summary>
        /// 데이터 유효성 검사
        /// </summary>
        /// <returns>데이터가 유효한지 여부</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) &&
                   XData != null && XData.Length > 0 &&
                   YData != null && YData.Length > 0 &&
                   XData.Length == YData.Length;
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Clear()
        {
            Name = string.Empty;
            XData = Array.Empty<float>();
            YData = Array.Empty<float>();
            Gates.Clear();
            FirstMaxIndex = -1;
        }
    }
}
