using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

namespace BBSGUI
{
    internal static class Program
    {
        private static Mutex? s_singleInstanceMutex;
        private static readonly string MutexId = GetMutexName();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            s_singleInstanceMutex = new Mutex(initiallyOwned: true, name: MutexId, createdNew: out var createdNew);
            if (!createdNew)
            {
                return;
            }

            Application.ApplicationExit += OnApplicationExit;

            ApplicationConfiguration.Initialize();

            // Create main window
            var mainForm = new ControlPanel();

            // Create tray icon
            var tray = new NotifyIcon();
            tray.Icon = SystemIcons.Application;
            tray.Text = "ControlPanel";
            tray.Visible = true;

            // Context menu
            var menu = new ContextMenuStrip();
            var openItem = new ToolStripMenuItem("Open");
            openItem.Click += (s, e) =>
            {
                if (mainForm.WindowState == FormWindowState.Minimized)
                    mainForm.WindowState = FormWindowState.Normal;
                if (!mainForm.Visible) mainForm.Show();
                mainForm.BringToFront();
                mainForm.Activate();
            };
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) =>
            {
                // Dispose tray and exit
                tray.Visible = false;
                tray.Dispose();
                Application.Exit();
            };
            menu.Items.Add(openItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);
            tray.ContextMenuStrip = menu;

            // Double-click opens the window
            tray.DoubleClick += (s, e) =>
            {
                if (mainForm.WindowState == FormWindowState.Minimized)
                    mainForm.WindowState = FormWindowState.Normal;
                if (!mainForm.Visible) mainForm.Show();
                mainForm.BringToFront();
                mainForm.Activate();
            };

            // Optionally hide main window on minimize to keep it in tray
            mainForm.Resize += (s, e) =>
            {
                if (mainForm.WindowState == FormWindowState.Minimized)
                {
                    mainForm.Hide();
                }
            };

            // Run application
            Application.Run(mainForm);
        }

        private static void OnApplicationExit(object? sender, EventArgs e)
        {
            try
            {
                if (s_singleInstanceMutex != null)
                {
                    try { s_singleInstanceMutex.ReleaseMutex(); } catch { }
                    s_singleInstanceMutex.Dispose();
                    s_singleInstanceMutex = null;
                }
            }
            catch { }
        }

        private static string GetMutexName()
        {
            // Global prefix ensures visibility across sessions on Windows
            return "Global\\BBSGUI_SINGLE_INSTANCE_5B3A2C3D_0000_4E2A_BB8B_123456789ABC";
        }
    }
}