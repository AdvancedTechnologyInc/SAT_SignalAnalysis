using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SAT_TestProgram.Data;
using SAT_TestProgram.Models;
using System.Windows.Threading;
using ScottPlot;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using ScottPlot.Plottable;
using Xceed.Wpf.Toolkit;
using MathNet.Numerics;

namespace SAT_TestProgram
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly DataManager _dataManager;
        private readonly Models.SignalProcessor _signalProcessor;
        private DataModel _rawSignalData;
        private DataModel _voidSignalData;
        private ObservableCollection<string> _appliedAlgorithms;
        private ScatterPlot rawScatter;
        private ScatterPlot voidScatter;

        public MainWindow()
        {
            InitializeComponent();
            _dataManager = DataManager.Instance;
            _signalProcessor = new Models.SignalProcessor();

            // Initialize variables
            _rawSignalData = null;
            _voidSignalData = null;
            _appliedAlgorithms = new ObservableCollection<string>();

            // Bind the algorithms list
            lstAppliedAlgorithms.ItemsSource = _appliedAlgorithms;

            InitializePlots();

            // Add mouse click event handlers for plots
            plotUpper.MouseDown += Plot_MouseDown;
            plotLower.MouseDown += Plot_MouseDown;
        }

        private void InitializePlots()
        {
            try
            {
                // Initialize main plots
                plotUpper.Plot.Title("Raw Signal");
                plotUpper.Plot.XLabel("Time [ns]");
                plotUpper.Plot.YLabel("Voltage");
                plotUpper.Refresh();

                plotLower.Plot.Title("Void Signal");
                plotLower.Plot.XLabel("Time [ns]");
                plotLower.Plot.YLabel("Voltage");
                plotLower.Refresh();

                plotPreview.Plot.Title("Preview");
                plotPreview.Plot.XLabel("Time [ns]");
                plotPreview.Plot.YLabel("Voltage");
                plotPreview.Refresh();

                // Set initial axis limits
                InitializeAxisLimits();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"플롯 초기화 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeAxisLimits()
        {
            try
            {
                // Set initial values for range sliders
                if (rangeSliderX != null)
                {
                    rangeSliderX.Minimum = ConstValue.PlotAxis.DefaultXMin;
                    rangeSliderX.Maximum = ConstValue.PlotAxis.DefaultXMax;
                    rangeSliderX.LowerValue = ConstValue.PlotAxis.DefaultXMin;
                    rangeSliderX.HigherValue = ConstValue.PlotAxis.DefaultXMax;
                }

                if (rangeSliderY != null)
                {
                    rangeSliderY.Minimum = ConstValue.PlotAxis.YAxisSliderMin;
                    rangeSliderY.Maximum = ConstValue.PlotAxis.YAxisSliderMax;
                    rangeSliderY.LowerValue = ConstValue.PlotAxis.DefaultYMin;
                    rangeSliderY.HigherValue = ConstValue.PlotAxis.DefaultYMax;
                }

                // Apply initial limits to plots
                UpdateAxisLimits();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"축 범위 초기화 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnLoadRawData_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = ConstValue.FileDialogs.CsvFilter,
                FilterIndex = ConstValue.FileDialogs.DefaultFilterIndex
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Clear previous data first
                    _rawSignalData = null;
                    plotUpper.Plot.Clear();
                    plotUpper.Refresh();

                    string fileName = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    await _dataManager.LoadDataAsync(openFileDialog.FileName);
                    if (_dataManager.CurrentData != null)
                    {
                        _dataManager.CurrentData.FileName = fileName;
                        _rawSignalData = _dataManager.CurrentData;
                        UpdatePlots(_dataManager.CurrentData);
                        UpdateSliderRanges(_dataManager.CurrentData);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        string.Format(ConstValue.ErrorMessages.LoadRawDataError, ex.Message),
                        "오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private async void BtnLoadVoidData_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = ConstValue.FileDialogs.CsvFilter,
                FilterIndex = ConstValue.FileDialogs.DefaultFilterIndex
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Clear previous data first
                    _voidSignalData = null;
                    plotLower.Plot.Clear();
                    plotLower.Refresh();

                    string fileName = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    await _dataManager.LoadDataAsync(openFileDialog.FileName);
                    if (_dataManager.CurrentData != null)
                    {
                        _dataManager.CurrentData.FileName = fileName;
                        _voidSignalData = _dataManager.CurrentData;
                        UpdatePlots(_dataManager.CurrentData);
                        UpdateSliderRanges(_dataManager.CurrentData);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        string.Format(ConstValue.ErrorMessages.LoadVoidDataError, ex.Message),
                        "오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void BtnClearRawData_Click(object sender, RoutedEventArgs e)
        {
            _rawSignalData = null;
            _dataManager.ClearAlgorithmDatas(true);  // Clear raw data algorithms
            plotUpper.Plot.Clear();
            plotUpper.Refresh();
            UpdatePreviewPlot();
        }

        private void BtnClearVoidData_Click(object sender, RoutedEventArgs e)
        {
            _voidSignalData = null;
            _dataManager.ClearAlgorithmDatas(false);  // Clear void data algorithms
            plotLower.Plot.Clear();
            plotLower.Refresh();
            UpdatePreviewPlot();
        }

        private void Algorithm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var currentData = chkRawSignal.IsChecked == true ? _rawSignalData : _voidSignalData;
                if (currentData == null)
                {
                    System.Windows.MessageBox.Show("데이터를 먼저 로드해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string algorithmName = button.Content.ToString();
                ProcessData(algorithmName);
            }
        }

        private void ProcessData(string algorithmName)
        {
            try
            {
                // 현재 선택된 데이터 확인
                var currentData = chkRawSignal.IsChecked == true ? _rawSignalData : _voidSignalData;
                bool isRawData = chkRawSignal.IsChecked == true;

                if (currentData == null)
                {
                    System.Windows.MessageBox.Show("데이터를 먼저 로드해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                float[] processedData = null;
                float[] inputData = currentData.YData.ToArray();

                switch (algorithmName)
                {
                    case "FDomainFilter":
                        processedData = _signalProcessor.FDomainFilter(inputData);
                        UpdateProcessedData(algorithmName, processedData, isRawData);
                        break;
                    case "ExtractEnvelope":
                        processedData = _signalProcessor.ExtractEnvelope(inputData);
                        UpdateProcessedData(algorithmName, processedData, isRawData);
                        break;
                    case "FilterWithEnvelope":
                        processedData = _signalProcessor.FDomainFilterWithEnvelope(inputData);
                        UpdateProcessedData(algorithmName, processedData, isRawData);
                        break;
                    case "BScanNorm":
                        if (currentData.Gates == null || currentData.Gates.Count == 0)
                        {
                            System.Windows.MessageBox.Show("게이트를 먼저 설정해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        processedData = _signalProcessor.BScanNormalization(inputData, currentData.Gates[0], 0.4f);
                        UpdateProcessedData(algorithmName, processedData, isRawData);
                        break;
                    case "CScanNorm":
                        if (currentData.Gates == null || currentData.Gates.Count == 0)
                        {
                            System.Windows.MessageBox.Show("게이트를 먼저 설정해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        var normalizedValues = _signalProcessor.CScanNormalization(inputData, currentData.Gates, currentData.FirstMaxIndex);
                        processedData = normalizedValues.ToArray();
                        break;
                }

                if (processedData != null)
                {
                    UpdateProcessedData(algorithmName, processedData, isRawData);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"데이터 처리 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateProcessedData(string algorithmName, float[] processedData, bool isRawData, float[] customXAxis = null)
        {
            try
            {
                var currentData = isRawData ? _rawSignalData : _voidSignalData;

                // 인덱스 기반 X축 데이터 생성
                float[] indexAxis = new float[processedData.Length];
                for (int i = 0; i < processedData.Length; i++)
                {
                    indexAxis[i] = i;
                }

                // Create new AlgorithmDatas instance
                var algorithmData = new AlgorithmDatas
                {
                    Name = algorithmName,
                    XData = indexAxis,  // customXAxis 대신 indexAxis 사용
                    YData = processedData,
                    Gates = currentData.Gates?.ToList(),
                    FirstMaxIndex = currentData.FirstMaxIndex
                };

                // Add to DataManager
                _dataManager.AddAlgorithmData(algorithmData, isRawData);

                // Update plot
                var plot = isRawData ? plotUpper.Plot : plotLower.Plot;
                plot.Clear();

                // Plot original data with index X-axis
                float[] originalIndexAxis = new float[currentData.YData.Length];
                for (int i = 0; i < currentData.YData.Length; i++)
                {
                    originalIndexAxis[i] = i;
                }
                double[] originalX = originalIndexAxis.Select(x => (double)x).ToArray();
                double[] originalY = currentData.YData.Select(y => (double)y).ToArray();
                var originalScatter = plot.AddScatter(originalX, originalY, label: "Original");

                // Plot all algorithm results
                var algorithmDatas = _dataManager.GetAllAlgorithmDatas(isRawData);
                foreach (var algData in algorithmDatas)
                {
                    double[] algXData = algData.XData.Select(x => (double)x).ToArray();
                    double[] algYData = algData.YData.Select(y => (double)y).ToArray();
                    plot.AddScatter(algXData, algYData, label: algData.Name);
                }

                // Update X-axis label to show it's index based
                plot.XLabel("Index");

                // Show legend
                plot.Legend();

                // Store scatter plot reference
                if (isRawData)
                    rawScatter = originalScatter;
                else
                    voidScatter = originalScatter;

                // Refresh the plot
                if (isRawData)
                    plotUpper.Refresh();
                else
                    plotLower.Refresh();
                
                // Add algorithm name to the list if not already present
                if (!_appliedAlgorithms.Contains(algorithmName))
                {
                    _appliedAlgorithms.Add(algorithmName);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"처리된 데이터 업데이트 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePlots(DataModel data)
        {
            try
            {
                if (data == null) return;

                var plot = data == _rawSignalData ? plotUpper.Plot : plotLower.Plot;
                plot.Clear();

                // Set plot title using the file name without extension
                string title = string.IsNullOrEmpty(data.FileName) ? "Signal" : data.FileName;
                plot.Title(title);

                // Create index-based X-axis data
                float[] indexAxis = new float[data.YData.Length];
                for (int i = 0; i < data.YData.Length; i++)
                {
                    indexAxis[i] = i;
                }

                // Convert float arrays to double arrays for plotting
                double[] xData = indexAxis.Select(x => (double)x).ToArray();
                double[] yData = data.YData.Select(y => (double)y).ToArray();

                // Plot the main signal data
                var scatter = plot.AddScatter(
                    xData,
                    yData,
                    label: "Original"
                );

                // Plot algorithm results
                var algorithmDatas = _dataManager.GetAllAlgorithmDatas(data == _rawSignalData);
                foreach (var algData in algorithmDatas)
                {
                    // Convert algorithm data arrays to double
                    double[] algXData = algData.XData.Select(x => (double)x).ToArray();
                    double[] algYData = algData.YData.Select(y => (double)y).ToArray();

                    var algScatter = plot.AddScatter(
                        algXData,
                        algYData,
                        label: algData.Name
                    );
                }

                // Update X-axis label to show it's index based
                plot.XLabel("Index");

                // Show legend if there are algorithm results
                if (algorithmDatas.Any())
                {
                    plot.Legend();
                }

                // Store scatter plot reference
                if (data == _rawSignalData)
                    rawScatter = scatter;
                else
                    voidScatter = scatter;

                // Update axis limits
                if (data.YData.Length > 0)
                {
                    double xMin = 0;
                    double xMax = data.YData.Length - 1;
                    double yMin = data.YData.Min();
                    double yMax = data.YData.Max();

                    // Add some padding
                    double xPadding = (xMax - xMin) * 0.05;
                    double yPadding = (yMax - yMin) * 0.05;

                    plot.SetAxisLimits(
                        xMin: xMin - xPadding,
                        xMax: xMax + xPadding,
                        yMin: yMin - yPadding,
                        yMax: yMax + yPadding
                    );
                }

                // Refresh the plot
                if (data == _rawSignalData)
                    plotUpper.Refresh();
                else
                    plotLower.Refresh();

                // Update preview plot if needed
                UpdatePreviewPlot();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"플롯 업데이트 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RangeSlider_LowerValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePreviewPlot();
        }

        private void RangeSlider_HigherValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePreviewPlot();
        }

        private void RangeSlider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                UpdatePreviewPlot();
            }
        }

        private void UpdatePreviewPlot()
        {
            try
            {
                if (_dataManager.CurrentData?.Volt != null && _dataManager.CurrentData.Second != null)
                {
                    plotPreview.Plot.Clear();

                    // 이미 나노초로 변환되어 있으므로 추가 변환 불필요
                    double[] xData = _dataManager.CurrentData.Second;

                    // Plot the full data range
                    plotPreview.Plot.AddScatter(xData, _dataManager.CurrentData.Volt, System.Drawing.Color.Gray, 1, 3);

                    // Plot the selected range with semi-transparent red
                    var selectedColor = System.Drawing.Color.FromArgb(128, 255, 0, 0);
                    double xMin = rangeSliderX.LowerValue;
                    double xMax = rangeSliderX.HigherValue;
                    double yMin = rangeSliderY.LowerValue;
                    double yMax = rangeSliderY.HigherValue;

                    // Create a shaded region to show the selected area
                    var xs = new double[] { xMin, xMin, xMax, xMax };
                    var ys = new double[] { yMin, yMax, yMax, yMin };
                    plotPreview.Plot.AddPolygon(xs, ys, fillColor: selectedColor, lineWidth: 0);

                    // Set axis limits to show full data range
                    plotPreview.Plot.SetAxisLimits(
                        xMin: xData[0],
                        xMax: xData[xData.Length - 1],
                        yMin: _dataManager.CurrentData.Volt.Min(),
                        yMax: _dataManager.CurrentData.Volt.Max()
                    );

                    plotPreview.Refresh();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"미리보기 업데이트 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyScale_Click(object sender, RoutedEventArgs e)
        {
            UpdateAxisLimits();
        }

        private void UpdateAxisLimits()
        {
            try
            {
                if (plotUpper != null && plotLower != null && 
                    rangeSliderX != null && rangeSliderY != null)
                {
                    // Update X axis limits
                    double xMin = rangeSliderX.LowerValue;
                    double xMax = rangeSliderX.HigherValue;
                    
                    // Update Y axis limits
                    double yMin = rangeSliderY.LowerValue;
                    double yMax = rangeSliderY.HigherValue;

                    // Apply limits to both plots
                    plotUpper.Plot.SetAxisLimits(xMin: xMin, xMax: xMax, yMin: yMin, yMax: yMax);
                    plotLower.Plot.SetAxisLimits(xMin: xMin, xMax: xMax, yMin: yMin, yMax: yMax);

                    // Refresh plots
                    plotUpper.Refresh();
                    plotLower.Refresh();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"축 범위 업데이트 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSliderRanges(DataModel data)
        {
            if (data?.Volt != null && data.Volt.Length > 0 && data.Second != null)
            {
                // X축 범위 설정 (이미 나노초 단위)
                double xMin = data.Second[0];
                double xMax = data.Second[data.Second.Length - 1];
                
                rangeSliderX.Minimum = xMin;
                rangeSliderX.Maximum = xMax;
                rangeSliderX.LowerValue = xMin;
                rangeSliderX.HigherValue = xMax;

                // Y축 범위 설정 (전압)
                double yMin = data.Volt.Min();
                double yMax = data.Volt.Max();
                
                // Y축에 약간의 여유 추가 (5%)
                double yPadding = (yMax - yMin) * 0.05;
                yMin -= yPadding;
                yMax += yPadding;

                rangeSliderY.Minimum = yMin;
                rangeSliderY.Maximum = yMax;
                rangeSliderY.LowerValue = yMin;
                rangeSliderY.HigherValue = yMax;
            }
        }

        private void ResetScale_Click(object sender, RoutedEventArgs e)
        {
            // Reset X axis slider
            if (_dataManager.CurrentData?.Volt != null && _dataManager.CurrentData.Volt.Length > 0)
            {
                rangeSliderX.LowerValue = 0;
                rangeSliderX.HigherValue = _dataManager.CurrentData.Volt.Length - 1;

                // Reset Y axis slider to data range
                double yMin = _dataManager.CurrentData.Volt.Min();
                double yMax = _dataManager.CurrentData.Volt.Max();
                
                // Add some padding to the Y range (10%)
                double padding = (yMax - yMin) * 0.1;
                yMin -= padding;
                yMax += padding;

                rangeSliderY.LowerValue = yMin;
                rangeSliderY.HigherValue = yMax;
            }
            else
            {
                rangeSliderX.LowerValue = ConstValue.PlotAxis.DefaultXMin;
                rangeSliderX.HigherValue = ConstValue.PlotAxis.DefaultXMax;
                rangeSliderY.LowerValue = ConstValue.PlotAxis.DefaultYMin;
                rangeSliderY.HigherValue = ConstValue.PlotAxis.DefaultYMax;
            }

            // Update preview
            UpdatePreviewPlot();
            // Apply the reset limits
            UpdateAxisLimits();
        }

        private void Plot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is WpfPlot plot)
            {
                // 클릭된 그래프의 데이터 가져오기
                DataModel selectedData = null;
                if (plot == plotUpper && _rawSignalData != null)
                {
                    selectedData = _rawSignalData;
                }
                else if (plot == plotLower && _voidSignalData != null)
                {
                    selectedData = _voidSignalData;
                }

                // 데이터가 있는 경우에만 업데이트
                if (selectedData?.Volt != null && selectedData.Volt.Length > 0)
                {
                    UpdateSliderRanges(selectedData);
                    _dataManager.SetCurrentData(selectedData);
                    UpdatePreviewPlot();
                }
            }
        }

        #region Parameter Validation
        private (bool isValid, double middleCutOff, double sideCutOff, float samplingRate) ValidateAndGetParameters()
        {
            try
            {
                // Middle Cut-off Ratio 검증
                if (!double.TryParse(txtMiddleCutOffRatio.Text, out double middleCutOff) || 
                    middleCutOff < 0 || middleCutOff > 1)
                {
                    System.Windows.MessageBox.Show(
                        "Middle Cut-off Ratio는 0과 1 사이의 값이어야 합니다.",
                        "파라미터 오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return (false, 0, 0, 0);
                }

                // Side Cut-off Ratio 검증
                if (!double.TryParse(txtSideCutoffRatio.Text, out double sideCutOff) || 
                    sideCutOff < 0 || sideCutOff > 1)
                {
                    System.Windows.MessageBox.Show(
                        "Side Cut-off Ratio는 0과 1 사이의 값이어야 합니다.",
                        "파라미터 오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return (false, 0, 0, 0);
                }

                // Sampling Rate 검증
                if (!float.TryParse(txtSamplingRate.Text, out float samplingRate) || 
                    samplingRate <= 0)
                {
                    System.Windows.MessageBox.Show(
                        "Sampling Rate는 0보다 큰 값이어야 합니다.",
                        "파라미터 오류",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return (false, 0, 0, 0);
                }

                return (true, middleCutOff, sideCutOff, samplingRate);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show(
                    "파라미터 값을 확인해주세요.",
                    "파라미터 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return (false, 0, 0, 0);
            }
        }
        #endregion

        #region FFT Algorithm Button Events
        private void BtnPerformFFT_Click(object sender, RoutedEventArgs e)
        {
            var (isValid, _, _, samplingRate) = ValidateAndGetParameters();
            if (!isValid) return;

            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                var (magnitudeData, frequencyAxis) = _signalProcessor.PerformFFT(_rawSignalData.YData, samplingRate);
                UpdateProcessedData("FFT", magnitudeData, true, frequencyAxis);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                var (magnitudeData, frequencyAxis) = _signalProcessor.PerformFFT(_voidSignalData.YData, samplingRate);
                UpdateProcessedData("FFT", magnitudeData, false, frequencyAxis);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnApplyFrequencyFilter_Click(object sender, RoutedEventArgs e)
        {
            var (isValid, middleCutOff, sideCutOff, samplingRate) = ValidateAndGetParameters();
            if (!isValid) return;

            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                var (magnitudeData, frequencyAxis) = _signalProcessor.ApplyFrequencyFilter(
                    _rawSignalData.YData, middleCutOff, sideCutOff, samplingRate);
                UpdateProcessedData("Frequency Filter", magnitudeData, true, frequencyAxis);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                var (magnitudeData, frequencyAxis) = _signalProcessor.ApplyFrequencyFilter(
                    _voidSignalData.YData, middleCutOff, sideCutOff, samplingRate);
                UpdateProcessedData("Frequency Filter", magnitudeData, false, frequencyAxis);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnPerformIFFT_Click(object sender, RoutedEventArgs e)
        {
            var (isValid, middleCutOff, sideCutOff, samplingRate) = ValidateAndGetParameters();
            if (!isValid) return;

            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                var (timeData, timeAxis) = _signalProcessor.PerformIFFT(
                    _rawSignalData.YData, middleCutOff, sideCutOff, samplingRate);
                UpdateProcessedData("IFFT", timeData, true, timeAxis);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                var (timeData, timeAxis) = _signalProcessor.PerformIFFT(
                    _voidSignalData.YData, middleCutOff, sideCutOff, samplingRate);
                UpdateProcessedData("IFFT", timeData, false, timeAxis);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnFDomainFilter_Click(object sender, RoutedEventArgs e)
        {
            var (isValid, middleCutOff, sideCutOff, _) = ValidateAndGetParameters();
            if (!isValid) return;

            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                float[] processedData = _signalProcessor.FDomainFilter(_rawSignalData.YData, middleCutOff, sideCutOff);
                UpdateProcessedData("FDomain Filter", processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] processedData = _signalProcessor.FDomainFilter(_voidSignalData.YData, middleCutOff, sideCutOff);
                UpdateProcessedData("FDomain Filter", processedData, false);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region Other Algorithm Button Events
        private void BtnExtractEnvelope_Click(object sender, RoutedEventArgs e)
        {
            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                float[] processedData = _signalProcessor.ExtractEnvelope(_rawSignalData.YData);
                UpdateProcessedData("Envelope", processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] processedData = _signalProcessor.ExtractEnvelope(_voidSignalData.YData);
                UpdateProcessedData("Envelope", processedData, false);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnFilterWithEnvelope_Click(object sender, RoutedEventArgs e)
        {
            var (isValid, middleCutOff, sideCutOff, _) = ValidateAndGetParameters();
            if (!isValid) return;

            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                float[] processedData = _signalProcessor.FDomainFilterWithEnvelope(_rawSignalData.YData, middleCutOff, sideCutOff);
                UpdateProcessedData("Filter+Envelope", processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] processedData = _signalProcessor.FDomainFilterWithEnvelope(_voidSignalData.YData, middleCutOff, sideCutOff);
                UpdateProcessedData("Filter+Envelope", processedData, false);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnBScanNorm_Click(object sender, RoutedEventArgs e)
        {
            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                if (_rawSignalData.Gates == null || _rawSignalData.Gates.Count == 0)
                {
                    System.Windows.MessageBox.Show("Raw Signal의 게이트를 먼저 설정해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    float[] processedData = _signalProcessor.BScanNormalization(
                        _rawSignalData.YData,
                        _rawSignalData.Gates[0],
                        0.5f
                    );
                    UpdateProcessedData("B-Scan Norm", processedData, true);
                    processedAny = true;
                }
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                if (_voidSignalData.Gates == null || _voidSignalData.Gates.Count == 0)
                {
                    System.Windows.MessageBox.Show("Void Signal의 게이트를 먼저 설정해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    float[] processedData = _signalProcessor.BScanNormalization(
                        _voidSignalData.YData,
                        _voidSignalData.Gates[0],
                        0.5f
                    );
                    UpdateProcessedData("B-Scan Norm", processedData, false);
                    processedAny = true;
                }
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnCScanNorm_Click(object sender, RoutedEventArgs e)
        {
            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                if (_rawSignalData.Gates == null || _rawSignalData.Gates.Count == 0)
                {
                    System.Windows.MessageBox.Show("Raw Signal의 게이트를 먼저 설정해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    float[] processedData = _signalProcessor.CScanNormalization(
                        _rawSignalData.YData,
                        _rawSignalData.Gates,
                        _rawSignalData.FirstMaxIndex
                    ).ToArray();
                    UpdateProcessedData("C-Scan Norm", processedData, true);
                    processedAny = true;
                }
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                if (_voidSignalData.Gates == null || _voidSignalData.Gates.Count == 0)
                {
                    System.Windows.MessageBox.Show("Void Signal의 게이트를 먼저 설정해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    float[] processedData = _signalProcessor.CScanNormalization(
                        _voidSignalData.YData,
                        _voidSignalData.Gates,
                        _voidSignalData.FirstMaxIndex
                    ).ToArray();
                    UpdateProcessedData("C-Scan Norm", processedData, false);
                    processedAny = true;
                }
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion
    }
}


