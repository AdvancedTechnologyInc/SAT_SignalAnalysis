using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;

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
        /// 게이트 데이터가 변경되었을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler<GateDatas> OnGateDataChanged;

        private Dictionary<string, AlgorithmDatas> _rawAlgorithmDatas;
        private Dictionary<string, AlgorithmDatas> _voidAlgorithmDatas;

        // 게이트 데이터 관리
        private ObservableCollection<GateDatas> _gateDatas;

        // Index Start/Stop 관리 (Signal 데이터에 1개만 존재)
        private int _indexStart = 0;
        private int _indexStop = 100;

        /// <summary>
        /// 게이트 데이터 컬렉션
        /// </summary>
        public ObservableCollection<GateDatas> GateDatas
        {
            get => _gateDatas;
            private set
            {
                _gateDatas = value;
            }
        }

        /// <summary>
        /// Index Start 값
        /// </summary>
        public int IndexStart
        {
            get => _indexStart;
            set
            {
                _indexStart = value;
                OnGateDataChanged?.Invoke(this, null);
            }
        }

        /// <summary>
        /// Index Stop 값
        /// </summary>
        public int IndexStop
        {
            get => _indexStop;
            set
            {
                _indexStop = value;
                OnGateDataChanged?.Invoke(this, null);
            }
        }

        /// <summary>
        /// DataManager 생성자 - private으로 선언하여 외부에서 인스턴스 생성을 막음
        /// </summary>
        private DataManager()
        {
            _dataSet = new List<DataModel>();
            _rawAlgorithmDatas = new Dictionary<string, AlgorithmDatas>();
            _voidAlgorithmDatas = new Dictionary<string, AlgorithmDatas>();
            _gateDatas = new ObservableCollection<GateDatas>();
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
                using (var reader = new StreamReader(filePath))
                {
                    string line;
                    var dataIndex = new List<int>();
                    var timeData = new List<double>();
                    var voltData = new List<double>();
                    var index = 0;

                    // Skip header
                    await reader.ReadLineAsync();

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        var values = line.Split(',');
                        if (values.Length >= 2 && double.TryParse(values[0], out double time) && double.TryParse(values[1], out double volt))
                        {
                            dataIndex.Add(index++);
                            timeData.Add(time * ConstValue.TimeUnit.SecondToNanosecond); // 초 단위를 나노초로 변환
                            voltData.Add(volt);
                        }
                    }

                    CurrentData = new DataModel
                    {
                        DataNum = dataIndex.Count,
                        DataIndex = dataIndex.ToArray(),
                        Second = timeData.ToArray(),
                        Volt = voltData.ToArray(),
                        XData = timeData.Select(t => (float)t).ToArray(),
                        YData = voltData.Select(v => (float)v).ToArray()
                    };

                    OnDataLoaded?.Invoke(this, CurrentData);
                }
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
        public void UpdateProcessedData(int index, string algorithmName, float[] processedData)
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
                var algorithmResult = new AlgorithmDatas(algorithmName, processedData.Length)
                {
                    Name = algorithmName,
                    XData = data.XData.ToArray(),
                    YData = processedData.ToArray(),
                    Gates = data.Gates?.ToList(),
                    FirstMaxIndex = data.FirstMaxIndex
                };

                _rawAlgorithmDatas[algorithmName] = algorithmResult;
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
            CurrentData = data;
            OnCurrentDataChanged?.Invoke(this, data);
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

        public void AddAlgorithmData(AlgorithmDatas data, bool isRawData = true)
        {
            if (data == null) return;
            
            if (isRawData)
                _rawAlgorithmDatas[data.Name] = data;
            else
                _voidAlgorithmDatas[data.Name] = data;
        }

        public AlgorithmDatas GetAlgorithmData(string name, bool isRawData = true)
        {
            var datas = isRawData ? _rawAlgorithmDatas : _voidAlgorithmDatas;
            return datas.TryGetValue(name, out var data) ? data : null;
        }

        public List<AlgorithmDatas> GetAllAlgorithmDatas(bool isRawData = true)
        {
            return (isRawData ? _rawAlgorithmDatas : _voidAlgorithmDatas).Values.ToList();
        }

        public void ClearAlgorithmDatas(bool isRawData = true)
        {
            if (isRawData)
                _rawAlgorithmDatas.Clear();
            else
                _voidAlgorithmDatas.Clear();
        }

        public void ClearAllAlgorithmDatas()
        {
            _rawAlgorithmDatas.Clear();
            _voidAlgorithmDatas.Clear();
        }

        #region Gate Data Management

        /// <summary>
        /// 게이트 데이터 추가
        /// </summary>
        /// <param name="gateData">추가할 게이트 데이터</param>
        public void AddGateData(GateDatas gateData)
        {
            if (gateData == null) return;

            _gateDatas.Add(gateData);
            OnGateDataChanged?.Invoke(this, gateData);
        }

        /// <summary>
        /// 게이트 데이터 수정
        /// </summary>
        /// <param name="index">수정할 게이트의 인덱스</param>
        /// <param name="gateData">수정된 게이트 데이터</param>
        public void UpdateGateData(int index, GateDatas gateData)
        {
            if (gateData == null || index < 0 || index >= _gateDatas.Count) return;

            _gateDatas[index] = gateData;
            OnGateDataChanged?.Invoke(this, gateData);
        }

        /// <summary>
        /// 게이트 데이터 삭제
        /// </summary>
        /// <param name="index">삭제할 게이트의 인덱스</param>
        public void RemoveGateData(int index)
        {
            if (index < 0 || index >= _gateDatas.Count) return;

            var removedGate = _gateDatas[index];
            _gateDatas.RemoveAt(index);
            OnGateDataChanged?.Invoke(this, removedGate);
        }

        /// <summary>
        /// 게이트 데이터 가져오기
        /// </summary>
        /// <param name="index">가져올 게이트의 인덱스</param>
        /// <returns>게이트 데이터</returns>
        public GateDatas GetGateData(int index)
        {
            if (index < 0 || index >= _gateDatas.Count) return null;
            return _gateDatas[index];
        }

        /// <summary>
        /// 모든 게이트 데이터 가져오기
        /// </summary>
        /// <returns>게이트 데이터 리스트</returns>
        public List<GateDatas> GetAllGateDatas()
        {
            return _gateDatas.ToList();
        }

        /// <summary>
        /// 게이트 데이터 초기화
        /// </summary>
        public void ClearGateDatas()
        {
            _gateDatas.Clear();
            OnGateDataChanged?.Invoke(this, null);
        }

        /// <summary>
        /// 게이트 데이터 개수
        /// </summary>
        public int GateDataCount => _gateDatas.Count;

        #endregion
    }
} 