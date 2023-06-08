using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Management;
using System.Drawing;
using static TimeRecorder.SystemInputsRefresh;

namespace TimeRecorder
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            InitTimer();
            AddRunningProcessesOnStartUp();

            FocusInputsIntervals = new System.Threading.Timer(RefreshAllFocusInputs, App.autoEvent, 20, 20);
            this.SizeToContent = SizeToContent.Width;
        }

        private Timer rtimer;
        private void InitTimer()
        {
            rtimer = new Timer();
            rtimer.AutoReset = true;
            rtimer.Interval = 1000; // in miliseconds
            rtimer.Elapsed += new ElapsedEventHandler(RefreshList);
            rtimer.Start();
        }

        private void AddRunningProcessesOnStartUp()
        {
            var plist = Processes.ProcessList;
            string pname;

            for (int i = 0; i < plist.Count; i++)
            {
                pname = Path.GetFileNameWithoutExtension(plist[i].PName);

                using (var searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_Process"))
                using (var results = searcher.Get())
                {
                    var query = from p in Process.GetProcessesByName(pname)
                                join mo in results.Cast<ManagementObject>()
                                on p.Id equals (int)(uint)mo["ProcessId"]
                                select new
                                {
                                    //Process = p,
                                    Id = (int)(uint)mo["ProcessId"],
                                };

                    foreach (var p in query)
                    {
                        App.AddRunningProcess(p.Id, IntPtr.Zero);
                    }
                }
            }
        }

        private void RefreshList(object sender, ElapsedEventArgs e)
        {

            Application.Current.Dispatcher.Invoke(new Action(() => {
                ProcessViewList.Items.Refresh();
            }));

        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddProcess addwindow = new AddProcess();
            addwindow.Show();
        }

        //private void BoxStartup_Click(object sender, RoutedEventArgs e)
        //{
        //    RegistryKey key = Registry.CurrentUser.OpenSubKey(RegPath, true);

        //    if ((bool)BoxStartup.IsChecked)
        //    {
        //        key.SetValue("TimeRecorder", "\""+System.Reflection.Assembly.GetEntryAssembly().Location+"\" -silent");
        //    }
        //    else
        //    {
        //        key.DeleteValue("TimeRecorder", false);
        //    }
        //}

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Normal:
                case WindowState.Maximized:
                    this.ShowInTaskbar = true;
                    rtimer.Start();
                    break;

                case WindowState.Minimized:
                    this.ShowInTaskbar = false;
                    rtimer.Stop();
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Application.Current.Shutdown();
            rtimer.Dispose();
            base.OnClosed(e);
        }
    }

    public static class TRIconConverter
    {
        public static ImageSource ToImageSource(this String icon)
        {

            if (!File.Exists(icon))
            {
                return null;
            }

            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                Icon.ExtractAssociatedIcon(icon).Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }
    }

    public class Processes
    {
        public static ObservableCollection<TRProcess> ProcessList { get; set; } = GetProcesses();
        public static ObservableCollection<TRProcess> GetProcesses()
        {

            string programPath = Directory.GetCurrentDirectory();
            string dataFolder = @"\data";
            string listFile = @"\processlist.csv";

            Directory.CreateDirectory(programPath + dataFolder);

            var file = programPath + dataFolder + listFile;

            if (!File.Exists(file))
            {
                File.Create(programPath + dataFolder + listFile);
                return new ObservableCollection<TRProcess>();
            }

            var lines = File.ReadAllLines(file);
            var list = new ObservableCollection<TRProcess>();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Split(',');
                var process = new TRProcess()
                {
                    Enabled = line[0] == "1" ? true : false,
                    RecordWnd = line[1] == "1" ? true : false,
                    MatchMode = line[2] == "1" ? true : false,
                    Name = line[3],
                    PName = line[4],
                    WndName = line[5],
                    Dir = line[6],
                    Hours = long.Parse(line[7]),
                    ViewHours = float.Parse(line[7])/3600000,
                    MinH = long.Parse(line[8]),
                    ViewMinH = float.Parse(line[8])/3600000,
                    FocusH = long.Parse(line[9]),
                    ViewFocusH = float.Parse(line[9])/3600000,
                    InputH = long.Parse(line[10]),
                    ViewInputH = float.Parse(line[10])/3600000,
                    InputKeyH = long.Parse(line[11]),
                    ViewInputKeyH = float.Parse(line[11])/3600000,
                    InputMouseH = long.Parse(line[12]),
                    ViewInputMouseH = float.Parse(line[12])/3600000,
                    InputKMH = long.Parse(line[13]),
                    ViewInputKMH = float.Parse(line[13])/3600000,
                    InputJoyH = long.Parse(line[14]),
                    ViewInputJoyH = float.Parse(line[14])/3600000,
                    InputWaitT = (long)Math.Round(float.Parse(line[15])*1000),
                    InputSaveT = (long)Math.Round(float.Parse(line[16])*1000),
                    First = line[17],
                    Last = line[18],
                    Ico = TRIconConverter.ToImageSource(line[6]),
                };
                list.Add(process);
            }
            return list;
        }
    }

    public class TRProcess
    {
        public bool Enabled { get; set; }
        public bool RecordWnd { get; set; }
        public bool MatchMode { get; set; }
        public string Name { get; set; }
        public string PName { get; set; }
        public string WndName { get; set; }
        public string Dir { get; set; }
        public long Hours { get; set; }
        public float ViewHours { get; set; }
        public long MinH { get; set; }
        public float ViewMinH { get; set; }
        public long FocusH { get; set; }
        public float ViewFocusH { get; set; }
        //Any
        public long InputH { get; set; }
        public float ViewInputH { get; set; }
        //Keyboard
        public long InputKeyH { get; set; }
        public float ViewInputKeyH { get; set; }
        //Mouse
        public long InputMouseH { get; set; }
        public float ViewInputMouseH { get; set; }
        //Keyboard&Mouse
        public long InputKMH { get; set; }
        public float ViewInputKMH { get; set; }
        //Controller
        public long InputJoyH { get; set; }
        public float ViewInputJoyH { get; set; }
        public long InputWaitT { get; set; }
        public long InputSaveT { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public ImageSource Ico { get; set; }
    }
}
