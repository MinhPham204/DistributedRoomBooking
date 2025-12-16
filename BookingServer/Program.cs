//bookingserver/program.cs
using System;
using System.IO;
using System.Windows.Forms;
namespace BookingServer;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (s, e) =>
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.WriteAllText(logPath, e.Exception.ToString());
                MessageBox.Show($"App crashed. Log saved to: {logPath}\n\n{e.Exception.Message}", "BookingServer Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                MessageBox.Show(e.Exception.ToString(), "BookingServer Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.WriteAllText(logPath, ex?.ToString() ?? "UnhandledException (non-Exception)");
                MessageBox.Show($"Unhandled exception. Log saved to: {logPath}", "BookingServer Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // ignore
            }
        };

        BookingServer.Form1 form;
        try
        {
            form = new BookingServer.Form1();
        }
        catch (Exception ex)
        {
            try
            {
                var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.WriteAllText(logPath, ex.ToString());
                MessageBox.Show($"Failed to start Form1. Log saved to: {logPath}\n\n{ex.Message}", "BookingServer Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                MessageBox.Show(ex.ToString(), "BookingServer Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }

        Application.Run(form);
    }    
}