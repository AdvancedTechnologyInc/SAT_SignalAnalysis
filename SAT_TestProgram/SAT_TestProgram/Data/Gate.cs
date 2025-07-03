using System;

namespace SAT_TestProgram.Data
{
    /// <summary>
    /// 게이트의 기본 정보를 담는 부모 클래스
    /// 시작과 끝 위치를 관리
    /// </summary>
    public class Gate
    {
        private int _start;
        private int _end;

        /// <summary>
        /// 게이트 시작 위치
        /// </summary>
        public int Start
        {
            get => _start;
            set
            {
                _start = value;
                OnStartChanged();
            }
        }

        /// <summary>
        /// 게이트 끝 위치
        /// </summary>
        public int End
        {
            get => _end;
            set
            {
                _end = value;
                OnEndChanged();
            }
        }

        /// <summary>
        /// 게이트 거리 (End - Start)
        /// </summary>
        public int Distance
        {
            get => Math.Abs(End - Start);
        }

        /// <summary>
        /// Gate 생성자
        /// </summary>
        public Gate()
        {
            Start = 0;
            End = 0;
        }

        /// <summary>
        /// Gate 생성자 (매개변수 포함)
        /// </summary>
        /// <param name="start">게이트 시작 위치</param>
        /// <param name="end">게이트 끝 위치</param>
        public Gate(int start, int end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// 시작 위치가 변경될 때 호출되는 가상 메서드
        /// </summary>
        protected virtual void OnStartChanged()
        {
        }

        /// <summary>
        /// 끝 위치가 변경될 때 호출되는 가상 메서드
        /// </summary>
        protected virtual void OnEndChanged()
        {
        }
    }
} 