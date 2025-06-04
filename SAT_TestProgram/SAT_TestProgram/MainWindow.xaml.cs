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
using System.Windows.Threading;
using ScottPlot;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using ScottPlot.Plottable;
using Xceed.Wpf.Toolkit;

namespace SAT_TestProgram
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DataManager _dataManager;
        private DataModel _rawSignalData;
        private DataModel _voidSignalData;
        private ObservableCollection<string> _appliedAlgorithms;
        private ScatterPlot rawScatter;
        private ScatterPlot voidScatter;

        public MainWindow()
        {
            InitializeComponent();
            _dataManager = DataManager.Instance;

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
            if (_dataManager.CurrentData == null) return;

            var currentData = _dataManager.CurrentData;
            double[] processedData = null;

            // 알고리즘에 따른 데이터 처리 로직
            switch (algorithmName)
            {
                case "Algorithm 1":
                    processedData = currentData.Volt.Select(x => x * ConstValue.AlgorithmFactors.Algorithm1Factor).ToArray();
                    break;
                case "Algorithm 2":
                    processedData = currentData.Volt.Select(x => x * ConstValue.AlgorithmFactors.Algorithm2Factor).ToArray();
                    break;
                case "Algorithm 3":
                    processedData = currentData.Volt.Select(x => x + ConstValue.AlgorithmFactors.Algorithm3Offset).ToArray();
                    break;
                case "Algorithm 4":
                    processedData = currentData.Volt.Select(x => x + ConstValue.AlgorithmFactors.Algorithm4Offset).ToArray();
                    break;
                case "Algorithm 5":
                    processedData = currentData.Volt.Select(x => x * x).ToArray();
                    break;
            }

            if (processedData != null)
            {
                var processedModel = new DataModel
                {
                    DataNum = currentData.DataNum,
                    DataIndex = (int[])currentData.DataIndex.Clone(),
                    Second = (double[])currentData.Second.Clone(),
                    Volt = processedData
                };

                if (processedModel.DataIndex.Length > 0)
                {
                    _dataManager.UpdateProcessedData(processedModel.DataIndex[0], algorithmName, processedData);
                }
                
                _voidSignalData = processedModel;
                UpdatePlots(processedModel);
            }
        }

        private void UpdatePlots(DataModel data)
        {
            if (plotUpper != null)
            {
                // 상단 그래프 업데이트 (Raw Signal)
                plotUpper.Plot.Clear();
                if (chkRawSignal.IsChecked == true && _rawSignalData != null)
                {
                    if (_rawSignalData.Volt != null && _rawSignalData.Volt.Length > 0 && _rawSignalData.Second != null)
                    {
                        // Convert seconds to nanoseconds only for display
                        double[] dataX = _rawSignalData.Second.Select(s => s * ConstValue.TimeUnit.SecondToNanosecond).ToArray();
                        rawScatter = plotUpper.Plot.AddScatter(dataX, _rawSignalData.Volt);
                        
                        // Set axis limits based on actual time range
                        double xMin = _rawSignalData.Second[0] * ConstValue.TimeUnit.SecondToNanosecond;
                        double xMax = _rawSignalData.Second[_rawSignalData.Second.Length - 1] * ConstValue.TimeUnit.SecondToNanosecond;
                        double yMin = _rawSignalData.Volt.Min();
                        double yMax = _rawSignalData.Volt.Max();

                        // Add some padding for y-axis only (5% of the range)
                        double yPadding = (yMax - yMin) * 0.05;

                        plotUpper.Plot.SetAxisLimits(
                            xMin: xMin,
                            xMax: xMax,
                            yMin: yMin - yPadding,
                            yMax: yMax + yPadding
                        );
                        
                        // Update plot title with file name
                        if (!string.IsNullOrEmpty(_rawSignalData.FileName))
                        {
                            plotUpper.Plot.Title(_rawSignalData.FileName);
                        }
                    }
                }
                plotUpper.Refresh();
            }

            // Check if void signal data exists
            if (_voidSignalData?.Volt != null && _voidSignalData.Volt.Length > 0)
            {
                // 하단 그래프 업데이트 (Void Signal)
                plotLower.Plot.Clear();
                if (chkProcessedSignal.IsChecked == true)
                {
                    if (_voidSignalData.Second != null)
                    {
                        // Convert seconds to nanoseconds only for display
                        double[] dataX = _voidSignalData.Second.Select(s => s * ConstValue.TimeUnit.SecondToNanosecond).ToArray();
                        voidScatter = plotLower.Plot.AddScatter(dataX, _voidSignalData.Volt);
                        
                        // Set axis limits based on actual time range
                        double xMin = _voidSignalData.Second[0] * ConstValue.TimeUnit.SecondToNanosecond;
                        double xMax = _voidSignalData.Second[_voidSignalData.Second.Length - 1] * ConstValue.TimeUnit.SecondToNanosecond;
                        double yMin = _voidSignalData.Volt.Min();
                        double yMax = _voidSignalData.Volt.Max();

                        // Add some padding for y-axis only (5% of the range)
                        double yPadding = (yMax - yMin) * 0.05;

                        plotLower.Plot.SetAxisLimits(
                            xMin: xMin,
                            xMax: xMax,
                            yMin: yMin - yPadding,
                            yMax: yMax + yPadding
                        );
                        
                        // Update plot title with file name
                        if (!string.IsNullOrEmpty(_voidSignalData.FileName))
                        {
                            plotLower.Plot.Title(_voidSignalData.FileName);
                        }
                    }
                }
                plotLower.Refresh();
            }

            // Update preview plot if data exists
            if (data?.Volt != null && data.Volt.Length > 0)
            {
                UpdatePreviewPlot();
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
    }
}

