using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetStream
{
    public class Taskbar
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string className, string windowText);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hwnd, int command);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, IntPtr windowTitle);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;

        protected static IntPtr Handle
        {
            get
            {
                return FindWindow("Shell_TrayWnd", "");
            }
        }

        protected static IntPtr HandleOfStartButton
        {
            get
            {
                IntPtr handleOfDesktop = GetDesktopWindow();
                IntPtr handleOfStartButton = FindWindowEx(handleOfDesktop, IntPtr.Zero, "button", IntPtr.Zero);
                return handleOfStartButton;
            }
        }

        public static void Show()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            try
            {
                IntPtr handle = Handle;
                if (handle != IntPtr.Zero)
                    ShowWindow(handle, SW_SHOW);

                IntPtr startButtonHandle = HandleOfStartButton;
                if (startButtonHandle != IntPtr.Zero)
                    ShowWindow(startButtonHandle, SW_SHOW);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Taskbar Show error: {ex.Message}");
            }
        }

        public static void Hide()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            try
            {
                IntPtr handle = Handle;
                if (handle != IntPtr.Zero)
                    ShowWindow(handle, SW_HIDE);

                IntPtr startButtonHandle = HandleOfStartButton;
                if (startButtonHandle != IntPtr.Zero)
                    ShowWindow(startButtonHandle, SW_HIDE);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Taskbar Hide error: {ex.Message}");
            }
        }
    }
}
