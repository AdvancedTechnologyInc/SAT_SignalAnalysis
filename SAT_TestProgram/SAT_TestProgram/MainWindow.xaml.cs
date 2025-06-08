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
using System.Numerics;

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
                plotUpper.Plot.XLabel("Index");
                plotUpper.Plot.YLabel("Voltage");
                plotUpper.Refresh();

                plotLower.Plot.Title("Void Signal");
                plotLower.Plot.XLabel("Index");
                plotLower.Plot.YLabel("Voltage");
                plotLower.Refresh();

                plotPreview.Plot.Title("Preview");
                plotPreview.Plot.XLabel("Index");
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
                            System.Windows.MessageBox.Show("게이트가 설정되지 않았습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        processedData = _signalProcessor.BScanNormalization(inputData, currentData.Gates[0], 0.4f);
                        UpdateProcessedData(algorithmName, processedData, isRawData);
                        break;
                    case "CScanNorm":
                        if (currentData.Gates == null || currentData.Gates.Count == 0)
                        {
                            System.Windows.MessageBox.Show("게이트가 설정되지 않았습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // CScan 정규화 수행
                        var normalizedValues = _signalProcessor.CScanNormalization(inputData, currentData.Gates, currentData.FirstMaxIndex);
                        processedData = normalizedValues.ToArray();
                        UpdateProcessedData(algorithmName, processedData, isRawData);
                        break;
                    case "Gaussian Filter":
                        processedData = _signalProcessor.ApplyGaussianFilter(inputData, sigma: 2.0, kernelSize: 7);
                        UpdateProcessedData(algorithmName, processedData, isRawData);
                        break;
                    case "Unsharp Masking":
                        processedData = _signalProcessor.ApplyUnsharpMasking(inputData, amount: 1.5f, sigma: 1.0, threshold: 0.1f);
                        UpdateProcessedData(algorithmName, processedData, isRawData);
                        break;
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
                if (_dataManager.CurrentData?.Volt != null)
                {
                    plotPreview.Plot.Clear();

                    // 인덱스 기반 X축 데이터 생성
                    double[] xData = Enumerable.Range(0, _dataManager.CurrentData.Volt.Length)
                                             .Select(i => (double)i)
                                             .ToArray();

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
                        xMin: 0,
                        xMax: xData.Length - 1,
                        yMin: _dataManager.CurrentData.Volt.Min(),
                        yMax: _dataManager.CurrentData.Volt.Max()
                    );

                    // Update X-axis label to show it's index based
                    plotPreview.Plot.XLabel("Index");

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
            if (data?.Volt != null && data.Volt.Length > 0)
            {
                // X축 범위를 인덱스 기반으로 설정
                double xMin = 0;
                double xMax = data.Volt.Length - 1;
                
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

        private (bool isValid, float middleCutOff, float sideCutOff, float samplingRate) ValidateAndGetParameters()
        {
            try
            {
                // Middle Cut-off Ratio 검증
                if (!float.TryParse(txtMiddleCutOffRatio.Text, out float middleCutOff) ||
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
                if (!float.TryParse(txtSideCutoffRatio.Text, out float sideCutOff) ||
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
        //private (bool isValid, float middleCutOff, float sideCutOff, float samplingRate) ValidateAndGetParameters()
        //{
        //    float middleCutOff = 0f;
        //    float sideCutOff = 0f;
        //    float samplingRate = 100f;

        //    try
        //    {
        //        // Validate and parse middle cut-off frequency
        //        if (!float.TryParse(txtMiddleCutOffRatio.Text, out middleCutOff))
        //        {
        //            System.Windows.MessageBox.Show("중간 차단 주파수가 올바른 숫자 형식이 아닙니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return (false, 0f, 0f, 100f);
        //        }

        //        // Validate and parse side cut-off frequency
        //        if (!float.TryParse(txtSideCutoffRatio.Text, out sideCutOff))
        //        {
        //            System.Windows.MessageBox.Show("측면 차단 주파수가 올바른 숫자 형식이 아닙니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return (false, 0f, 0f, 100f);
        //        }

        //        // Validate and parse sampling rate
        //        if (!float.TryParse(txtSamplingRate.Text, out samplingRate))
        //        {
        //            System.Windows.MessageBox.Show("샘플링 레이트가 올바른 숫자 형식이 아닙니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return (false, 0f, 0f, 100f);
        //        }

        //        // Additional validation
        //        if (middleCutOff <= 0f)
        //        {
        //            System.Windows.MessageBox.Show("중간 차단 주파수는 0보다 커야 합니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return (false, 0f, 0f, 100f);
        //        }

        //        if (sideCutOff <= 0f)
        //        {
        //            System.Windows.MessageBox.Show("측면 차단 주파수는 0보다 커야 합니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return (false, 0f, 0f, 100f);
        //        }

        //        if (samplingRate <= 0f)
        //        {
        //            System.Windows.MessageBox.Show("샘플링 레이트는 0보다 커야 합니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return (false, 0f, 0f, 100f);
        //        }

        //        // Convert ratio to frequency
        //        middleCutOff = middleCutOff * samplingRate / 2f;
        //        sideCutOff = sideCutOff * samplingRate / 2f;

        //        return (true, middleCutOff, sideCutOff, samplingRate);
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Windows.MessageBox.Show($"매개변수 검증 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return (false, 0f, 0f, 100f);
        //    }
        //}
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
                float[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedData(true) : _rawSignalData.YData;

                var (magnitudeData, frequencyAxis, complexData) = _signalProcessor.PerformFFT(inputData, samplingRate);
                UpdateProcessedData("FFT", magnitudeData, true, frequencyAxis);
                
                // Store Complex data in the latest algorithm data
                var latestData = _dataManager.GetAllAlgorithmDatas(true).LastOrDefault();
                if (latestData != null)
                {
                    latestData.ComplexData = complexData;
                }
                
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedData(false) : _voidSignalData.YData;

                var (magnitudeData, frequencyAxis, complexData) = _signalProcessor.PerformFFT(inputData, samplingRate);
                UpdateProcessedData("FFT", magnitudeData, false, frequencyAxis);
                
                // Store Complex data in the latest algorithm data
                var latestData = _dataManager.GetAllAlgorithmDatas(false).LastOrDefault();
                if (latestData != null)
                {
                    latestData.ComplexData = complexData;
                }
                
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
                float[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedData(true) : _rawSignalData.YData;

                float[] processedData = _signalProcessor.FDomainFilter(inputData, middleCutOff, sideCutOff);
                UpdateProcessedData("FDomain Filter", processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedData(false) : _voidSignalData.YData;

                float[] processedData = _signalProcessor.FDomainFilter(inputData, middleCutOff, sideCutOff);
                UpdateProcessedData("FDomain Filter", processedData, false);
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
                Complex[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedComplexData(true) : 
                    _rawSignalData.YData.Select(y => new Complex(y, 0)).ToArray();

                var (magnitudeData, frequencyAxis, complexData) = _signalProcessor.ApplyFrequencyFilter(inputData, middleCutOff, sideCutOff, samplingRate);
                UpdateProcessedData("Frequency Filter", magnitudeData, true, frequencyAxis);
                
                // Store Complex data in the latest algorithm data
                var latestData = _dataManager.GetAllAlgorithmDatas(true).LastOrDefault();
                if (latestData != null)
                {
                    latestData.ComplexData = complexData;
                }
                
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                Complex[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedComplexData(false) : 
                    _voidSignalData.YData.Select(y => new Complex(y, 0)).ToArray();

                var (magnitudeData, frequencyAxis, complexData) = _signalProcessor.ApplyFrequencyFilter(inputData, middleCutOff, sideCutOff, samplingRate);
                UpdateProcessedData("Frequency Filter", magnitudeData, false, frequencyAxis);
                
                // Store Complex data in the latest algorithm data
                var latestData = _dataManager.GetAllAlgorithmDatas(false).LastOrDefault();
                if (latestData != null)
                {
                    latestData.ComplexData = complexData;
                }
                
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
            var (isValid, _, _, samplingRate) = ValidateAndGetParameters();
            if (!isValid) return;

            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                Complex[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedComplexData(true) : 
                    _rawSignalData.YData.Select(y => new Complex(y, 0)).ToArray();

                var (timeData, timeAxis, complexData) = _signalProcessor.PerformIFFT(inputData, samplingRate);
                UpdateProcessedData("IFFT", timeData, true, timeAxis);
                
                // Store Complex data in the latest algorithm data
                var latestData = _dataManager.GetAllAlgorithmDatas(true).LastOrDefault();
                if (latestData != null)
                {
                    latestData.ComplexData = complexData;
                }
                
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                Complex[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedComplexData(false) : 
                    _voidSignalData.YData.Select(y => new Complex(y, 0)).ToArray();

                var (timeData, timeAxis, complexData) = _signalProcessor.PerformIFFT(inputData, samplingRate);
                UpdateProcessedData("IFFT", timeData, false, timeAxis);
                
                // Store Complex data in the latest algorithm data
                var latestData = _dataManager.GetAllAlgorithmDatas(false).LastOrDefault();
                if (latestData != null)
                {
                    latestData.ComplexData = complexData;
                }
                
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private float[] GetLatestProcessedData(bool isUpperSignal)
        {
            var algorithmDatas = _dataManager.GetAllAlgorithmDatas(isUpperSignal);
            if (algorithmDatas != null && algorithmDatas.Any())
                return algorithmDatas.Last().YData;

            // If only YData exists, return it
            return isUpperSignal ? _rawSignalData.YData : _voidSignalData.YData;
        }

        private Complex[] GetLatestProcessedComplexData(bool isUpperSignal)
        {
            var algorithmDatas = _dataManager.GetAllAlgorithmDatas(isUpperSignal);
            if (algorithmDatas == null || !algorithmDatas.Any())
                return null;

            var latestData = algorithmDatas.LastOrDefault();
            if (latestData == null)
                return null;

            // If ComplexData exists, return it directly
            if (latestData.ComplexData != null && latestData.ComplexData.Length > 0)
            {
                return latestData.ComplexData;
            }

            // If only YData exists, create new Complex array with zero imaginary parts
            if (latestData.YData != null)
            {
                int n = latestData.YData.Length;
                Complex[] complexSignal = new Complex[n];
                for (int i = 0; i < n; i++)
                {
                    complexSignal[i] = new Complex(latestData.YData[i], 0);
                }
                return complexSignal;
            }

            return null;
        }
        #endregion

        #region Other Algorithm Button Events
        private void BtnExtractEnvelope_Click(object sender, RoutedEventArgs e)
        {
            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedData(true) : _rawSignalData.YData;

                float[] processedData = _signalProcessor.ExtractEnvelope(inputData);
                UpdateProcessedData("Envelope", processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedData(false) : _voidSignalData.YData;

                float[] processedData = _signalProcessor.ExtractEnvelope(inputData);
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
                float[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedData(true) : _rawSignalData.YData;

                float[] processedData = _signalProcessor.FDomainFilterWithEnvelope(inputData, middleCutOff, sideCutOff);
                UpdateProcessedData("Filter+Envelope", processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedData(false) : _voidSignalData.YData;

                float[] processedData = _signalProcessor.FDomainFilterWithEnvelope(inputData, middleCutOff, sideCutOff);
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

        private void BtnGaussianFilter_Click(object sender, RoutedEventArgs e)
        {
            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedData(false) : _rawSignalData.YData;

                float[] processedData = _signalProcessor.ApplyGaussianFilter(inputData, sigma: 2.0, kernelSize: 7);
                UpdateProcessedData("Gaussian Filter", processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedData(true) : _voidSignalData.YData;

                float[] processedData = _signalProcessor.ApplyGaussianFilter(inputData, sigma: 2.0, kernelSize: 7);
                UpdateProcessedData("Gaussian Filter", processedData, false);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnUnsharpMasking_Click(object sender, RoutedEventArgs e)
        {
            bool processedAny = false;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedData(false) : _rawSignalData.YData;

                float[] processedData = _signalProcessor.ApplyUnsharpMasking(inputData, amount: 1.5f, sigma: 1.0, threshold: 0.1f);
                UpdateProcessedData("Unsharp Masking", processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedData(true) : _voidSignalData.YData;

                float[] processedData = _signalProcessor.ApplyUnsharpMasking(inputData, amount: 1.5f, sigma: 1.0, threshold: 0.1f);
                UpdateProcessedData("Unsharp Masking", processedData, false);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnZeroOffset_Click(object sender, RoutedEventArgs e)
        {
            bool processedAny = false;
            bool applyAbsolute = chkAbsoluteValue.IsChecked == true;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedData(false) : _rawSignalData.YData;

                float[] processedData = _signalProcessor.ApplyZeroOffset(inputData, applyAbsolute);
                string algorithmName = applyAbsolute ? "Zero Offset (Absolute)" : "Zero Offset";
                UpdateProcessedData(algorithmName, processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedData(true) : _voidSignalData.YData;

                float[] processedData = _signalProcessor.ApplyZeroOffset(inputData, applyAbsolute);
                string algorithmName = applyAbsolute ? "Zero Offset (Absolute)" : "Zero Offset";
                UpdateProcessedData(algorithmName, processedData, false);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnThresholdFilter_Click(object sender, RoutedEventArgs e)
        {
            // Threshold 값 검증
            if (!float.TryParse(txtThresholdValue.Text, out float threshold))
            {
                System.Windows.MessageBox.Show(
                    "Threshold 값이 올바르지 않습니다. 숫자를 입력해주세요.",
                    "파라미터 오류",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool processedAny = false;
            bool useAbsoluteValue = chkThresholdAbsolute.IsChecked == true;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedData(true) : _rawSignalData.YData;

                float[] processedData = _signalProcessor.ApplyThresholdFilter(inputData, threshold, useAbsoluteValue);
                string algorithmName = useAbsoluteValue ? 
                    $"Threshold Filter (Abs > {threshold})" : 
                    $"Threshold Filter (> {threshold})";
                UpdateProcessedData(algorithmName, processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedData(true) : _voidSignalData.YData;

                float[] processedData = _signalProcessor.ApplyThresholdFilter(inputData, threshold, useAbsoluteValue);
                string algorithmName = useAbsoluteValue ? 
                    $"Threshold Filter (Abs > {threshold})" : 
                    $"Threshold Filter (> {threshold})";
                UpdateProcessedData(algorithmName, processedData, true);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnHilbertTransform_Click(object sender, RoutedEventArgs e)
        {
            bool processedAny = false;
            bool returnEnvelope = chkHilbertEnvelope.IsChecked == true;

            // Raw Signal 처리
            if (chkRawSignal.IsChecked == true && _rawSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingUpper.IsChecked == true ? 
                    GetLatestProcessedData(true) : _rawSignalData.YData;

                float[] processedData = _signalProcessor.ApplyHilbertTransform(inputData, returnEnvelope);
                string algorithmName = returnEnvelope ? 
                    "Hilbert Transform (Envelope)" : 
                    "Hilbert Transform (Phase)";
                UpdateProcessedData(algorithmName, processedData, true);
                processedAny = true;
            }

            // Void Signal 처리
            if (chkProcessedSignal.IsChecked == true && _voidSignalData?.YData != null)
            {
                float[] inputData = chkContinuousProcessingLower.IsChecked == true ? 
                    GetLatestProcessedData(true) : _voidSignalData.YData;

                float[] processedData = _signalProcessor.ApplyHilbertTransform(inputData, returnEnvelope);
                string algorithmName = returnEnvelope ? 
                    "Hilbert Transform (Envelope)" : 
                    "Hilbert Transform (Phase)";
                UpdateProcessedData(algorithmName, processedData, true);
                processedAny = true;
            }

            // 둘 다 처리되지 않은 경우
            if (!processedAny)
            {
                System.Windows.MessageBox.Show("처리할 데이터가 없습니다. 데이터를 로드하고 체크박스를 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}


