using Machina.FFXIV;
using Machina.Infrastructure;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using UniversalisCommon;
using UniversalisStandaloneUploader.Properties;

namespace UniversalisStandaloneUploader
{
    public partial class UploaderForm : Form
    {
        private FFXIVNetworkMonitor _ffxivNetworkMonitor;
        private PacketProcessor _packetProcessor;

        private const string ApiKey = "xQAqN1PTellr4hZQfbgeIwp4zDCutFFUferOHBuN";

        public UpdateCheckResult UpdateCheckRes { get; set; }
        public Exception UpdateCheckException { get; set; }

        private bool InitializedCapture { get; set; }
        private Thread InitializeThread { get; set; }

        public UploaderForm()
        {
            InitializeComponent();

            winPCapCheckBox.Checked = Settings.Default.UseWinPCap;

            try
            {
                if (Settings.Default.UpgradeRequired)
                {
                    Settings.Default.Upgrade();
                    Settings.Default.UpgradeRequired = false;
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                Log("Settings upgrade failed: " + ex);
            }

            if (Settings.Default.FirstLaunch)
            {
                Settings.Default.FirstLaunch = false;
                Settings.Default.Save();

                MessageBox.Show(Resources.FirstLaunchWelcome, Resources.UniversalisFormTitle, MessageBoxButtons.OK);
            }
        }

        private void UploaderForm_Resize(object sender, EventArgs e)
        {
            //if the form is minimized  
            //hide it from the task bar  
            //and show the system tray icon (represented by the NotifyIcon control)  
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                ShowTrayIcon();
            }
        }

        public void ShowTrayIcon()
        {
            systemTrayIcon.Visible = true;
            systemTrayIcon.ShowBalloonTip(1000);
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void ShowLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
        }

        private void SystemTrayIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
        }

        public void Log(string text)
        {
            logTextBox.AppendText($"{text}\n");
        }

        private void UploaderForm_Load(object sender, EventArgs e)
        {
            switch (UpdateCheckRes)
            {
                case UpdateCheckResult.NeedsUpdate:
                    Log(
                        "Application needs update. Please refrain from asking for support before attempting to update.");
                    break;
                case UpdateCheckResult.RemoteVersionParsingFailed:
                    Log("Failed to parse remote version information. Please report this error.");
                    break;
                case UpdateCheckResult.UpToDate:
                default:
                    break;
            }

            if (UpdateCheckException != null)
            {
                Log($"[ERROR] Failed to perform update check:\n{UpdateCheckException}");
            }

            try
            {
                _packetProcessor = new PacketProcessor(ApiKey);
                _packetProcessor.Log += (_, message) => BeginInvoke(new Action(() => Log(message)));

                InitializeThread = new Thread(() =>
                {
                    while (!InitializedCapture)
                    {
                        InitializeNetworkMonitor();
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                });
                InitializeThread.Start();

                Log("Uploader initialized.");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Could not initialize:\n{ex}");
            }
        }

        private void InitializeNetworkMonitor()
        {
            try
            {
                _ffxivNetworkMonitor?.Stop();
            }
            catch (NullReferenceException)
            {
                // This happens internally in Machina when the monitor is stopped before it
                // has been started - we can either store additional state or just catch the
                // exception here.
            }

            _ffxivNetworkMonitor = new FFXIVNetworkMonitor();
            _ffxivNetworkMonitor.MessageReceivedEventHandler += (_, _, message) =>
                _packetProcessor?.ProcessZonePacket(message);

            _ffxivNetworkMonitor.MonitorType = NetworkMonitorType.RawSocket;

            if (winPCapCheckBox.Checked)
            {
                _ffxivNetworkMonitor.MonitorType = NetworkMonitorType.WinPCap;
            }

            var window = FindWindow("FFXIVGAME", null);
            if (window == IntPtr.Zero)
            {
                return;
            }

            if (0 == GetWindowThreadProcessId(window, out var pid) || pid == 0)
            {
                return;
            }

            var proc = Process.GetProcessById(Convert.ToInt32(pid));
            var gamePath = proc.MainModule?.FileName;
            _ffxivNetworkMonitor.OodlePath = gamePath;

            _ffxivNetworkMonitor.Start();
            InitializedCapture = true;
        }

        private void UploaderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown || MessageBox.Show(Resources.AskStopUploadingData,
                    Resources.UniversalisFormTitle,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes)
            {
                try
                {
                    _ffxivNetworkMonitor.Stop();
                }
                finally
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                e.Cancel = true;
                Hide();
                ShowTrayIcon();
            }
        }

        private void WinPCapCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.UseWinPCap = winPCapCheckBox.Checked;
            Settings.Default.Save();

            try
            {
                InitializeNetworkMonitor();
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Could not re-initialize network monitor:\n{ex}");
            }
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }
}