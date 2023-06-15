using System;
using System.Collections.ObjectModel;
//using System.Diagnostics;
using System.IO;
//using System.Linq;
//using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using System.Management;
using System.Drawing;
using System.Windows.Data;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using static TimeRecorder.SystemInputsRefresh;
using static TimeRecorder.SystemProcessesTracker;

namespace TimeRecorder
{
    public partial class MainWindow : Window
    {
        public static bool IsOpen = false;
        public static MainWindow WndObject;

        public MainWindow()
        {
            InitializeComponent();
            //InitTimer();
            //AddRunningProcessesOnStartUp();
            App.CleanUpOnStartUp();
            LoadListIcons();

            IsOpen = true;
            WndObject = this;
            this.SizeToContent = SizeToContent.Width;

            CheckAllSystemProcessesIntervals = new Timer(CheckAllSystemProcesses, null, 1000, Timeout.Infinite);
            FocusInputsIntervals = new Timer(RefreshAllFocusInputs, null, 20, Timeout.Infinite);
        }

        //private Timer rtimer;
        //private void InitTimer()
        //{
        //rtimer = new Timer();
        //rtimer.AutoReset = true;
        //rtimer.Interval = 1000; // in miliseconds
        //rtimer.Elapsed += new ElapsedEventHandler(RefreshList);
        //rtimer.Start();
        //}

        //private void AddRunningProcessesOnStartUp()
        //{
        //var plist = Processes.ProcessList;
        //string pname;

        //for (int i = 0; i < plist.Count; i++)
        //{
        //pname = Path.GetFileNameWithoutExtension(plist[i].PName);

        //using (var searcher = new ManagementObjectSearcher("SELECT ProcessId FROM Win32_Process"))
        //using (var results = searcher.Get())
        //{
        //var query = from p in Process.GetProcessesByName(pname)
        //join mo in results.Cast<ManagementObject>()
        //on p.Id equals (int)(uint)mo["ProcessId"]
        //select new
        //{
        ////Process = p,
        //Id = (int)(uint)mo["ProcessId"],
        //};

        //foreach (var p in query)
        //{
        //App.AddRunningProcess(p.Id, IntPtr.Zero);
        //}
        //}
        //}
        //}

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddProcess.IsOpen) 
            {
                if(AddProcess.WndObject.WindowState == WindowState.Minimized) 
                {
                    AddProcess.WndObject.WindowState = WindowState.Normal;
                }
                AddProcess.WndObject.Activate(); return; 
            }

            AddProcess.WndObject = new AddProcess();
            AddProcess.WndObject.Show();
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
                    //rtimer.Start();
                    IsOpen = true;
                    LoadListIcons();
                    break;

                case WindowState.Minimized:
                    this.ShowInTaskbar = false;
                    //rtimer.Stop();
                    IsOpen = false;
                    UnloadListIcons();
                    CleanUpAfterUnloadingIcons(); //We try to free some memory from the BitmapSource objects right away so it just doesn't stay there.
                    if (AddProcess.IsOpen) 
                    {
                        AddProcess.WndObject.Close();
                    }
                    break;
            }
        }

        public void CleanUpAfterUnloadingIcons()
        {
            for (int i = 1; i <= 5; i++)
            {
                CleanUp(i * 1000);
            }
            async void CleanUp(int delay)
            {
                await Task.Delay(delay);

                if (IsOpen) { return; }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        public void UnloadListIcons()
        {
            var plist = Processes.ProcessList;

            for (int i = 0; i < plist.Count; i++)
            {
                plist[i].Ico = null;
            }

            // BitmapSource has memory issues, we have to change the source binding or replace the old column with a new one.
            // It isn't a perfect method but it is better than nothing.
            ProcessViewList.Columns[0].SetValue(System.Windows.Controls.Image.SourceProperty, null);
            ProcessViewList.Columns[0] = new DataGridTemplateColumn(){};
            ProcessViewList.UpdateLayout(); // -->https://github.com/dotnet/wpf/issues/2397
        }
        public void LoadListIcons()
        {
            var plist = Processes.ProcessList;

            for (int i = 0; i < plist.Count; i++)
            {
                plist[i].Ico = TRIconConverter.ToImageSource(plist[i].IcoDir, plist[i].Dir);
            }

            FrameworkElementFactory image = new FrameworkElementFactory(typeof(System.Windows.Controls.Image));
            image.SetValue(System.Windows.Controls.Image.HeightProperty, 20d);
            image.SetValue(System.Windows.Controls.Image.WidthProperty, 20d);
            image.SetBinding(System.Windows.Controls.Image.SourceProperty, new Binding("Ico"));
            DataTemplate CellTemplate = new DataTemplate() { VisualTree = image };
            ProcessViewList.Columns[0] = new DataGridTemplateColumn() { Header = "", Width = 25, CellTemplate = CellTemplate };
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
            //rtimer.Dispose();
        }
    }

    public static class TRIconConverter
    {
        public static ImageSource ToImageSource(this String icon, string dir)
        {
            if (string.IsNullOrEmpty(icon))
            {
                icon = dir;
            }
            if (!File.Exists(icon))
            {
                return null;
            }

            if (icon.Substring(0, 2) == @".\")
            {
                icon = Directory.GetCurrentDirectory() + icon.Substring(1, icon.Length-1);
            }
            try
            {
                Uri image = new Uri(icon);

                if (image != null)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = image;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return bitmap;
                }
                else
                {
                    return Imaging.CreateBitmapSourceFromHIcon(
                        Icon.ExtractAssociatedIcon(icon).Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch
            {
                return Imaging.CreateBitmapSourceFromHIcon(
                    Icon.ExtractAssociatedIcon(icon).Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            } 
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
                    IcoDir = line[7],
                    Hours = long.Parse(line[8]),
                    ViewHours = float.Parse(line[8])/3600000,
                    MinH = long.Parse(line[9]),
                    ViewMinH = float.Parse(line[9])/3600000,
                    FocusH = long.Parse(line[10]),
                    ViewFocusH = float.Parse(line[10])/3600000,
                    InputH = long.Parse(line[11]),
                    ViewInputH = float.Parse(line[11])/3600000,
                    InputKeyH = long.Parse(line[12]),
                    ViewInputKeyH = float.Parse(line[12])/3600000,
                    InputMouseH = long.Parse(line[13]),
                    ViewInputMouseH = float.Parse(line[13])/3600000,
                    InputKMH = long.Parse(line[14]),
                    ViewInputKMH = float.Parse(line[14])/3600000,
                    InputJoyH = long.Parse(line[15]),
                    ViewInputJoyH = float.Parse(line[15])/3600000,
                    InputWaitT = (long)Math.Round(float.Parse(line[16])*1000),
                    InputSaveT = (long)Math.Round(float.Parse(line[17])*1000),
                    First = line[18],
                    Last = line[19],
                    Ico = null,
                };
                list.Add(process);
            }
            return list;
        }
    }

    public class TRProcess : INotifyPropertyChanged
    {
        public bool Enabled { get; set; }
        public bool RecordWnd { get; set; }
        public bool MatchMode { get; set; }
        public string Dir { get; set; }
        public string IcoDir { get; set; }

        public long Hours { get; set; }
        public long MinH { get; set; }
        public long FocusH { get; set; }
        public long InputH { get; set; }
        public long InputKeyH { get; set; }
        public long InputMouseH { get; set; }
        public long InputKMH { get; set; }
        public long InputJoyH { get; set; }

        public long InputWaitT { get; set; }
        public long InputSaveT { get; set; }

        // Elements shown in UI

        public string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }
        public string _pname { get; set; }
        public string PName
        {
            get { return _pname; }
            set { _pname = value; OnPropertyChanged("PName"); }
        }
        public string _wndname { get; set; }
        public string WndName
        {
            get { return _wndname; }
            set { _wndname = value; OnPropertyChanged("WndName"); }
        }
        public float _viewhours { get; set; }
        public float ViewHours
        {
            get { return _viewhours; }
            set { _viewhours = value; OnPropertyChanged("ViewHours"); }
        }
        public float _viewminh { get; set; }
        public float ViewMinH
        {
            get { return _viewminh; }
            set { _viewminh = value; OnPropertyChanged("ViewMinH"); }
        }
        public float _viewfocush { get; set; }
        public float ViewFocusH
        {
            get { return _viewfocush; }
            set { _viewfocush = value; OnPropertyChanged("ViewFocusH"); }
        }
        public float _viewinputh { get; set; }
        public float ViewInputH
        {
            get { return _viewinputh; }
            set { _viewinputh = value; OnPropertyChanged("ViewInputH"); }
        }
        public float _viewinputkeyh { get; set; }
        public float ViewInputKeyH
        {
            get { return _viewinputkeyh; }
            set { _viewinputkeyh = value; OnPropertyChanged("ViewInputKeyH"); }
        }
        public float _viewinputmouseh { get; set; }
        public float ViewInputMouseH
        {
            get { return _viewinputmouseh; }
            set { _viewinputmouseh = value; OnPropertyChanged("ViewInputMouseH"); }
        }
        public float _viewinputkmh { get; set; }
        public float ViewInputKMH
        {
            get { return _viewinputkmh; }
            set { _viewinputkmh = value; OnPropertyChanged("ViewInputKMH"); }
        }
        public float _viewinputjoyh { get; set; }
        public float ViewInputJoyH
        {
            get { return _viewinputjoyh; }
            set { _viewinputjoyh = value; OnPropertyChanged("ViewInputJoyH"); }
        }
        public string _first { get; set; }
        public string First
        {
            get { return _first; }
            set { _first = value; OnPropertyChanged("First"); }
        }
        public string _last { get; set; }
        public string Last
        {
            get { return _last; }
            set { _last = value; OnPropertyChanged("Last"); }
        }
        public ImageSource _ico { get; set; }
        public ImageSource Ico
        {
            get { return _ico; }
            set { _ico = value; OnPropertyChanged("Ico"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            //if (MainWindow.IsOpen) 
            //{
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            //}
        }
    }
}
