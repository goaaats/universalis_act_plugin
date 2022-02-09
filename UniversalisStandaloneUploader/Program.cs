using System;
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var uploaderForm = new UploaderForm();

            // Update check
            try
            {
                uploaderForm.UpdateCheckRes = UpdateUtils.UpdateCheck(Assembly.GetAssembly(typeof(Program)));
                if (uploaderForm.UpdateCheckRes == UpdateCheckResult.NeedsUpdate)
                {
                    var dlgResult = MessageBox.Show(
                        Resources.UniversalisNeedsUpdateLong,
                        Resources.UniversalisNeedsUpdateLongCaption, MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Asterisk);

                    if (dlgResult == DialogResult.OK)
                    {
                        UpdateUtils.OpenLatestReleaseInBrowser();
                    }
                }
            }
            catch (Exception ex)
            {
                uploaderForm.UpdateCheckException = ex;
            }

            Application.Run();

            uploaderForm.ShowTrayIcon();
        }
    }
}
