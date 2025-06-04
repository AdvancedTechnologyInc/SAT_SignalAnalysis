using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAT_TestProgram.Models;

namespace SAT_TestProgram.Data
{
    /// <summary>
    /// 신호 데이터의 기본 구조를 정의하는 모델 클래스
    /// 원본 데이터와 처리된 데이터, 그리고 각종 알고리즘 처리 결과를 저장
    /// </summary>
    public class DataModel
    {
        // 파일 이름
        public string FileName { get; set; }

        // 데이터 개수
        public int DataNum { get; set; }

        // 데이터 인덱스 배열
        public int[] DataIndex { get; set; }

        // 시간(초) 배열
        public double[] Second { get; set; }

        // 전압 배열
        public double[] Volt { get; set; }

        // X축 데이터 (시간)
        public float[] XData { get; set; }

        // Y축 데이터 (전압)
        public float[] YData { get; set; }

        // 게이트 목록
        public List<SignalProcessor.Gate> Gates { get; set; }

        // 첫 번째 최대값의 인덱스
        public int FirstMaxIndex { get; set; }

        /// <summary>
        /// DataModel 생성자
        /// AlgorithmResults 딕셔너리를 초기화
        /// </summary>
        public DataModel()
        {
            FileName = string.Empty;
            DataNum = 0;
            DataIndex = Array.Empty<int>();
            Second = Array.Empty<double>();
            Volt = Array.Empty<double>();
            XData = Array.Empty<float>();
            YData = Array.Empty<float>();
            Gates = new List<SignalProcessor.Gate>();
            FirstMaxIndex = -1;
        }

        // 데이터 초기화를 위한 생성자
        public DataModel(int dataNum)
        {
            DataNum = dataNum;
            DataIndex = new int[dataNum];
            Second = new double[dataNum];
            Volt = new double[dataNum];
            XData = new float[dataNum];
            YData = new float[dataNum];
            Gates = new List<SignalProcessor.Gate>();
            FirstMaxIndex = -1;
        }

        // 데이터 복사를 위한 복사 생성자
        public DataModel(DataModel other)
        {
            if (other != null)
            {
                DataNum = other.DataNum;
                DataIndex = other.DataIndex?.ToArray() ?? Array.Empty<int>();
                Second = other.Second?.ToArray() ?? Array.Empty<double>();
                Volt = other.Volt?.ToArray() ?? Array.Empty<double>();
                XData = other.XData?.ToArray() ?? Array.Empty<float>();
                YData = other.YData?.ToArray() ?? Array.Empty<float>();
                Gates = other.Gates?.ToList() ?? new List<SignalProcessor.Gate>();
                FirstMaxIndex = other.FirstMaxIndex;
            }
            else
            {
                DataNum = 0;
                DataIndex = Array.Empty<int>();
                Second = Array.Empty<double>();
                Volt = Array.Empty<double>();
                XData = Array.Empty<float>();
                YData = Array.Empty<float>();
                Gates = new List<SignalProcessor.Gate>();
                FirstMaxIndex = -1;
            }
        }

        // 데이터 유효성 검사
        public bool IsValid()
        {
            return DataNum > 0 &&
                   DataIndex != null && DataIndex.Length == DataNum &&
                   Second != null && Second.Length == DataNum &&
                   Volt != null && Volt.Length == DataNum &&
                   XData != null && XData.Length == DataNum &&
                   YData != null && YData.Length == DataNum;
        }

        // 데이터 초기화
        public void Clear()
        {
            DataNum = 0;
            DataIndex = Array.Empty<int>();
            Second = Array.Empty<double>();
            Volt = Array.Empty<double>();
            XData = Array.Empty<float>();
            YData = Array.Empty<float>();
            Gates.Clear();
            FirstMaxIndex = -1;
        }
    }
} 