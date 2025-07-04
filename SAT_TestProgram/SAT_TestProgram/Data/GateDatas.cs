using System;
using System.ComponentModel;

namespace SAT_TestProgram.Data
{
    /// <summary>
    /// 게이트 데이터를 관리하는 클래스
    /// 게이트의 인덱스, 시작/끝 위치, 거리 등의 정보를 저장
    /// </summary>
    public class GateDatas : Gate, INotifyPropertyChanged
    {
        private string _name;
        private int _maxIndexRaw; // Raw Signal에서 게이트 영역 내에서 가장 큰 Voltage를 가진 Index
        private int _maxIndexVoid; // Void Signal에서 게이트 영역 내에서 가장 큰 Voltage를 가진 Index
        private int _indexDifferenceRaw; // Raw Signal에서 이전 게이트와의 Index 차이
        private int _indexDifferenceVoid; // Void Signal에서 이전 게이트와의 Index 차이
        private double _calculatedDistanceRaw; // Raw Signal에서 계산된 거리 (μm)
        private double _calculatedDistanceVoid; // Void Signal에서 계산된 거리 (μm)
        private double _soundVelocity; // 개별 게이트의 음속 (m/s)
        private int _frameStart; // B Scan에서 사용할 Frame 시작 인덱스
        private int _frameEnd; // B Scan에서 사용할 Frame 끝 인덱스

        /// <summary>
        /// 게이트 시작 위치 (double 타입으로 오버라이드)
        /// </summary>
        public new double Start
        {
            get => base.Start;
            set
            {
                base.Start = (int)value;
                OnPropertyChanged(nameof(Start));
                OnPropertyChanged(nameof(Distance));
            }
        }

        /// <summary>
        /// 게이트 끝 위치 (double 타입으로 오버라이드)
        /// </summary>
        public new double End
        {
            get => base.End;
            set
            {
                base.End = (int)value;
                OnPropertyChanged(nameof(End));
                OnPropertyChanged(nameof(Distance));
            }
        }

        /// <summary>
        /// 게이트 거리 (End - Start)
        /// </summary>
        public new double Distance
        {
            get => Math.Abs(End - Start);
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
        /// Raw Signal에서 게이트 영역 내에서 가장 큰 Voltage를 가진 Index
        /// </summary>
        public int MaxIndexRaw
        {
            get => _maxIndexRaw;
            set
            {
                _maxIndexRaw = value;
                OnPropertyChanged(nameof(MaxIndexRaw));
            }
        }

        /// <summary>
        /// Void Signal에서 게이트 영역 내에서 가장 큰 Voltage를 가진 Index
        /// </summary>
        public int MaxIndexVoid
        {
            get => _maxIndexVoid;
            set
            {
                _maxIndexVoid = value;
                OnPropertyChanged(nameof(MaxIndexVoid));
            }
        }

        /// <summary>
        /// 현재 활성화된 신호에 따른 MaxIndex (Raw/Void 구분)
        /// </summary>
        /// <param name="isRawSignal">Raw Signal인지 여부</param>
        /// <returns>해당 신호의 MaxIndex</returns>
        public int GetMaxIndex(bool isRawSignal)
        {
            return isRawSignal ? MaxIndexRaw : MaxIndexVoid;
        }

        /// <summary>
        /// Raw Signal에서 이전 게이트와의 Index 차이
        /// </summary>
        public int IndexDifferenceRaw
        {
            get => _indexDifferenceRaw;
            set
            {
                _indexDifferenceRaw = value;
                OnPropertyChanged(nameof(IndexDifferenceRaw));
            }
        }

        /// <summary>
        /// Void Signal에서 이전 게이트와의 Index 차이
        /// </summary>
        public int IndexDifferenceVoid
        {
            get => _indexDifferenceVoid;
            set
            {
                _indexDifferenceVoid = value;
                OnPropertyChanged(nameof(IndexDifferenceVoid));
            }
        }

        /// <summary>
        /// Raw Signal에서 계산된 거리 (μm)
        /// </summary>
        public double CalculatedDistanceRaw
        {
            get => _calculatedDistanceRaw;
            set
            {
                _calculatedDistanceRaw = value;
                OnPropertyChanged(nameof(CalculatedDistanceRaw));
                OnPropertyChanged(nameof(CalculatedDistanceRawFormatted));
            }
        }

        /// <summary>
        /// Raw Signal에서 계산된 거리 (μm) - 소수점 첫째 자리까지 포맷
        /// </summary>
        public string CalculatedDistanceRawFormatted
        {
            get => _calculatedDistanceRaw.ToString("F1");
        }

        /// <summary>
        /// Void Signal에서 계산된 거리 (μm)
        /// </summary>
        public double CalculatedDistanceVoid
        {
            get => _calculatedDistanceVoid;
            set
            {
                _calculatedDistanceVoid = value;
                OnPropertyChanged(nameof(CalculatedDistanceVoid));
                OnPropertyChanged(nameof(CalculatedDistanceVoidFormatted));
            }
        }

        /// <summary>
        /// Void Signal에서 계산된 거리 (μm) - 소수점 첫째 자리까지 포맷
        /// </summary>
        public string CalculatedDistanceVoidFormatted
        {
            get => _calculatedDistanceVoid.ToString("F1");
        }

        /// <summary>
        /// 개별 게이트의 음속 (m/s)
        /// </summary>
        public double SoundVelocity
        {
            get => _soundVelocity;
            set
            {
                _soundVelocity = value;
                OnPropertyChanged(nameof(SoundVelocity));
            }
        }

        /// <summary>
        /// B Scan에서 사용할 Frame 시작 인덱스
        /// </summary>
        public int FrameStart
        {
            get => _frameStart;
            set
            {
                _frameStart = value;
                OnPropertyChanged(nameof(FrameStart));
            }
        }

        /// <summary>
        /// B Scan에서 사용할 Frame 끝 인덱스
        /// </summary>
        public int FrameEnd
        {
            get => _frameEnd;
            set
            {
                _frameEnd = value;
                OnPropertyChanged(nameof(FrameEnd));
            }
        }

        /// <summary>
        /// GateDatas 생성자
        /// </summary>
        public GateDatas() : base()
        {
            Name = "Gate";
        }

        /// <summary>
        /// GateDatas 생성자 (매개변수 포함)
        /// </summary>
        /// <param name="start">게이트 시작 위치</param>
        /// <param name="end">게이트 끝 위치</param>
        /// <param name="name">게이트 이름</param>
        public GateDatas(double start, double end, string name = "Gate") : base((int)start, (int)end)
        {
            Name = name;
        }

        /// <summary>
        /// 게이트 데이터 복사
        /// </summary>
        /// <returns>복사된 GateDatas 객체</returns>
        public GateDatas Clone()
        {
            return new GateDatas(Start, End, Name);
        }

        /// <summary>
        /// 시작 위치가 변경될 때 호출되는 메서드 (부모 클래스 오버라이드)
        /// </summary>
        protected override void OnStartChanged()
        {
            base.OnStartChanged();
            // PropertyChanged 이벤트는 Start 속성 setter에서 처리됨
        }

        /// <summary>
        /// 끝 위치가 변경될 때 호출되는 메서드 (부모 클래스 오버라이드)
        /// </summary>
        protected override void OnEndChanged()
        {
            base.OnEndChanged();
            // PropertyChanged 이벤트는 End 속성 setter에서 처리됨
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