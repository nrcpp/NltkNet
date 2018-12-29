using NltkNet;
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

namespace SampleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow _instance;

        public MainWindow()
        {
            InitializeComponent();

            _instance = this;
            Loaded += MainWindow_Loaded;
        }

        public static void Log(string msg)
        {
            _instance.Dispatcher.BeginInvoke(new Action(() => _instance._logText.Text += msg + "\r\n"));
        }

        public static void ClearLog(string msg) =>        
            _instance.Dispatcher.BeginInvoke(new Action(() => _instance._logText.Text = ""));
        

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                Log("Nltk.Init() Begin");

                Nltk.Init(new List<string>
                {
                    @"C:\IronPython27\Lib",
                    @"C:\IronPython27\Lib\site-packages",
                });

                Log("Nltk.Init() Done");
            });
        }


    }
}
