using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using Dalamud.Game.Network;
using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.Structures;
using Dalamud.Game.Network.Universalis.MarketBoardUploaders;
using FFXIV_ACT_Plugin;
using UniversalisPlugin.MarketBoardUploaders.Universalis;

[assembly: AssemblyTitle("FFXIVMB ACT plugin")]
[assembly: AssemblyDescription("ACT plugin that automatically uploads market board data to FFXIVMB.com, forked from Universalis.")]
[assembly: AssemblyCompany("purveyor")]
[assembly: AssemblyVersion("1.0.0.0")]

namespace UniversalisPlugin
{
	public class UniversalisPluginControl : UserControl, IActPluginV1
	{
		#region Designer Created Code (Avoid editing)
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UniversalisPluginControl));
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.logTextBox = new System.Windows.Forms.RichTextBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(106, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(194, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Thank you for contributing to FFXIVMB!";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 50);
            this.pictureBox1.TabIndex = 4;
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
            this.logTextBox.TextChanged += new System.EventHandler(this.LogTextBox_TextChanged);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(423, 232);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(238, 137);
            this.richTextBox1.TabIndex = 5;
            this.richTextBox1.Text = resources.GetString("richTextBox1.Text");
            this.richTextBox1.TextChanged += new System.EventHandler(this.RichTextBox1_TextChanged);
            // 
            // UniversalisPluginControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.richTextBox1);
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
        private RichTextBox richTextBox1;
        private System.Windows.Forms.Label label1;

		#endregion
		public UniversalisPluginControl()
		{
			InitializeComponent();
		}

		Label lblStatus;    // The status label that appears in ACT's Plugin tab
		string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\PluginSample.config.xml");
		SettingsSerializer xmlSettings;

        private Definitions _definitions;

        public FFXIV_ACT_Plugin.FFXIV_ACT_Plugin FfxivPlugin;

        public uint CurrentWorldId => FfxivPlugin.DataRepository.GetCombatantList()
            .First(c => c.ID == FfxivPlugin.DataRepository.GetCurrentPlayerID()).CurrentWorldID;
        public ulong LocalContentId;

        private List<MarketBoardItemRequest> _marketBoardRequests = new List<MarketBoardItemRequest>();
        private IMarketBoardUploader _uploader;

        #region IActPluginV1 Members
		public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
		{
			lblStatus = pluginStatusText;   // Hand the status label's reference to our local var
			pluginScreenSpace.Controls.Add(this);   // Add this UserControl to the tab ACT provides
			this.Dock = DockStyle.Fill; // Expand the UserControl to fill the tab's client space
			xmlSettings = new SettingsSerializer(this); // Create a new settings serializer and pass it this instance
			LoadSettings();

            pluginScreenSpace.Text = "FFXIVMB";

            Log(Definitions.GetJson());

            try
            {
                //if (CheckNeedsUpdate())
                //{
                //    MessageBox.Show(
                //        "The FFXIVMB plugin needs to be updated. Please download an updated version from the FFXIVMB website",
                //        "FFXIVMB plugin update", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                //    Process.Start("https://ffxivmb.com/Downloads");
                //    return;
                //}

                _definitions = Definitions.Get();
                FfxivPlugin = GetFfxivPlugin();

                FfxivPlugin.DataSubscription.NetworkReceived += DataSubscriptionOnNetworkReceived;

                _uploader = new FFXIVMBUploader(this);
                Log("FFXIVMB plugin loaded.");
                lblStatus.Text = "Plugin Started";
            }
            catch (Exception ex)
            {
                Log("[ERROR] Could not initialize plugin:\n" + ex);
                lblStatus.Text = "Plugin Failed";
            }
        }

        public void DeInitPlugin()
		{
			// Unsubscribe from any events you listen to when exiting!
            FfxivPlugin.DataSubscription.NetworkReceived -= DataSubscriptionOnNetworkReceived;

			SaveSettings();
			lblStatus.Text = "Plugin Exited";
		}
		#endregion

        #region FFXIV plugin handling

        private void DataSubscriptionOnNetworkReceived(string connection, long epoch, byte[] message)
        {
            var opCode = BitConverter.ToInt16(message, 0x12);

            if (opCode == _definitions.PlayerSetup)
            {
                LocalContentId = BitConverter.ToUInt64(message, 0x20);
                Log($"New CID: {LocalContentId.ToString("X")}");
                return;
            }

            if (opCode == _definitions.MarketBoardItemRequestStart)
            {
                var catalogId = (uint) BitConverter.ToInt32(message, 0x20);
                var amount = message[0x2B];

                _marketBoardRequests.Add(new MarketBoardItemRequest
                {
                    CatalogId = catalogId,
                    AmountToArrive = amount,
                    Listings = new List<MarketBoardCurrentOfferings.MarketBoardItemListing>(),
                    History = new List<MarketBoardHistory.MarketBoardHistoryListing>()
                });

                Log($"NEW MB REQUEST START: item#{catalogId} amount#{amount}");
                return;
            }

            if (opCode == _definitions.MarketBoardOfferings)
            {
                var listing = MarketBoardCurrentOfferings.Read(message.Skip(0x20).ToArray());

                var request =
                    this._marketBoardRequests.LastOrDefault(
                        r => r.CatalogId == listing.ItemListings[0].CatalogId && !r.IsDone);

                if (request == null)
                {
                    Log(
                        $"[ERROR] Market Board data arrived without a corresponding request: item#{listing.ItemListings[0].CatalogId}");
                    return;
                }

                if (request.Listings.Count + listing.ItemListings.Count > request.AmountToArrive)
                {
                    Log(
                        $"[ERROR] Too many Market Board listings received for request: {request.Listings.Count + listing.ItemListings.Count} > {request.AmountToArrive} item#{listing.ItemListings[0].CatalogId}");
                    return;
                }

                if (request.ListingsRequestId != -1 && request.ListingsRequestId != listing.RequestId)
                {
                    Log(
                        $"[ERROR] Non-matching RequestIds for Market Board data request: {request.ListingsRequestId}, {listing.RequestId}");
                    return;
                }

                if (request.ListingsRequestId == -1 && request.Listings.Count > 0)
                {
                    Log(
                        $"[ERROR] Market Board data request sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                    return;
                }

                if (request.ListingsRequestId == -1)
                {
                    request.ListingsRequestId = listing.RequestId;
                    Log($"First Market Board packet in sequence: {listing.RequestId}");
                }

                request.Listings.AddRange(listing.ItemListings);

                Log($"Added {listing.ItemListings.Count} ItemListings to request#{request.ListingsRequestId}, now {request.Listings.Count}/{request.AmountToArrive}, item#{request.CatalogId}");

                if (request.IsDone)
                {
                    Log($"Market Board request finished, starting upload: request#{request.ListingsRequestId} item#{request.CatalogId} amount#{request.AmountToArrive}");
                    try
                    {
                        this._uploader.Upload(request);
                    }
                    catch (Exception ex)
                    {
                        Log("[ERROR] Market Board data upload failed:\n" + ex);
                    }
                }

                return;
            }

            if (opCode == _definitions.MarketBoardHistory)
            {
                var listing = MarketBoardHistory.Read(message.Skip(0x20).ToArray());

                var request = this._marketBoardRequests.LastOrDefault(r => r.CatalogId == listing.CatalogId);

                if (request == null)
                {
                    Log(
                        $"Market Board data arrived without a corresponding request: item#{listing.CatalogId}");
                    return;
                }

                if (request.ListingsRequestId != -1)
                {
                    Log(
                        $"Market Board data history sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                    return;
                }

                request.History.AddRange(listing.HistoryListings);

                Log($"Added history for item#{listing.CatalogId}");
            }
        }

        private FFXIV_ACT_Plugin.FFXIV_ACT_Plugin GetFfxivPlugin()
        {
            object ffxivPlugin = null;  
            
            while (ffxivPlugin == null)
            {
                var plugins = Advanced_Combat_Tracker.ActGlobals.oFormActMain.ActPlugins;
                foreach (var plugin in plugins)
                {
                    if (plugin.pluginFile.Name.ToUpper().Contains("FFXIV_ACT_Plugin".ToUpper()) &&
                        plugin.lblPluginStatus.Text.ToUpper().Contains("FFXIV Plugin Started.".ToUpper()))
                    {
                        ffxivPlugin = plugin.pluginObj;
                    }
                }
                System.Threading.Thread.Sleep(1);
            }

            if (ffxivPlugin == null)
                throw new Exception("Could not find FFXIV plugin. Make sure that it is loaded before FFXIVMB ACT plugin.");

            return (FFXIV_ACT_Plugin.FFXIV_ACT_Plugin) ffxivPlugin;
        }

        #endregion

        #region Miscellaneous

        public void Log(string text) => logTextBox.AppendText($"{text}\n");

        //private static bool CheckNeedsUpdate()
        //{
        //    using (var client = new WebClient())
        //    {
        //        var remoteVersion =
        //            client.DownloadString(
        //                "https://raw.githubusercontent.com/goaaats/universalis_act_plugin/master/version");

        //        return !remoteVersion.StartsWith(Util.GetAssemblyVersion());
        //    }
        //}

        #endregion

        void LoadSettings()
		{
			// Add any controls you want to save the state of
			//xmlSettings.AddControlSetting(textBox1.Name, textBox1);

			if (File.Exists(settingsFile))
			{
				FileStream fs = new FileStream(settingsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				XmlTextReader xReader = new XmlTextReader(fs);

				try
				{
					while (xReader.Read())
					{
						if (xReader.NodeType == XmlNodeType.Element)
						{
							if (xReader.LocalName == "SettingsSerializer")
							{
								xmlSettings.ImportFromXml(xReader);
							}
						}
					}
				}
				catch (Exception ex)
				{
					lblStatus.Text = "Error loading settings: " + ex.Message;
				}
				xReader.Close();
			}
		}
		void SaveSettings()
		{
			FileStream fs = new FileStream(settingsFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
			XmlTextWriter xWriter = new XmlTextWriter(fs, Encoding.UTF8);
			xWriter.Formatting = Formatting.Indented;
			xWriter.Indentation = 1;
			xWriter.IndentChar = '\t';
			xWriter.WriteStartDocument(true);
			xWriter.WriteStartElement("Config");    // <Config>
			xWriter.WriteStartElement("SettingsSerializer");    // <Config><SettingsSerializer>
			xmlSettings.ExportToXml(xWriter);   // Fill the SettingsSerializer XML
			xWriter.WriteEndElement();  // </SettingsSerializer>
			xWriter.WriteEndElement();  // </Config>
			xWriter.WriteEndDocument(); // Tie up loose ends (shouldn't be any)
			xWriter.Flush();    // Flush the file buffer to disk
			xWriter.Close();
		}

        private void LogTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
