using System;
using System.IO;
//using System.Linq;
using System.Windows;
using System.Windows.Input;
//using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;
//using System.Diagnostics;

namespace TimeRecorder
{
    public partial class AddProcess : Window
    {
        public static bool IsOpen = false;
        public static AddProcess WndObject;

        public AddProcess()
        {
            InitializeComponent();
            IsOpen = true;
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IsOpen = false;
            WndObject = null;
        }

        private void AddFind_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "Exe Files|*.exe";
            fileDialog.DefaultExt = "*.exe";
            fileDialog.Title = "Select";
            bool? dialogOK = fileDialog.ShowDialog();

            if (dialogOK == true)
            {
                DirTextBox.Text = fileDialog.FileName;
            }
        }

        private void IcoAddFind_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "All Files|*|Common Image Files|*.png;*.ico;*.jpg;*.jpeg;*.jpe;*.jfif;*.bmp";
            fileDialog.DefaultExt = "*";
            fileDialog.Title = "Select";
            bool? dialogOK = fileDialog.ShowDialog();

            if (dialogOK == true)
            {
                IcoDirTextBox.Text = fileDialog.FileName;
            }
        }

        private void NumbericTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void InputWaitTextChange(object sender, EventArgs e)
        {
            float inputwait;
            bool isNum = float.TryParse(InputWaitTextBox.Text, out inputwait);

            if (isNum)
            {
                if (inputwait < 0.05)
                {
                    inputwait = 0.05f;
                    InputWaitTextBox.Text = "0.05";
                }

                float inputsave;
                bool isNum2 = float.TryParse(InputSaveTextBox.Text, out inputsave);

                if (isNum2 && inputsave > inputwait)
                {
                    InputSaveTextBox.Text = InputWaitTextBox.Text;
                }
            }
        }
        private void InputSaveTextChange(object sender, EventArgs e)
        {
            float inputwait;
            float inputsave;

            if (float.TryParse(InputSaveTextBox.Text, out inputsave) && float.TryParse(InputWaitTextBox.Text, out inputwait) && inputsave > inputwait)
            {
                InputSaveTextBox.Text = InputWaitTextBox.Text;
            }
        }

        private void WaitTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            InputWaitTextChange(null,null);
        }
        private void SaveTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            InputSaveTextChange(null, null);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var list = Processes.ProcessList;
            string pname;
            string dir = DirTextBox.Text.Trim();
            float hours;
            float inputwait;
            float inputsave;
            string diricon = null;
            string programPath = Directory.GetCurrentDirectory();

            if (string.IsNullOrEmpty(NameTextBox.Text))
            {
                MessageBox.Show("Name Box is empty!!");
                return;
            }

            if (string.IsNullOrEmpty(dir))
            {
                MessageBox.Show("Program's Path Box is empty!!");
                return;
            }

            if (!File.Exists(dir))
            {
                MessageBox.Show(".exe's path does't exist!!");
                return;
            }

            if (Path.GetExtension(dir) != ".exe")
            {
                MessageBox.Show("Program's path has an invalid file extension!!");
                return;
            }
            else
            {
                pname = Path.GetFileNameWithoutExtension(dir);
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Dir.Equals(dir) && ( ((list[i].RecordWnd == true && UseWndCheckBox.IsChecked == true) && list[i].WndName.Equals(WndNameTextBox.Text)) ||
                (list[i].RecordWnd == false && UseWndCheckBox.IsChecked == false) ))
                {
                    MessageBox.Show("Can't add the same program with the same configuration twice!!");
                    return;
                }
            }

            if (!float.TryParse(HoursTextBox.Text, out hours))
            {
                MessageBox.Show("Stating Hours has an invalid number character!!");
                return;
            }

            if (!float.TryParse(InputWaitTextBox.Text, out inputwait))
            {
                MessageBox.Show("Stating Hours has an invalid number character!!");
                return;
            }
            if (!float.TryParse(InputSaveTextBox.Text, out inputsave))
            {
                MessageBox.Show("Stating Hours has an invalid number character!!");
                return;
            }

            if (inputwait < 0.05f)
            {
                inputwait = 0.05f;
            }
            if (inputsave > inputwait)
            {
                inputsave = inputwait;
            }

            if ((bool)UseWndIconCheckBox.IsChecked)
            {
                diricon = "waitWnd";
            }
            else
            {
                diricon = IcoDirTextBox.Text;

                if (diricon.Contains(programPath))
                {
                    diricon = "."+diricon.Substring(programPath.Length);
                }
            }

            long realhours = (long)(hours*3600000);
            long realinputwait = (long)Math.Round(inputwait*1000);
            long realinputsave = (long)Math.Round(inputsave*1000);
            string datetime = "--";

            bool recordwnd = (bool)UseWndCheckBox.IsChecked;
            TRProcess process;

            System.Windows.Media.ImageSource ico = null;

            if (MainWindow.IsOpen)
            {
                ico = TRIconConverter.ToImageSource(diricon, dir);
            }

            if (recordwnd)
            {
                process = new TRProcess()
                {
                    Enabled = true,
                    RecordWnd = recordwnd,
                    MatchMode = (bool)MatchModeCheckBox.IsChecked,
                    Name = NameTextBox.Text.Trim(),
                    PName = pname + ".exe",
                    WndName = WndNameTextBox.Text.Trim(),
                    Dir = dir,
                    IcoDir = diricon,
                    Hours = realhours,
                    ViewHours = hours,
                    MinH = 0,
                    ViewMinH = 0,
                    FocusH = 0,
                    ViewFocusH = 0,
                    InputH = 0,
                    ViewInputH = 0,
                    InputKeyH = 0,
                    ViewInputKeyH = 0,
                    InputMouseH = 0,
                    ViewInputMouseH = 0,
                    InputKMH = 0,
                    ViewInputKMH = 0,
                    InputJoyH = 0,
                    ViewInputJoyH = 0,
                    InputWaitT = realinputwait,
                    InputSaveT = realinputsave,
                    First = datetime,
                    Last = datetime,
                    Ico = ico
                };
            }
            else
            {
                process = new TRProcess()
                {
                    Enabled = true,
                    RecordWnd = recordwnd,
                    Name = NameTextBox.Text.Trim(),
                    PName = pname+".exe",
                    WndName = "--",
                    Dir = dir,
                    IcoDir = diricon,
                    Hours = realhours,
                    ViewHours = hours,
                    MinH = 0,
                    ViewMinH = 0,
                    FocusH = 0,
                    ViewFocusH = 0,
                    InputH = 0,
                    ViewInputH = 0,
                    InputKeyH = 0,
                    ViewInputKeyH = 0,
                    InputMouseH = 0,
                    ViewInputMouseH = 0,
                    InputKMH = 0,
                    ViewInputKMH = 0,
                    InputJoyH = 0,
                    ViewInputJoyH = 0,
                    InputWaitT = realinputwait,
                    InputSaveT = realinputsave,
                    First = datetime,
                    Last = datetime,
                    Ico = ico
                };
            }
            list.Add(process);

            string dataFolder = @"\data";
            string listFile = @"\processlist.csv";

            string file = programPath + dataFolder + listFile;
            
            File.AppendAllText(file, 
                $"1," +
                $"{Convert.ToInt32(process.RecordWnd)}," +
                $"{Convert.ToInt32(process.MatchMode)}," +
                $"{process.Name}," +
                $"{process.PName}," +
                $"{process.WndName}," +
                $"{dir}," +
                $"{diricon}," +
                $"{process.Hours}," +
                $"{process.MinH}," +
                $"{process.FocusH}," +
                $"{process.InputH}," +
                $"{process.InputKeyH}," +
                $"{process.InputMouseH}," +
                $"{process.InputKMH}," +
                $"{process.InputJoyH}," +
                $"{inputwait}," +
                $"{inputsave}," +
                $"{datetime}," +
                $"{datetime}" +
            $"\n");

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

            Close();
        }
    }
}
