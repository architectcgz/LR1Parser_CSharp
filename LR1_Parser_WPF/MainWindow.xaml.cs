using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Exp3_test;
using Microsoft.Win32;

namespace Exp3_WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window,INotifyPropertyChanged
{
        private string _filePath;
        private LR1Parser _lr1Parser;
        private ObservableCollection<StepInfo> _stepInfos; 
        private string _timeDay;
        private string _timeHM;
        private DispatcherTimer _timer;

        public ObservableCollection<StepInfo> StepInfos
        {
            get { return _stepInfos; }
            set {
                _stepInfos = value;
                OnPropertyChanged();
            }
        }

        public string TimeDay
        {
            get { return _timeDay; }
            set
            {
                _timeDay = value;
                OnPropertyChanged();
            }
        }

        public string TimeHM
        {
            get { return _timeHM; }
            set
            {
                _timeHM = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            _lr1Parser = new();
            _stepInfos = new ObservableCollection<StepInfo>();
            DataGrid.ItemsSource = StepInfos;
            _timer = new ();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            var input = InputTextBox.Text;
            if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
            {
                StatusTextBlock.Foreground = Brushes.Red;
                StatusTextBlock.Text = "请先输入要分析的内容";
                return;
            }
            //注意这里要使得_lr1Parser中清除上次的StepInfos
            StepInfos.Clear();
            StatusTextBlock.Foreground = Brushes.Black;
            var result = _lr1Parser.ParseInput(InputTextBox.Text);
            StatusTextBlock.Foreground = result.StartsWith("ERROR") ? Brushes.Red : Brushes.Black;
            StatusTextBlock.Text = result;
            foreach (var stepInfo in _lr1Parser.StepInfos)
            {
                Console.WriteLine(stepInfo.ToString());
                StepInfos.Add(stepInfo);
            }

        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            // 设置初始目录为相对于当前项目目录的 "grammars" 文件夹
            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string grammarDirectory = System.IO.Path.Combine(projectDirectory, @"..\..\..\grammars");
            openFileDialog.InitialDirectory = System.IO.Path.GetFullPath(grammarDirectory);

            if (openFileDialog.ShowDialog() == true)
            {
                _filePath = openFileDialog.FileName;
                // 在这里处理文件选择逻辑
                StatusTextBlock.Text = $"选择的文法文件: {_filePath}";
                _lr1Parser = new LR1Parser();//清空上次生成的内容
                
                _lr1Parser.ReadGrammarFromFile(_filePath);

                // 预处理文法
                _lr1Parser.PreprocessGrammar();
                // 构建规范LR1项集族
                _lr1Parser.BuildCanonicalCollection();
                //_lr1Parser.PrintCanonicalCollection();
                // 构建分析表
                _lr1Parser.GenerateAnalysisTable();
            }
        }

        private void LookAnalysisTableButton_Click(object sender,RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                StatusTextBlock.Text = "请先选择语法文件!";
                StatusTextBlock.Foreground = Brushes.Red;
                return;
            }
            try
            {
                AnalysisTablePage analysisTablePage = new AnalysisTablePage(_lr1Parser.ActionTable,_lr1Parser.GotoTable);
                StatusTextBlock.Text = "查看分析表";
                StatusTextBlock.Foreground = Brushes.Black;
                analysisTablePage.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing AnalysisTablePage: {ex.Message}");
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeDay = DateTime.Now.ToString("yyyy-MM-dd");
            TimeHM = DateTime.Now.ToString("HH:mm:ss");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
}