using System;
using System.Collections.Generic;

namespace SAT_TestProgram.Models
{
    /// <summary>
    /// 게이트 설정을 관리하고 두께 계산을 수행하는 에디터 클래스
    /// 게이트 설정 변경 및 두께 계산 기능을 제공
    /// 이벤트 기반으로 설정 변경을 통지
    /// </summary>
    public class GateEditor
    {
        /// <summary>
        /// 게이트 설정이 변경되었을 때 발생하는 이벤트
        /// </summary>
        public event EventHandler<GateSettings> OnGateSettingsChanged;

        /// <summary>
        /// 두께 계산을 수행하는 계산기 인스턴스
        /// </summary>
        private ThicknessCalculator _calculator;

        /// <summary>
        /// GateEditor 생성자
        /// 두께 계산기를 초기화
        /// </summary>
        public GateEditor()
        {
            _calculator = new ThicknessCalculator();
        }

        /// <summary>
        /// 게이트 설정을 업데이트하고 이벤트 발생
        /// </summary>
        /// <param name="settings">새로운 게이트 설정</param>
        public void UpdateGateSettings(GateSettings settings)
        {
            // Validate and update gate settings
            OnGateSettingsChanged?.Invoke(this, settings);
        }

        /// <summary>
        /// 신호 데이터와 게이트 설정을 기반으로 두께를 계산
        /// </summary>
        /// <param name="signalData">분석할 신호 데이터</param>
        /// <param name="settings">게이트 설정</param>
        /// <returns>계산된 두께 값</returns>
        public double CalculateThickness(double[] signalData, GateSettings settings)
        {
            return _calculator.Calculate(signalData, settings);
        }
    }

    /// <summary>
    /// 두께 계산을 수행하는 계산기 클래스
    /// 게이트 설정에 따라 신호 데이터로부터 두께를 계산
    /// </summary>
    public class ThicknessCalculator
    {
        /// <summary>
        /// 신호 데이터와 게이트 설정을 기반으로 두께를 계산
        /// </summary>
        /// <param name="signalData">분석할 신호 데이터</param>
        /// <param name="settings">게이트 설정</param>
        /// <returns>계산된 두께 값</returns>
        public double Calculate(double[] signalData, GateSettings settings)
        {
            // TODO: Implement thickness calculation logic based on gate settings
            return 0.0;
        }
    }

    /// <summary>
    /// 게이트의 설정 정보를 저장하는 클래스
    /// 게이트의 위치, 임계값, 타입 등의 설정을 포함
    /// </summary>
    public class GateSettings
    {
        /// <summary>
        /// 게이트의 시작 위치
        /// </summary>
        public double StartPosition { get; set; }

        /// <summary>
        /// 게이트의 끝 위치
        /// </summary>
        public double EndPosition { get; set; }

        /// <summary>
        /// 게이트의 임계값
        /// </summary>
        public double Threshold { get; set; }

        /// <summary>
        /// 게이트의 타입 (예: "Peak", "Edge" 등)
        /// </summary>
        public string GateType { get; set; }

        /// <summary>
        /// 추가적인 게이트 매개변수를 저장하는 딕셔너리
        /// </summary>
        public Dictionary<string, double> AdditionalParameters { get; set; }

        /// <summary>
        /// GateSettings 생성자
        /// 추가 매개변수 딕셔너리를 초기화
        /// </summary>
        public GateSettings()
        {
            AdditionalParameters = new Dictionary<string, double>();
        }
    }
} 