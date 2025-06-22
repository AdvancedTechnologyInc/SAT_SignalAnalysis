using System;
using System.ComponentModel;

namespace SAT_TestProgram.Data
{
    /// <summary>
    /// 게이트 데이터를 관리하는 클래스
    /// 게이트의 인덱스, 시작/끝 위치, 거리 등의 정보를 저장
    /// </summary>
    public class GateDatas : INotifyPropertyChanged
    {
        private int _index;
        private double _gateStart;
        private double _gateStop;
        private double _distance;
        private string _name;

        /// <summary>
        /// 게이트 인덱스
        /// </summary>
        public int Index
        {
            get => _index;
            set
            {
                _index = value;
                OnPropertyChanged(nameof(Index));
            }
        }

        /// <summary>
        /// 게이트 시작 위치
        /// </summary>
        public double GateStart
        {
            get => _gateStart;
            set
            {
                _gateStart = value;
                CalculateDistance();
                OnPropertyChanged(nameof(GateStart));
            }
        }

        /// <summary>
        /// 게이트 끝 위치
        /// </summary>
        public double GateStop
        {
            get => _gateStop;
            set
            {
                _gateStop = value;
                CalculateDistance();
                OnPropertyChanged(nameof(GateStop));
            }
        }

        /// <summary>
        /// 게이트 거리 (GateStop - GateStart)
        /// </summary>
        public double Distance
        {
            get => _distance;
            private set
            {
                _distance = value;
                OnPropertyChanged(nameof(Distance));
            }
        }

        /// <summary>
        /// 게이트 이름
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// GateDatas 생성자
        /// </summary>
        public GateDatas()
        {
            Index = 0;
            GateStart = 0;
            GateStop = 0;
            Distance = 0;
            Name = "Gate";
        }

        /// <summary>
        /// GateDatas 생성자 (매개변수 포함)
        /// </summary>
        /// <param name="index">게이트 인덱스</param>
        /// <param name="gateStart">게이트 시작 위치</param>
        /// <param name="gateStop">게이트 끝 위치</param>
        /// <param name="name">게이트 이름</param>
        public GateDatas(int index, double gateStart, double gateStop, string name = "Gate")
        {
            Index = index;
            GateStart = gateStart;
            GateStop = gateStop;
            Name = name;
            CalculateDistance();
        }

        /// <summary>
        /// 거리 계산
        /// </summary>
        private void CalculateDistance()
        {
            Distance = Math.Abs(GateStop - GateStart);
        }

        /// <summary>
        /// 게이트 데이터 복사
        /// </summary>
        /// <returns>복사된 GateDatas 객체</returns>
        public GateDatas Clone()
        {
            return new GateDatas(Index, GateStart, GateStop, Name);
        }

        /// <summary>
        /// PropertyChanged 이벤트
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 속성 변경 알림
        /// </summary>
        /// <param name="propertyName">변경된 속성 이름</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 