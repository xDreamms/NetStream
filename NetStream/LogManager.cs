using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace NetStream
{
    public class LogManager
    {
        public static void Initialize()
        {
            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetStream");

            if (!Directory.Exists(path))
            {
                FileHelper.IsFirstStart = true;
                Directory.CreateDirectory(path);
            }
            else
            {
                FileHelper.IsFirstStart = false;
            }
            string logsLocation = System.IO.Path.Combine(path, "Logs");

            if (!Directory.Exists(logsLocation))
            {
                Directory.CreateDirectory(logsLocation);
            }
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Async(a => a.File(System.IO.Path.Combine(logsLocation, "all-.logs"), rollingInterval: RollingInterval.Day))
                .MinimumLevel.Debug()
                .CreateLogger();
        }
    }
}
