using System;
using System.Windows.Forms;

namespace UniversalisStandaloneUploader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var uploaderForm = new UploaderForm();

            Application.Run();

            
            uploaderForm.ShowTrayIcon();
        }
    }
}
