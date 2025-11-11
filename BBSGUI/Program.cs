using System.Runtime.InteropServices;
using System.Threading;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace BBSGUI
{
    internal static class Program
    {
        private static Mutex? s_singleInstanceMutex;
        private static readonly string MutexId = GetMutexName();
        private static NotifyIcon? s_trayIcon;
        private static bool s_allowExit = false;

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

            AppDomain.CurrentDomain.ProcessExit += OnApplicationExit;

            ApplicationConfiguration.Initialize();

#if WINDOWS
            // Create main window (WinForms)
            var mainForm = new ControlPanel();

            // Intercept close to hide instead of exit
            mainForm.FormClosing += (s, e) =>
            {
                if (!s_allowExit && e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    mainForm.Hide();
                }
            };

            // Create tray icon
            s_trayIcon = new NotifyIcon();
            s_trayIcon.Icon = SystemIcons.Application;
            s_trayIcon.Text = "ControlPanel";
            s_trayIcon.Visible = true;

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
                if (s_trayIcon != null)
                {
                    s_trayIcon.Visible = false;
                    s_trayIcon.Dispose();
                    s_trayIcon = null;
                }
                s_allowExit = true;
                Application.Exit();
            };
            menu.Items.Add(openItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);
            s_trayIcon.ContextMenuStrip = menu;

            // Double-click opens the window
            s_trayIcon.DoubleClick += (s, e) =>
            {
                if (mainForm.WindowState == FormWindowState.Minimized)
                    mainForm.WindowState = FormWindowState.Normal;
                if (!mainForm.Visible) mainForm.Show();
                mainForm.BringToFront();
                mainForm.Activate();
            };

            // Hide to tray on minimize
            mainForm.Resize += (s, e) =>
            {
                if (mainForm.WindowState == FormWindowState.Minimized)
                {
                    mainForm.Hide();
                }
            };

            // Run application
            Application.Run(mainForm);
#else
            // Non-Windows: run app (no tray)
            var app = new ControlPanel();
#endif
        }

        private static void OnApplicationExit(object? sender, EventArgs e)
        {
            try
            {
#if WINDOWS
                if (s_trayIcon != null)
                {
                    try { s_trayIcon.Visible = false; } catch { }
                    try { s_trayIcon.Dispose(); } catch { }
                    s_trayIcon = null;
                }
#endif
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