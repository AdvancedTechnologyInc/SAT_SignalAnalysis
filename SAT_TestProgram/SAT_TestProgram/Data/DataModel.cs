using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAT_TestProgram.Data
{
    /// <summary>
    /// 신호 데이터의 기본 구조를 정의하는 모델 클래스
    /// 원본 데이터와 처리된 데이터, 그리고 각종 알고리즘 처리 결과를 저장
    /// </summary>
    public class DataModel
    {

        // 데이터 개수
        public int DataNum { get; set; }

        // 데이터 인덱스 배열
        public int[] DataIndex { get; set; }

        // 시간(초) 배열
        public double[] Second { get; set; }

        // 전압 배열
        public double[] Volt { get; set; }

        /// <summary>
        /// DataModel 생성자
        /// AlgorithmResults 딕셔너리를 초기화
        /// </summary>
        public DataModel()
        {
            DataNum = 0;
            DataIndex = Array.Empty<int>();
            Second = Array.Empty<double>();
            Volt = Array.Empty<double>();
        }

        // 데이터 초기화를 위한 생성자
        public DataModel(int dataNum)
        {
            DataNum = dataNum;
            DataIndex = new int[dataNum];
            Second = new double[dataNum];
            Volt = new double[dataNum];
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
            }
            else
            {
                DataNum = 0;
                DataIndex = Array.Empty<int>();
                Second = Array.Empty<double>();
                Volt = Array.Empty<double>();
            }
        }

        // 데이터 유효성 검사
        public bool IsValid()
        {
            return DataNum > 0 &&
                   DataIndex != null && DataIndex.Length == DataNum &&
                   Second != null && Second.Length == DataNum &&
                   Volt != null && Volt.Length == DataNum;
        }

        // 데이터 초기화
        public void Clear()
        {
            DataNum = 0;
            DataIndex = Array.Empty<int>();
            Second = Array.Empty<double>();
            Volt = Array.Empty<double>();
        }
    }
} 