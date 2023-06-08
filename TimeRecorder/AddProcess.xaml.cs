using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Management;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows.Controls;

namespace TimeRecorder
{
    public partial class AddProcess : Window
    {
        public AddProcess()
        {
            InitializeComponent();
        }

        private void AddFind_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = false;
            fileDialog.Filter = "Exe Files|*.exe";
            fileDialog.DefaultExt = "*.exe";
            bool? dialogOK = fileDialog.ShowDialog();

            if (dialogOK == true)
            {
                DirTextBox.Text = fileDialog.FileName;
            }
        }

        private void NumbericTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void InputWaitTextChange(object sender, TextChangedEventArgs e)
        {
            float inputwait;

            if (float.TryParse(InputWaitTextBox.Text, out inputwait) && inputwait < 0.05)
            {
                InputWaitTextBox.Text = "0.05";
            }
        }

        private void InputSaveTextChange(object sender, TextChangedEventArgs e)
        {
            float inputwait;
            float inputsave;

            if (float.TryParse(InputSaveTextBox.Text, out inputsave) && float.TryParse(InputWaitTextBox.Text, out inputwait) && inputsave > inputwait)
            {
                InputSaveTextBox.Text = InputWaitTextBox.Text;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var list = Processes.ProcessList;
            string pname;
            string dir = DirTextBox.Text.Trim();
            float hours;
            float inputwait;
            float inputsave;

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

            long realhours = (long)(hours*3600000);
            long realinputwait = (long)Math.Round(inputwait*1000);
            long realinputsave = (long)Math.Round(inputsave*1000);
            string datetime = "--";

            bool recordwnd = (bool)UseWndCheckBox.IsChecked;
            TRProcess process;

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
                    Ico = TRIconConverter.ToImageSource(dir),
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
                    Ico = TRIconConverter.ToImageSource(dir),
                };
            }
            list.Add(process);

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

            string programPath = Directory.GetCurrentDirectory();
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

            Close();
        }
    }
}
