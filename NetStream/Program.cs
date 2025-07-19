using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NetStream
{
    internal class Program
    {
        [STAThread]
        [UnmanagedCallersOnly]
        public static void NativeEntryPoint(int argc, IntPtr argv)
        {
            Thread staThread = new Thread(RunMainManaged);
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join(); 
        }

        // WPF başlatma metodu
        [STAThread]
        public static void RunMainManaged()
        {
            try
            {
                var app = new App();
                app.Startup += app.App_OnStartup;
                app.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }
    }
}
