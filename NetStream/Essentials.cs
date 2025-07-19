using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using NetStream.Views;
using Newtonsoft.Json;
using Serilog;

namespace NetStream
{
    public class Essentials
    {
        //public static void KillJackett()
        //{
        //    StopJackett();
        //}

        //public static bool IsJacketRunning()
        //{
        //    return Process.GetProcesses().Any(x => x.ProcessName == "JackettConsole");
        //}

        //public static Process? _jackettProcess;

        public static async Task RunJacketAsync()
        {
            await Task.Run(() => StartJackettService());
        }

        static List<string> torrentExcludeList = new List<string>
        {
            "knaben",
            "52bt",
            "btmet",
            "xtratorrent-st",
            "exttorrents",
            "gktorrent",
            "idope",
            "kickasstorrents-to",
            "limetorrents",
            "magnetcat",
            "nntt",
            "postman",
            "torrent9",
            "torrentcore",
            "torrentqq"
        };

        //private static async void StartJackett()
        //{
        //    try
        //    {
        //        if (IsJacketRunning())
        //        {
        //            foreach (var jackettProcess in Process.GetProcesses().Where(x => x.ProcessName == "JackettConsole"))
        //            {
        //                jackettProcess.Kill();
        //            }
        //        }

        //        string jackettConsolePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "Jackett\\JackettConsole.exe";
        //        _jackettProcess = new Process
        //        {
        //            StartInfo = new ProcessStartInfo
        //            {
        //                FileName = jackettConsolePath,
        //                Arguments = "--NoRestart", 
        //                UseShellExecute = false,
        //                RedirectStandardOutput = true,
        //                RedirectStandardError = true,
        //                CreateNoWindow = true
        //            }
        //        };

        //        _jackettProcess.OutputDataReceived += OutputHandler;
        //        _jackettProcess.ErrorDataReceived += OutputHandler;

        //        _jackettProcess.Start();
        //        _jackettProcess.BeginOutputReadLine();
        //        _jackettProcess.BeginErrorReadLine();

        //        Log.Information("Jackett started successfully.");

        //    }
        //    catch (Exception exception)
        //    {
        //        Log.Error("Running jackett failed: "+ exception.Message);
        //    }
        //}

        //public static void StopJackett()
        //{
        //    try
        //    {
        //        if (_jackettProcess != null && !_jackettProcess.HasExited)
        //        {
        //            _jackettProcess.Kill();
        //            _jackettProcess.Dispose();
        //            Log.Information("Jackett stopped successfully.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error("Error stopping Jackett: " + ex.Message);
        //    }
        //}

        public static string TextAfter(string value, string search)
        {
            return value.Substring(value.IndexOf(search) + search.Length);
        }

        public static async void GetSelectedIndexers()
        {
            if (File.Exists(NetStream.AppSettingsManager.appSettings.IndexersPath))
            {
                var text = File.ReadAllText(NetStream.AppSettingsManager.appSettings.IndexersPath);
                if (String.IsNullOrWhiteSpace(text))
                {
                    var allIndexers = await JackettService.GetIndexersAsync();
                    foreach (var indexer in allIndexers)
                    {
                        if (!torrentExcludeList.Contains(indexer.Id))
                        {
                            JackettService.SelectedIndexers.Add(new Indexer
                            {
                                Id = indexer.Id,
                                Title = indexer.Title
                            });
                        }
                    }

                    var js = JsonConvert.SerializeObject(JackettService.SelectedIndexers);
                    File.WriteAllText(NetStream.AppSettingsManager.appSettings.IndexersPath, js);
                }
                else
                {
                    bool changed = false;

                    JackettService.SelectedIndexers = JsonConvert.DeserializeObject<List<Indexer>>(
                        File.ReadAllText(NetStream.AppSettingsManager.appSettings.IndexersPath));
                    var allIndexers = await JackettService.GetIndexersAsync();

                    if (JackettService.SelectedIndexers != null && JackettService.SelectedIndexers.Count > 0)
                    {
                        foreach (var selectedIndexer in JackettService.SelectedIndexers.ToList())
                        {
                            if (!allIndexers.Any(x => x.Id == selectedIndexer.Id))
                            {
                                JackettService.SelectedIndexers.Remove(selectedIndexer);
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                    {
                        var js = JsonConvert.SerializeObject(JackettService.SelectedIndexers);
                        File.WriteAllText(NetStream.AppSettingsManager.appSettings.IndexersPath, js);
                    }
                }
            }
        }

        static async void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine != null && outLine.Data != null)
            {
                if (outLine.Data.Contains("Now listening on"))
                {
                    string jacketApiUrl = TextAfter(outLine.Data, "Now listening on: ");
                    AppSettingsManager.appSettings.JacketApiUrl = jacketApiUrl;
                    AppSettingsManager.SaveAppSettings();
                }
            }
        }

        //public static string PythonVersion()
        //{
        //    try
        //    {
        //        string result = "";
        //        string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        //        string path = localFolder + "\\Programs\\Python\\python.exe";
        //        ProcessStartInfo pycheck = new ProcessStartInfo();
        //        pycheck.FileName = path;
        //        pycheck.Arguments = "--version";
        //        pycheck.UseShellExecute = false;
        //        pycheck.RedirectStandardOutput = true;
        //        pycheck.CreateNoWindow = true;

        //        using (Process process = Process.Start(pycheck))
        //        {
        //            using (StreamReader reader = process.StandardOutput)
        //            {
        //                result = reader.ReadToEnd();
        //                return result;
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return "";
        //    }
        //}

        //public static void InstallPython()
        //{
        //    var pythonInstallerPath = AppDomain.CurrentDomain.BaseDirectory;
        //    RunCommand(pythonInstallerPath, $".\\{"python-3.13.0-amd64.exe"} /quiet InstallAllUsers=1 PrependPath=1 Include_test=0 DefaultAllUsersTargetDir=%LocalAppData%\\Programs\\Python");

        //    string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        //    string pythonEnvironmentVariable1 = localFolder + "\\Programs\\Python\\Python313\\Scripts\\";
        //    string pythonEnvironmentVariable2 = localFolder + "\\Programs\\Python\\Python313\\";
        //    string pythonEnvironmentVariable3 = localFolder + "\\Programs\\Python\\Launcher\\";

        //    AddNewEnvironmentVariable(pythonEnvironmentVariable1);
        //    AddNewEnvironmentVariable(pythonEnvironmentVariable2);
        //    AddNewEnvironmentVariable(pythonEnvironmentVariable3);
        //}

        //public static void InstallFFsubsync()
        //{
        //    string pythonYolu = GetPythonInstallPath();
        //    string output = string.Empty;
        //    string error = string.Empty;

        //    Process p = new Process();
        //    p.StartInfo.FileName = pythonYolu;
        //    p.StartInfo.Arguments = "-m pip install ffsubsync";
        //    p.StartInfo.UseShellExecute = false;
        //    p.StartInfo.RedirectStandardOutput = true; // Standart çıktıyı al
        //    p.StartInfo.RedirectStandardError = true;  // Hata çıktısını al
        //    p.StartInfo.CreateNoWindow = true;         // Yeni pencere açma

        //    try
        //    {
        //        p.Start();

        //        // Çıktıları asenkron olarak oku
        //        output = p.StandardOutput.ReadToEnd();
        //        error = p.StandardError.ReadToEnd();

        //        p.WaitForExit();

        //        // Komut başarıyla tamamlandığında
        //        if (p.ExitCode == 0)
        //        {
        //            Log.Information("Successfull installation");
        //        }
        //        else
        //        {
        //            Log.Error(error);
        //            if (error.Contains("Microsoft Visual C++ 14.0 or greater is required"))
        //            {
        //                InstallBuildTools();
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error(ex.Message);
        //        if (error.Contains("Microsoft Visual C++ 14.0 or greater is required"))
        //        {
        //            InstallBuildTools();
        //        }
        //    }
        //}

        //private static bool installedBuildTools = false;
        //static void InstallBuildTools()
        //{
        //    if (!installedBuildTools)
        //    {
        //        string buildToolsUrl = "https://aka.ms/vs/17/release/vs_buildtools.exe";
        //        string buildToolsPath = Path.Combine(Path.GetTempPath(), "vs_buildtools.exe");
        //        Log.Information("Downloading Visual Studio Build Tools...");

        //        using (WebClient client = new WebClient())
        //        {
        //            client.DownloadFile(buildToolsUrl, buildToolsPath);
        //        }

        //        Log.Information("Downloaded Build Tools. Starting installation...");

        //        // Gerekli bileşenleri ekle
        //        string buildToolsArgs = "--quiet --wait --norestart " +
        //                                "--add Microsoft.VisualStudio.Workload.VCTools " +
        //                                "--add Microsoft.VisualStudio.Component.Windows10SDK.19041 " +
        //                                "--add Microsoft.VisualStudio.Component.VC.ATLMFC " +
        //                                "--add Microsoft.VisualStudio.Component.VC.CMake.Project " +
        //                                "--add Microsoft.VisualStudio.Component.VC.Llvm.ClangToolset " +
        //                                "--add Microsoft.VisualStudio.Component.VC.CoreBuildTools " +
        //                                "--add Microsoft.VisualStudio.Component.Windows81SDK";


        //        var r = RunProcess(buildToolsPath, buildToolsArgs);

        //        if (r)
        //        {
        //            installedBuildTools = true;
        //            InstallFFsubsync();
        //        }
        //        Log.Information("Build Tools have been installed successfully");
        //    }
           
        //}



        static bool RunProcess(string fileName, string arguments)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Log.Information("Output: " + output);
                if (!string.IsNullOrWhiteSpace(error))
                    Log.Error("Error: " + error);

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Command unsuccess: {error}");
                }

                return true;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return false;
            }
        }

        public static string RunCommand(string where, string command)
        {
            string output = string.Empty;
            string error = string.Empty;

            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/C cd " + where + " & " + command;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true; // Standart çıktıyı al
            p.StartInfo.RedirectStandardError = true;  // Hata çıktısını al
            p.StartInfo.CreateNoWindow = true;         // Yeni pencere açma

            // Komutu çalıştır
            try
            {
                p.Start();

                // Çıktıları asenkron olarak oku
                output = p.StandardOutput.ReadToEnd();
                error = p.StandardError.ReadToEnd();

                p.WaitForExit();

                // Komut başarıyla tamamlandığında
                if (p.ExitCode == 0)
                {
                    Log.Information("Successfull installation");
                    return $"Başarıyla tamamlandı: {output}";
                }
                else
                {
                    Log.Error(error);
                    return $"Hata oluştu: {error}";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return $"Bir hata oluştu: {ex.Message}";
            }
        }

        //public static void runCommand(string where, string command, bool admin = false)
        //{
        //    const int ERROR_CANCELLED = 1223;

        //    Process p = new Process();
        //    p.StartInfo.FileName = "cmd.exe";
        //    if (admin)
        //    {
        //        p.StartInfo.UseShellExecute = true;
        //        p.StartInfo.Verb = "runas";
        //    }
        //    p.StartInfo.Arguments = "/C cd " + where + " &" + command;

        //    if (admin)
        //    {
        //        try
        //        {
        //            p.Start();
        //            p.WaitForExit();
        //        }
        //        catch (Win32Exception ex)
        //        {
        //            if (ex.NativeErrorCode == ERROR_CANCELLED)
        //            {
        //                new CustomMessageBox("This operation needs administrator permission.", MessageType.Error, MessageButtons.Ok).ShowDialog();
        //                Application.Current.Shutdown();
        //            }
        //            else
        //                throw;
        //        }
        //    }
        //    else
        //    {
        //        p.Start();
        //        p.WaitForExit();
        //    }
        //}

        //public static void AddNewEnvironmentVariable(string variable)
        //{
        //    string path = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
        //    string newEnvironmentVariable = ";" + variable;
        //    Environment.SetEnvironmentVariable("Path", path + newEnvironmentVariable, EnvironmentVariableTarget.User);
        //}

        //public static bool IsPythonReadyToUse()
        //{
        //    return !String.IsNullOrWhiteSpace(PythonVersion());
        //}

        //public static void InstallFFmpeg()
        //{
        //    string ffmpeg = AppDomain.CurrentDomain.BaseDirectory + "ffmpeg\\bin";
        //    AddNewEnvironmentVariable(ffmpeg);
        //}

        //public static void InstallEssentials()
        //{
        //    if (!IsPythonReadyToUse())
        //    {
        //        InstallPython();
        //        InstallFFsubsync();
        //        InstallFFmpeg();
        //        Log.Information("Installed essentials");
        //    }
        //}

        //public static void InstallQBittorrent()
        //{
        //    string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "qBittorrent");
        //    if (Directory.Exists(path))
        //    {
        //        File.Copy("qBittorrent.ini", System.IO.Path.Combine(path, "qBittorrent.ini"), true);
        //    }
        //    else
        //    {
        //        Directory.CreateDirectory(path);
        //        File.Copy("qBittorrent.ini", System.IO.Path.Combine(path, "qBittorrent.ini"), true);
        //    }
        //    var qbittorentInstallerPath = AppDomain.CurrentDomain.BaseDirectory;
        //    RunCommand(qbittorentInstallerPath, $".\\{"qbittorrent_5.0.3_x64_setup.exe"} /S");
        //}

        //public static Dictionary<bool, string> checkInstalled(string c_name)
        //{
        //    string displayName;

        //    string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        //    RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey);
        //    if (key != null)
        //    {
        //        foreach (RegistryKey subkey in key.GetSubKeyNames().Select(keyName => key.OpenSubKey(keyName)))
        //        {
        //            displayName = subkey.GetValue("DisplayName") as string;

        //            if (displayName != null && displayName.Contains(c_name))
        //            {
        //                var uninstallstrin = (subkey.GetValue("UninstallString") as string).Replace("\"", "");
        //                var directory = Directory.GetParent(uninstallstrin);
        //                if (directory != null)
        //                {
        //                    return new Dictionary<bool, string>() { { true, directory.GetFiles().FirstOrDefault(x => x.FullName.Contains("qbittorrent.exe")).FullName } };
        //                }
        //            }
        //        }
        //        key.Close();
        //    }

        //    registryKey = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
        //    key = Registry.LocalMachine.OpenSubKey(registryKey);
        //    if (key != null)
        //    {
        //        foreach (RegistryKey subkey in key.GetSubKeyNames().Select(keyName => key.OpenSubKey(keyName)))
        //        {
        //            displayName = subkey.GetValue("DisplayName") as string;

        //            if (displayName != null && displayName.Contains(c_name))
        //            {
        //                var uninstallstrin = (subkey.GetValue("UninstallString") as string).Replace("\"", "");
        //                var directory = Directory.GetParent(uninstallstrin);
        //                if (directory != null)
        //                {
        //                    return new Dictionary<bool, string>() { { true, directory.GetFiles().FirstOrDefault(x => x.FullName.Contains("qbittorrent.exe")).FullName } };
        //                }
        //            }
        //        }
        //        key.Close();
        //    }
        //    return null;
        //}
        //public static string GetFFSubSyncInstallPath()
        //{
        //    var pythonExePath = GetPythonInstallPath();
        //    if (String.IsNullOrWhiteSpace(pythonExePath)) return "";
        //    var pythonDirectory = Path.GetDirectoryName(pythonExePath);
        //    string scriptsPath = Path.Combine(pythonDirectory, "Scripts");
        //    string ffsubsyncPath = Path.Combine(scriptsPath, "ffsubsync.exe");
        //    if (File.Exists(ffsubsyncPath))
        //    {
        //        return ffsubsyncPath;
        //    }
        //    else
        //    {
        //        return "";
        //    }
        //}
        //public static string GetPythonInstallPath()
        //{

        //    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Python\PythonCore"))
        //    {
        //        if (key != null)
        //        {
        //            foreach (var version in key.GetSubKeyNames())
        //            {
        //                using (RegistryKey installKey = key.OpenSubKey(version + @"\InstallPath"))
        //                {
        //                    string pythonPath = installKey?.GetValue("")?.ToString();
        //                    if (!string.IsNullOrEmpty(pythonPath))
        //                        return System.IO.Path.Combine(pythonPath, "python.exe");
        //                }
        //            }
        //        }
        //    }

        //    return "";
        //}
        //public static bool IsQBittorrentInstalled()
        //{
        //    var qbittorrent = checkInstalled("qBittorrent");
        //    if (qbittorrent != null)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        //public static void RunQBittorrent()
        //{
        //    var qbittorrent = checkInstalled("qBittorrent");
        //    if (qbittorrent != null)
        //    {
        //        if (!(Process.GetProcesses().Any(x => x.ProcessName == "qbittorrent")))
        //        {
        //            Process process = new Process();
        //            process.StartInfo.FileName = qbittorrent.FirstOrDefault().Value; // QBittorrent'in yolu
        //            process.StartInfo.Arguments = "";  // Web UI'nin aktif olabilmesi için argüman eklemeye gerek yok
        //            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;  // Pencereyi gizlemek için
        //            process.StartInfo.UseShellExecute = false;
        //            process.StartInfo.RedirectStandardOutput = true;
        //            process.StartInfo.RedirectStandardError = true;
        //            process.Start();
        //        }
        //    }
        //    //else
        //    //{
        //    //    InstallQBittorrent();
        //    //    qbittorrent = checkInstalled("qBittorrent");
        //    //    if (qbittorrent != null)
        //    //    {
        //    //        if (!(Process.GetProcesses().Any(x => x.ProcessName == "qbittorrent")))
        //    //        {
        //    //            Process.Start(qbittorrent.FirstOrDefault().Value);
        //    //        }
        //    //    }
        //    //}
        //}

        public static void InstallJackett()
        {
            try
            {
                var destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett\\Indexers");
                var destinationFolder2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett");

                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                }

                var files = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JacketConfig\\Indexers"));

                foreach (var file in files)
                {
                    File.Copy(file, Path.Combine(destinationFolder, Path.GetFileName(file)), true);
                }

                var serviceConfigFile = "JacketConfig\\ServerConfig.json";
                File.Copy(serviceConfigFile, Path.Combine(destinationFolder2, "ServerConfig.json"),true);

                AppSettingsManager.appSettings.JacketApiUrl = "http://127.0.0.1:9117/";
                AppSettingsManager.appSettings.JacketApiKey = "htxi2hsv7gp3b245n5mcf8kz7vwf5sp4";
                AppSettingsManager.SaveAppSettings();

                InstallSilentlyJackett();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }


        public static void InstallSilentlyJackett()
        {
            try
            {
                if (IsJackettInstalled())
                {
                    return;
                }

                string installerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Jackett.Installer.Windows.exe");
                if (!File.Exists(installerPath))
                {
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/SILENT", 
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error during installation: " + e.Message);
            }
        }

        public static bool IsJackettInstalled()
        {
            // Method 1: Check for Jackett service using sc query command
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "query Jackett",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // If the service exists, the output will contain "SERVICE_NAME: Jackett"
                    if (output.Contains("SERVICE_NAME") && output.Contains("Jackett"))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore errors from sc command
            }

            // Method 2: Check common installation directories
            string[] possiblePaths = new string[]
            {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Jackett"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Jackett"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jackett")
            };

            foreach (string path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    return true;
                }
            }

            // Method 3: Check for Jackett processes
            Process[] processes = Process.GetProcessesByName("Jackett");
            if (processes.Length > 0)
            {
                return true;
            }

            // Method 4: Check Windows registry for Jackett
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (string subkeyName in key.GetSubKeyNames())
                        {
                            using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                            {
                                if (subkey != null)
                                {
                                    string displayName = subkey.GetValue("DisplayName") as string;
                                    if (!string.IsNullOrEmpty(displayName) && displayName.Contains("Jackett"))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore registry access errors
            }

            return false;
        }

        public static bool StartJackettService()
        {
            try
            {
                // First check if the service exists and get its status
                ProcessStartInfo checkPsi = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "query Jackett",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                bool serviceExists = false;
                bool serviceRunning = false;

                using (Process checkProcess = Process.Start(checkPsi))
                {
                    string output = checkProcess.StandardOutput.ReadToEnd();
                    checkProcess.WaitForExit();

                    if (output.Contains("SERVICE_NAME") && output.Contains("Jackett"))
                    {
                        serviceExists = true;
                        serviceRunning = output.Contains("RUNNING");
                    }
                }

                // If service exists but is not running, start it
                if (serviceExists && !serviceRunning)
                {
                    ProcessStartInfo startPsi = new ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = "start Jackett",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using (Process startProcess = Process.Start(startPsi))
                    {
                        startProcess.WaitForExit();
                        return startProcess.ExitCode == 0;
                    }
                }
                else if (serviceExists && serviceRunning)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

            return false;
        }

        //public static InstallerPage GetInstallerState()
        //{
        //    var pythonInstalled = Essentials.IsPythonReadyToUse();
        //    var qBittorrentInstalled = Essentials.IsQBittorrentInstalled();
        //    var ffsubsyncInstalled = Essentials.GetFFSubSyncInstallPath() != "";
        //    if (pythonInstalled && qBittorrentInstalled && ffsubsyncInstalled)
        //    {
        //        return InstallerPage.Done;
        //    }
        //    else 
        //    {
        //        return InstallerPage.Main;
        //    }
        //}

    }
}
