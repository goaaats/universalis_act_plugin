using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using UniversalisCommon;
using UniversalisStandaloneUploader.Properties;

namespace UniversalisStandaloneUploader
{
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            if (UpdateUtils.CheckNeedsUpdate(Assembly.GetAssembly(typeof(Program))))
            {
                MessageBox.Show(
                    Resources.UniversalisNeedsUpdateLong,
                    Resources.UniversalisNeedsUpdateLongCaption, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                Process.Start("https://github.com/goaaats/universalis_act_plugin/releases/latest");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var uploaderForm = new UploaderForm();

            Application.Run();

            uploaderForm.ShowTrayIcon();
        }
    }
}
