﻿using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Common;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using UniversalisCommon;
using UniversalisPlugin.Properties;

namespace UniversalisPlugin
{
    public class UniversalisPluginControl : UserControl, IActPluginV1
    {
        #region Designer Created Code (Avoid editing)

        /// <summary>
        ///     Required designer variable.
        /// </summary>
        private readonly IContainer components = null;

        /// <summary>
        ///     Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        ///     Required method for Designer support - do not modify
        ///     the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.logTextBox = new System.Windows.Forms.RichTextBox();
            this.uploadedItemsLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(91, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(200, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Thank you for contributing to Universalis!";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImage = global::UniversalisPlugin.Properties.Resources.universalis_bodge;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.InitialImage = null;
            this.pictureBox1.Location = new System.Drawing.Point(18, 53);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(361, 158);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // logTextBox
            // 
            this.logTextBox.Location = new System.Drawing.Point(18, 217);
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.Size = new System.Drawing.Size(361, 152);
            this.logTextBox.TabIndex = 3;
            this.logTextBox.Text = "";
            // 
            // uploadedItemsLabel
            // 
            this.uploadedItemsLabel.AutoSize = true;
            this.uploadedItemsLabel.Location = new System.Drawing.Point(141, 29);
            this.uploadedItemsLabel.Name = "uploadedItemsLabel";
            this.uploadedItemsLabel.Size = new System.Drawing.Size(93, 13);
            this.uploadedItemsLabel.TabIndex = 4;
            this.uploadedItemsLabel.Text = "Uploaded Items: 0";
            this.uploadedItemsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // UniversalisPluginControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.uploadedItemsLabel);
            this.Controls.Add(this.logTextBox);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.label1);
            this.Name = "UniversalisPluginControl";
            this.Size = new System.Drawing.Size(686, 384);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private PictureBox pictureBox1;
        private RichTextBox logTextBox;
        private Label uploadedItemsLabel;
        private Label label1;

        #endregion

        public UniversalisPluginControl()
        {
            InitializeComponent();
        }

        private Label lblStatus; // The status label that appears in ACT's Plugin tab

        private readonly string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName,
            "Config\\UniversalisPlugin.config.xml");

        private SettingsSerializer xmlSettings;

        public object FFXIVPlugin;

        private const string ApiKey = "CiAQfpfIK6eDcBLRUSv1rp6neR7MsWsRkrhHvzBH";
        private PacketProcessor _universalisPacketProcessor;

        private int _uploadCount;

        #region IActPluginV1 Members

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            lblStatus = pluginStatusText; // Hand the status label's reference to our local var
            pluginScreenSpace.Controls.Add(this); // Add this UserControl to the tab ACT provides
            Dock = DockStyle.Fill; // Expand the UserControl to fill the tab's client space
            xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance
            LoadSettings();

            pluginScreenSpace.Text = Resources.UniversalisTitle;

            try
            {
                var updateCheckRes = UpdateUtils.UpdateCheck(Assembly.GetAssembly(GetType()));
                switch (updateCheckRes)
                {
                    case UpdateCheckResult.NeedsUpdate:
                        var dlgResult = MessageBox.Show(
                            Resources.UniversalisNeedsUpdateLong,
                            Resources.UniversalisNeedsUpdateLongCaption, MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);

                        if (dlgResult == DialogResult.OK)
                        {
                            UpdateUtils.OpenLatestReleaseInBrowser();
                        }

                        Log("Plugin needs update. Please refrain from asking for plugin support before attempting to update.");
                        lblStatus.Text = Resources.NeedsUpdate;
                        break;
                    case UpdateCheckResult.RemoteVersionParsingFailed:
                        Log("Failed to parse remote version information. Please report this error.");
                        lblStatus.Text = Resources.UpdateCheckFailed;
                        break;
                    case UpdateCheckResult.UpToDate:
                    default:
                        break;
                }

                var ffxivPlugin = GetFFXIVPlugin();
                FFXIVPlugin = ffxivPlugin;

                _universalisPacketProcessor = new PacketProcessor(ApiKey);
                _universalisPacketProcessor.Log += (_, message) => Log(message);

                var subs = FFXIVPlugin.GetType().GetProperty(nameof(FFXIV_ACT_Plugin.FFXIV_ACT_Plugin.DataSubscription))?.GetValue(FFXIVPlugin, null);
                if (subs == null)
                {
                    throw new NullReferenceException("Failed to get data subscriptions object!");
                }

                var recvDelegate = (NetworkReceivedDelegate)DataSubscriptionOnNetworkReceived;
                subs.GetType().GetEvent(nameof(IDataSubscription.NetworkReceived)).AddEventHandler(subs, recvDelegate);

                Log("Universalis plugin loaded.");
                lblStatus.Text = Resources.PluginStarted;
            }
            catch (Exception ex)
            {
                Log("[ERROR] Could not initialize plugin:\n" + ex);
                lblStatus.Text = Resources.PluginFailed;
            }
        }

        public void DeInitPlugin()
        {
            // Unsubscribe from any events you listen to when exiting!
            var subs = FFXIVPlugin.GetType().GetProperty(nameof(FFXIV_ACT_Plugin.FFXIV_ACT_Plugin.DataSubscription))!.GetValue(FFXIVPlugin, null);

            var recvDelegate = (NetworkReceivedDelegate)DataSubscriptionOnNetworkReceived;
            subs.GetType().GetEvent(nameof(IDataSubscription.NetworkReceived)).RemoveEventHandler(subs, recvDelegate);

            SaveSettings();
            lblStatus.Text = Resources.PluginExited;
        }

        #endregion

        #region FFXIV plugin handling

        private void DataSubscriptionOnNetworkReceived(string connection, long epoch, byte[] message)
        {
            try
            {
                if (_universalisPacketProcessor.ProcessZonePacket(message))
                    IncreaseUploadCount();
            }
            catch (Exception e)
            {
                Log("[ERROR] Uncaught exception in DataSubscriptionOnNetworkReceived: " + e);
            }
        }

        private static FFXIV_ACT_Plugin.FFXIV_ACT_Plugin GetFFXIVPlugin()
        {
            var plugins = ActGlobals.oFormActMain.ActPlugins;

            if (plugins
                    .Where(p => p.pluginFile.Name.ToUpper().Contains(nameof(FFXIV_ACT_Plugin).ToUpper()))
                    .FirstOrDefault(p => p.pluginObj is FFXIV_ACT_Plugin.FFXIV_ACT_Plugin)
                    ?.pluginObj is not FFXIV_ACT_Plugin.FFXIV_ACT_Plugin ffxivPlugin)
            {
                throw new Exception("Could not find FFXIV plugin. Make sure that it is loaded before Universalis.");
            }

            return ffxivPlugin;
        }

        #endregion

        #region Miscellaneous

        public void Log(string text)
        {
            logTextBox.AppendText($"{text}\n");
        }

        public void IncreaseUploadCount()
        {
            _uploadCount++;
            uploadedItemsLabel.Text = string.Format(Resources.UploadedItemsCount, _uploadCount);
        }

        #endregion

        public long LastSavedContentId;

        private void LoadSettings()
        {
            // Add any controls you want to save the state of
            //xmlSettings.AddControlSetting(textBox1.Name, textBox1);
            xmlSettings.AddLongSetting("LastSavedContentId");

            if (File.Exists(settingsFile))
            {
                var fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var xReader = new XmlTextReader(fs);

                try
                {
                    while (xReader.Read())
                        if (xReader.NodeType == XmlNodeType.Element)
                            if (xReader.LocalName == "SettingsSerializer")
                                xmlSettings.ImportFromXml(xReader);
                }
                catch (Exception ex)
                {
                    lblStatus.Text = Resources.ErrorLoadingSettings + ex.Message;
                }

                xReader.Close();
            }
        }

        private void SaveSettings()
        {
            var fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            var xWriter = new XmlTextWriter(fs, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                Indentation = 1,
                IndentChar = '\t',
            };

            xWriter.WriteStartDocument(true);
            xWriter.WriteStartElement("Config"); // <Config>
            xWriter.WriteStartElement("SettingsSerializer"); // <Config><SettingsSerializer>
            xmlSettings.ExportToXml(xWriter); // Fill the SettingsSerializer XML
            xWriter.WriteEndElement(); // </SettingsSerializer>
            xWriter.WriteEndElement(); // </Config>
            xWriter.WriteEndDocument(); // Tie up loose ends (shouldn't be any)
            xWriter.Flush(); // Flush the file buffer to disk
            xWriter.Close();
        }
    }
}