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
        private readonly SignalProcessor _signalProcessor;
        private DataModel _rawSignalData;
        private DataModel _voidSignalData;
        private ObservableCollection<string> _appliedAlgorithms;
        private ScatterPlot rawScatter;
        private ScatterPlot voidScatter;

        public MainWindow()
        {
            InitializeComponent();
            _dataManager = DataManager.Instance;
            _signalProcessor = new SignalProcessor();

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
            plotUpper.Plot.Clear();
            plotUpper.Plot.Title("Raw Signal");
            plotUpper.Refresh();
        }

        private void BtnClearVoidData_Click(object sender, RoutedEventArgs e)
        {
            _voidSignalData = null;
            plotLower.Plot.Clear();
            plotLower.Plot.Title("Void Signal");
            plotLower.Refresh();
        }

        private void Algorithm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string algorithmName = button.Content.ToString();
                if (_dataManager.CurrentData != null)
                {
                    _appliedAlgorithms.Add(algorithmName);
                    ProcessData(algorithmName);
                }
                else
                {
                    System.Windows.MessageBox.Show("데이터를 먼저 로드해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ProcessData(string algorithmName)
        {
            try
            {
                if (_rawSignalData == null)
                {
                    System.Windows.MessageBox.Show("Raw 데이터를 먼저 로드해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                float[] processedData = null;
                float[] inputData = _rawSignalData.YData.ToArray();

                switch (algorithmName)
                {
                    case "FDomainFilter":
                        processedData = _signalProcessor.FDomainFilter(inputData);
                        break;
                    case "ExtractEnvelope":
                        processedData = _signalProcessor.ExtractEnvelope(inputData);
                        break;
                    case "FilterWithEnvelope":
                        processedData = _signalProcessor.FDomainFilterWithEnvelope(inputData);
                        break;
                    case "BScanNorm":
                        if (_rawSignalData.Gates == null || _rawSignalData.Gates.Count == 0)
                        {
                            System.Windows.MessageBox.Show("게이트를 먼저 설정해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        processedData = _signalProcessor.BScanNormalization(inputData, _rawSignalData.Gates[0], 0.4f);
                        break;
                    case "CScanNorm":
                        if (_rawSignalData.Gates == null || _rawSignalData.Gates.Count == 0)
                        {
                            System.Windows.MessageBox.Show("게이트를 먼저 설정해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        var normalizedValues = _signalProcessor.CScanNormalization(inputData, _rawSignalData.Gates, _rawSignalData.FirstMaxIndex);
                        processedData = normalizedValues.ToArray();
                        break;
                }

                if (processedData != null)
                {
                    UpdateProcessedData(algorithmName, processedData);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"데이터 처리 중 오류 발생: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateProcessedData(string algorithmName, float[] processedData)
        {
            try
            {
                // Create new AlgorithmDatas instance
                var algorithmData = new AlgorithmDatas
                {
                    Name = algorithmName,
                    XData = _rawSignalData.XData.ToArray(),
                    YData = processedData,
                    Gates = _rawSignalData.Gates?.ToList(),
                    FirstMaxIndex = _rawSignalData.FirstMaxIndex
                };

                // Add to DataManager
                _dataManager.AddAlgorithmData(algorithmData);

                // Update raw signal data with processed data
                _rawSignalData.YData = processedData;
                
                // Update plot
                UpdatePlots(_rawSignalData);
                
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

                // Convert float arrays to double arrays for plotting
                double[] xData = data.XData.Select(x => (double)x).ToArray();
                double[] yData = data.YData.Select(y => (double)y).ToArray();

                // Plot the main signal data
                var scatter = plot.AddScatter(
                    xData,
                    yData,
                    label: "Original"
                );

                // If this is raw signal data, also plot algorithm results
                if (data == _rawSignalData)
                {
                    var algorithmDatas = _dataManager.GetAllAlgorithmDatas();
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

                    // Show legend if there are algorithm results
                    if (algorithmDatas.Any())
                    {
                        plot.Legend();
                    }
                }

                // Store scatter plot reference
                if (data == _rawSignalData)
                    rawScatter = scatter;
                else
                    voidScatter = scatter;

                // Update axis limits
                if (data.XData.Length > 0 && data.YData.Length > 0)
                {
                    double xMin = data.XData.Min();
                    double xMax = data.XData.Max();
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

                    // Convert seconds to nanoseconds for display
                    double[] xData = _dataManager.CurrentData.Second
                        .Select(s => s * ConstValue.TimeUnit.SecondToNanosecond).ToArray();

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
                // X축 범위 설정 (시간 - 나노초 단위)
                double xMin = data.Second[0] * ConstValue.TimeUnit.SecondToNanosecond;
                double xMax = data.Second[data.Second.Length - 1] * ConstValue.TimeUnit.SecondToNanosecond;
                
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

        #region Algorithm Button Events
        private void BtnFDomainFilter_Click(object sender, RoutedEventArgs e)
        {
            if (chkRawSignal.IsChecked == true && _rawSignalData?.Volt != null)
            {
                float[] processedData = _signalProcessor.FDomainFilter(_signalProcessor.ConvertToFloat(_rawSignalData.Volt));
                UpdateProcessedData("FDomain Filter", processedData);
            }
        }

        private void BtnExtractEnvelope_Click(object sender, RoutedEventArgs e)
        {
            if (chkRawSignal.IsChecked == true && _rawSignalData?.Volt != null)
            {
                float[] processedData = _signalProcessor.ExtractEnvelope(_signalProcessor.ConvertToFloat(_rawSignalData.Volt));
                UpdateProcessedData("Envelope", processedData);
            }
        }

        private void BtnFilterWithEnvelope_Click(object sender, RoutedEventArgs e)
        {
            if (chkRawSignal.IsChecked == true && _rawSignalData?.Volt != null)
            {
                float[] processedData = _signalProcessor.FDomainFilterWithEnvelope(_signalProcessor.ConvertToFloat(_rawSignalData.Volt));
                UpdateProcessedData("Filter+Envelope", processedData);
            }
        }

        private void BtnBScanNorm_Click(object sender, RoutedEventArgs e)
        {
            if (chkRawSignal.IsChecked == true && _rawSignalData?.Volt != null)
            {
                // Create a gate for the entire signal
                var gate = new SignalProcessor.Gate(0, _rawSignalData.Volt.Length - 1);
                float[] processedData = _signalProcessor.BScanNormalization(
                    _signalProcessor.ConvertToFloat(_rawSignalData.Volt),
                    gate,
                    0.5f  // threshold ratio
                );
                UpdateProcessedData("B-Scan Norm", processedData);
            }
        }

        private void BtnCScanNorm_Click(object sender, RoutedEventArgs e)
        {
            if (chkRawSignal.IsChecked == true && _rawSignalData?.Volt != null)
            {
                // Create sample gates (you might want to make these configurable)
                var gates = new List<SignalProcessor.Gate>
                {
                    new SignalProcessor.Gate(0, _rawSignalData.Volt.Length / 3),
                    new SignalProcessor.Gate(_rawSignalData.Volt.Length / 3, 2 * _rawSignalData.Volt.Length / 3),
                    new SignalProcessor.Gate(2 * _rawSignalData.Volt.Length / 3, _rawSignalData.Volt.Length - 1)
                };

                List<float> normalizedValues = _signalProcessor.CScanNormalization(
                    _signalProcessor.ConvertToFloat(_rawSignalData.Volt),
                    gates,
                    0  // origin first max index
                );

                // Convert normalized values to a signal
                float[] processedData = new float[_rawSignalData.Volt.Length];
                for (int i = 0; i < normalizedValues.Count; i++)
                {
                    int start = gates[i].StartIndex;
                    int end = gates[i].EndIndex;
                    for (int j = start; j <= end; j++)
                    {
                        processedData[j] = normalizedValues[i];
                    }
                }

                UpdateProcessedData("C-Scan Norm", processedData);
            }
        }
        #endregion
    }
}

