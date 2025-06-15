using System;
using System.Windows.Forms;

namespace FDBEditor
{
    internal static class Program
    {
        /// <summary>
        ///  Entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize(); // Untuk .NET 6+ (Windows Forms styles)
            Application.Run(new MainEditor());
        }
    }
}
