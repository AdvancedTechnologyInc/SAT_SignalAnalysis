using System;

namespace SAT_TestProgram
{
    public static class ConstValue
    {
        // Time Unit Conversion Constants
        public static class TimeUnit
        {
            public const double SecondToNanosecond = 1e9; // 1 second = 1,000,000,000 nanoseconds
        }

        // Plot Axis Default Values
        public static class PlotAxis
        {
            // X Axis
            public const double DefaultXMin = 0;
            public const double DefaultXMax = 100;
            
            // Y Axis
            public const double DefaultYMin = -5;
            public const double DefaultYMax = 5;

            // Y Axis Slider Range
            public const double YAxisSliderMin = -10;
            public const double YAxisSliderMax = 10;
        }

        // Plot Titles
        public static class PlotTitles
        {
            public const string RawDataTitle = "Raw Data";
            public const string VoidDataTitle = "Void Data";
            public const string XAxisLabel = "Index";
            public const string YAxisLabel = "Voltage";
        }

        // Algorithm Constants
        public static class AlgorithmFactors
        {
            public const double Algorithm1Factor = 1.1;  // 10% increase
            public const double Algorithm2Factor = 0.9;  // 10% decrease
            public const double Algorithm3Offset = 0.5;  // Add 0.5
            public const double Algorithm4Offset = -0.5; // Subtract 0.5
        }

        // File Dialog Filters
        public static class FileDialogs
        {
            public const string CsvFilter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            public const int DefaultFilterIndex = 1;
        }

        // Error Messages
        public static class ErrorMessages
        {
            public const string LoadRawDataError = "Raw 데이터 로드 중 오류 발생: {0}";
            public const string LoadVoidDataError = "Void 데이터 로드 중 오류 발생: {0}";
            public const string NoDataLoadedError = "데이터를 먼저 로드해주세요.";
        }
    }
} 