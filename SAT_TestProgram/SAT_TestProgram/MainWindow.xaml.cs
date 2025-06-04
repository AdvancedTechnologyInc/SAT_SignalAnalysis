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
        private ScatterPlot rawScatter;
        private ScatterPlot voidScatter;
        private ObservableCollection<DataModel> _rawSignalList;
        private ObservableCollection<DataModel> _voidSignalList;
        private ObservableCollection<string> _appliedAlgorithms;

        public MainWindow()
        {
            InitializeComponent();
            _dataManager = DataManager.Instance;

            // Initialize collections
            _rawSignalList = new ObservableCollection<DataModel>();
            _voidSignalList = new ObservableCollection<DataModel>();
            _appliedAlgorithms = new ObservableCollection<string>();

            // Bind the algorithms list
            lstAppliedAlgorithms.ItemsSource = _appliedAlgorithms;

            InitializePlots();
        }

        private void InitializePlots()
        {
            try
            {
                // Initialize main plots
                plotUpper.Plot.Title("Raw Signal");
                plotUpper.Plot.XLabel("Sample");
                plotUpper.Plot.YLabel("Voltage");
                plotUpper.Refresh();

                plotLower.Plot.Title("Void Signal");
                plotLower.Plot.XLabel("Sample");
                plotLower.Plot.YLabel("Voltage");
                plotLower.Refresh();

                // Initialize preview plot
                plotPreview.Plot.Title("Preview");
                plotPreview.Plot.Style(figureBackground: System.Drawing.Color.Transparent);
                plotPreview.Plot.XAxis.Label("");
                plotPreview.Plot.YAxis.Label("");
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
                    await _dataManager.LoadDataAsync(openFileDialog.FileName);
                    if (_dataManager.CurrentData != null)
                    {
                        _rawSignalList.Add(_dataManager.CurrentData);
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
                    await _dataManager.LoadDataAsync(openFileDialog.FileName);
                    if (_dataManager.CurrentData != null)
                    {
                        _voidSignalList.Add(_dataManager.CurrentData);
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
            _rawSignalList.Clear();
            UpdatePlots(null);
        }

        private void BtnClearVoidData_Click(object sender, RoutedEventArgs e)
        {
            _voidSignalList.Clear();
            UpdatePlots(null);
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
                
                _voidSignalList.Add(processedModel);
                UpdatePlots(processedModel);
            }
        }

        private void UpdatePlots(DataModel data)
        {
            if (plotUpper != null)
            {
                // 상단 그래프 업데이트 (Raw Signal)
                plotUpper.Plot.Clear();
                if (chkRawSignal.IsChecked == true)
                {
                    foreach (var rawData in _rawSignalList)
                    {
                        if (rawData?.Volt != null && rawData.Volt.Length > 0)
                        {
                            // Convert seconds to nanoseconds (multiply by 1e9)
                            double[] dataX = rawData.Second != null 
                                ? rawData.Second.Select(s => s * 1e9).ToArray()
                                : Enumerable.Range(0, rawData.Volt.Length).Select(x => (double)x).ToArray();
                            rawScatter = plotUpper.Plot.AddScatter(dataX, rawData.Volt);
                        }
                    }
                }
                plotUpper.Refresh();
            }

            if (plotLower != null)
            {
                // 하단 그래프 업데이트 (Void Signal)
                plotLower.Plot.Clear();
                if (chkProcessedSignal.IsChecked == true)
                {
                    foreach (var voidData in _voidSignalList)
                    {
                        if (voidData?.Volt != null && voidData.Volt.Length > 0)
                        {
                            // Convert seconds to nanoseconds (multiply by 1e9)
                            double[] dataX = voidData.Second != null 
                                ? voidData.Second.Select(s => s * 1e9).ToArray()
                                : Enumerable.Range(0, voidData.Volt.Length).Select(x => (double)x).ToArray();
                            voidScatter = plotLower.Plot.AddScatter(dataX, voidData.Volt);
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
                if (_dataManager.CurrentData?.Volt != null)
                {
                    plotPreview.Plot.Clear();

                    // Plot the full data range with nanoseconds
                    double[] xData = _dataManager.CurrentData.Second != null 
                        ? _dataManager.CurrentData.Second.Select(s => s * 1e9).ToArray()
                        : Enumerable.Range(0, _dataManager.CurrentData.Volt.Length).Select(x => (double)x).ToArray();
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
                        xMin: xData.First(),
                        xMax: xData.Last(),
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
            if (data?.Volt != null && data.Volt.Length > 0)
            {
                // Get X axis range from Second data if available (convert to nanoseconds)
                double[] xData = data.Second != null 
                    ? data.Second.Select(s => s * 1e9).ToArray()
                    : Enumerable.Range(0, data.Volt.Length).Select(x => (double)x).ToArray();
                double xMin = xData.First();
                double xMax = xData.Last();
                
                if (rangeSliderX != null)
                {
                    rangeSliderX.Minimum = xMin;
                    rangeSliderX.Maximum = xMax;
                    rangeSliderX.LowerValue = xMin;
                    rangeSliderX.HigherValue = xMax;
                }

                // Update Y axis range
                if (rangeSliderY != null && data.Volt.Length > 0)
                {
                    double yMin = data.Volt.Min();
                    double yMax = data.Volt.Max();
                    
                    // Add some padding to the Y range (10%)
                    double padding = (yMax - yMin) * 0.1;
                    yMin -= padding;
                    yMax += padding;

                    rangeSliderY.Minimum = yMin;
                    rangeSliderY.Maximum = yMax;
                    rangeSliderY.LowerValue = yMin;
                    rangeSliderY.HigherValue = yMax;
                }

                // Update preview plot
                UpdatePreviewPlot();
                // Apply the initial limits
                UpdateAxisLimits();
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

        private void PlotPreview_MouseMove(object sender, MouseEventArgs e)
        {
            // 미리보기 플롯에서는 마우스 이벤트를 무시
            e.Handled = true;
        }
    }
}

