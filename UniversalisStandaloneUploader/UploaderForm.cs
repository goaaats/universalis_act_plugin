using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Machina;
using Machina.FFXIV;
using UniversalisCommon;
using UniversalisPlugin;

namespace UniversalisStandaloneUploader
{
    public partial class UploaderForm : Form
    {
        private FFXIVNetworkMonitor _ffxivNetworkMonitor;
        private PacketProcessor _packetProcessor;

        private const string ApiKey = "xQAqN1PTellr4hZQfbgeIwp4zDCutFFUferOHBuN";

        public UploaderForm()
        {
            InitializeComponent();

            winPCapCheckBox.Checked = Properties.Settings.Default.UseWinPCap;

            #if DEBUG
            Log(Definitions.GetJson());
#endif
        }

        private void UploaderForm_Resize(object sender, EventArgs e)
        {
            //if the form is minimized  
            //hide it from the task bar  
            //and show the system tray icon (represented by the NotifyIcon control)  
            if (this.WindowState == FormWindowState.Minimized)  
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
            try
            {
                _packetProcessor = new PacketProcessor(ApiKey);
                _packetProcessor.Log += (o, message) => 
                    this.BeginInvoke(new Action(() => Log(message)));

                InitializeNetworkMonitor();

                Log("Uploader initialized.");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Could not initialize:\n{ex}");
            }
        }

        private void InitializeNetworkMonitor()
        {
            _ffxivNetworkMonitor = new FFXIVNetworkMonitor();
            _ffxivNetworkMonitor.MessageReceived += (connection, epoch, message) =>
                _packetProcessor.ProcessZonePacket(message);

            _ffxivNetworkMonitor.MonitorType = TCPNetworkMonitor.NetworkMonitorType.RawSocket;

            if (winPCapCheckBox.Checked)
                _ffxivNetworkMonitor.MonitorType = TCPNetworkMonitor.NetworkMonitorType.WinPCap;

            _ffxivNetworkMonitor.Start();
        }

        private void UploaderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.WindowsShutDown || MessageBox.Show("Do you want to stop uploading market board data?", "Universalis Uploader",
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
            Properties.Settings.Default.UseWinPCap = winPCapCheckBox.Checked;
            Properties.Settings.Default.Save();

            try
            {
                _ffxivNetworkMonitor.Stop();
                InitializeNetworkMonitor();
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Could not re-initialize network monitor:\n{ex}");
            }
        }
    }
}
