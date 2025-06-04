using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace SAT_TestProgram.Data
{
    /// <summary>
    /// 데이터 관리를 담당하는 매니저 클래스
    /// 싱글톤 패턴으로 구현되어 전역적인 데이터 접근을 제공
    /// 데이터의 로드, 저장, 검색 및 업데이트 기능을 제공
    /// 이벤트 기반으로 데이터 상태 변경을 통지
    /// </summary>
    public class DataManager
    {
        private static readonly object _lock = new object();
        private static DataManager _instance;

        /// <summary>
        /// DataManager의 싱글톤 인스턴스를 가져옴
        /// </summary>
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new DataManager();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 로드된 모든 데이터셋을 저장하는 리스트
        /// </summary>
        private List<DataModel> _dataSet;

        /// <summary>
        /// 현재 선택된 데이터 모델
        /// </summary>
        public DataModel CurrentData { get; private set; }

        /// <summary>
        /// 데이터가 로드되었을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler<DataModel> OnDataLoaded;

        /// <summary>
        /// 데이터가 처리되었을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler<DataModel> OnDataProcessed;

        /// <summary>
        /// 현재 데이터가 변경되었을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler<DataModel> OnCurrentDataChanged;

        /// <summary>
        /// DataManager 생성자 - private으로 선언하여 외부에서 인스턴스 생성을 막음
        /// </summary>
        private DataManager()
        {
            _dataSet = new List<DataModel>();
        }

        /// <summary>
        /// 파일로부터 데이터를 비동기적으로 로드
        /// </summary>
        /// <param name="filePath">로드할 파일의 경로</param>
        /// <returns>로드 작업 완료를 나타내는 Task</returns>
        public async Task LoadDataAsync(string filePath)
        {
            try
            {
                // CSV 파일 읽기
                string[] lines = await Task.Run(() => File.ReadAllLines(filePath));
                if (lines.Length <= 2) // 헤더와 최소 1개의 데이터가 필요
                {
                    throw new Exception("파일에 데이터가 없습니다.");
                }

                // 유효한 데이터 개수 계산 (2번째 행부터 시작)
                var validData = new List<(double time, double voltage)>();
                await Task.Run(() =>
                {
                    for (int i = 1; i < lines.Length; i++) // 2번째 행부터 시작
                    {
                        var values = lines[i].Split(',');
                        if (values.Length >= 2) // 시간, 전압 데이터가 필요
                        {
                            if (double.TryParse(values[0], out double time) &&
                                double.TryParse(values[1], out double voltage))
                            {
                                validData.Add((time, voltage));
                            }
                        }
                    }
                });

                // 유효한 데이터가 없는 경우
                if (validData.Count == 0)
                {
                    throw new Exception("유효한 데이터가 없습니다.");
                }

                // 데이터 모델 생성 및 초기화
                var data = new DataModel();
                data.DataNum = validData.Count;
                data.DataIndex = Enumerable.Range(0, validData.Count).ToArray();
                data.Second = new double[validData.Count];
                data.Volt = new double[validData.Count];

                // 데이터 할당
                for (int i = 0; i < validData.Count; i++)
                {
                    data.DataIndex[i] = i;
                    data.Second[i] = validData[i].time;  // Keep original seconds
                    data.Volt[i] = validData[i].voltage;
                }

                _dataSet.Add(data);
                OnDataLoaded?.Invoke(this, data);
                
                // 새로 로드된 데이터를 현재 데이터로 설정
                SetCurrentData(data);
            }
            catch (Exception ex)
            {
                throw new Exception($"데이터 로드 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 인덱스의 데이터를 검색
        /// </summary>
        /// <param name="index">검색할 데이터의 인덱스</param>
        /// <returns>찾은 DataModel 객체, 없으면 null</returns>
        public DataModel GetData(int index)
        {
            if (_dataSet == null)
            {
                _dataSet = new List<DataModel>();
                return null;
            }

            try
            {
                return _dataSet.Find(d => d != null && 
                                        d.DataNum > 0 && 
                                        d.DataIndex != null && 
                                        d.DataIndex.Length > 0 && 
                                        d.DataIndex.Contains(index));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 모든 데이터셋을 반환
        /// </summary>
        /// <returns>데이터셋의 복사본</returns>
        public List<DataModel> GetAllData()
        {
            return new List<DataModel>(_dataSet);
        }

        /// <summary>
        /// 처리된 데이터를 업데이트하고 이벤트 발생
        /// </summary>
        /// <param name="index">업데이트할 데이터의 인덱스</param>
        /// <param name="algorithmName">적용된 알고리즘 이름</param>
        /// <param name="processedData">처리된 새 데이터</param>
        /// <exception cref="ArgumentException">데이터가 유효하지 않은 경우</exception>
        public void UpdateProcessedData(int index, string algorithmName, double[] processedData)
        {
            if (string.IsNullOrEmpty(algorithmName))
            {
                throw new ArgumentException("알고리즘 이름이 필요합니다.", nameof(algorithmName));
            }

            if (processedData == null || processedData.Length == 0)
            {
                throw new ArgumentException("처리된 데이터가 필요합니다.", nameof(processedData));
            }

            try
            {
                var data = GetData(index);
                if (data == null)
                {
                    throw new ArgumentException($"인덱스 {index}의 데이터를 찾을 수 없습니다.", nameof(index));
                }

                // 데이터 길이 검증
                if (data.DataNum > 0 && processedData.Length != data.DataNum)
                {
                    throw new ArgumentException($"처리된 데이터의 길이({processedData.Length})가 원본 데이터의 길이({data.DataNum})와 일치하지 않습니다.");
                }

                // 알고리즘 결과 생성 및 저장
                var algorithmResult = new AlgorithmDatas(algorithmName, processedData.Length);
                algorithmResult.ProcessData = (double[])processedData.Clone(); // 데이터 복사본 저장

                OnDataProcessed?.Invoke(this, data);
            }
            catch (Exception ex)
            {
                throw new Exception($"데이터 업데이트 중 오류 발생: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 현재 선택된 데이터를 설정
        /// </summary>
        /// <param name="data">현재 데이터로 설정할 DataModel</param>
        public void SetCurrentData(DataModel data)
        {
            if (data != CurrentData)
            {
                CurrentData = data;
                OnCurrentDataChanged?.Invoke(this, data);
            }
        }

        /// <summary>
        /// 데이터셋을 초기화
        /// </summary>
        public void ClearData()
        {
            _dataSet.Clear();
            CurrentData = null;
            OnCurrentDataChanged?.Invoke(this, null);
        }
    }
} 