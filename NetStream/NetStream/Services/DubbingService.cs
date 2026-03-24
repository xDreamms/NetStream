using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace NetStream.Services
{
    public class DubbingService
    {
        // This service is designed to orchestrate the AI Dubbing pipeline.
        // It currently simulates the process but is structured to call external Python scripts.

        public event EventHandler<string> StatusChanged;
        public event EventHandler<double> ProgressChanged;

        public async Task StartDubbingAsync(string videoPath, string targetLanguage)
        {
            try
            {
                string scriptPath = Path.Combine(Environment.CurrentDirectory, "Scripts", "dubbing_pipeline.py");
                string outputDir = Path.Combine(Environment.CurrentDirectory, "DubbedOutput");
                
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                // 1. Setup Python Environment
                var pythonManager = new PythonManager();
                pythonManager.StatusChanged += (s, msg) => ReportProgress(msg, 0);
                
                string pythonPath = await pythonManager.EnsurePythonAsync();
                await pythonManager.EnsureDependenciesAsync(pythonPath);

                // 2. Run Pipeline
                // First, get the site-packages path
                string sitePackagesPath = await GetSitePackagesPathAsync(pythonPath);
                
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = pythonPath;
                start.Arguments = $"\"{scriptPath}\" --input_video \"{videoPath}\" --target_language \"{targetLanguage}\" --output_dir \"{outputDir}\"";
                
                // Set PYTHONPATH to include site-packages
                if (!string.IsNullOrEmpty(sitePackagesPath))
                {
                    start.EnvironmentVariables["PYTHONPATH"] = sitePackagesPath;
                }
                
                Console.WriteLine(scriptPath);
                Console.WriteLine(videoPath);
                Console.WriteLine(targetLanguage);
                Console.WriteLine(outputDir);
                Console.WriteLine($"Site-packages: {sitePackagesPath}");
                
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;
                start.CreateNoWindow = true;

                using (Process process = new Process { StartInfo = start })
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine($"Python Output: {e.Data}");
                            if (e.Data.StartsWith("PROGRESS:"))
                            {
                                // Format: PROGRESS:50|Message
                                try
                                {
                                    var parts = e.Data.Split('|');
                                    var progressPart = parts[0].Split(':')[1];
                                    var message = parts[1];
                                    
                                    if (double.TryParse(progressPart, out double progress))
                                    {
                                        ReportProgress(message, progress);
                                    }
                                }
                                catch { }
                            }
                        }
                    };
                    
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine($"Python Error: {e.Data}");
                            // Report error to UI so user can see why it failed
                            ReportProgress($"Error: {e.Data}", 0);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        ReportProgress("Dubbing Completed Successfully!", 100);
                    }
                    else
                    {
                        ReportProgress("Dubbing Failed. Check logs.", 0);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportProgress($"Error: {ex.Message}", 0);
            }
        }

        private void ReportProgress(string status, double progress)
        {
            StatusChanged?.Invoke(this, status);
            ProgressChanged?.Invoke(this, progress);
        }

        private async Task<string> GetSitePackagesPathAsync(string pythonPath)
        {
            try
            {
                // For embedded Python, try to get the actual site-packages path
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = pythonPath;
                psi.Arguments = "-c \"import site; print(';'.join(site.getsitepackages()))\"";
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        var paths = output.Trim().Split(';');
                        // Return the first valid path that contains "site-packages"
                        foreach (var path in paths)
                        {
                            if (path.Contains("site-packages") && Directory.Exists(path))
                            {
                                return path;
                            }
                        }
                    }
                }
                
                // Fallback: construct path manually for embedded Python
                string pythonDir = Path.GetDirectoryName(pythonPath);
                string sitePackages = Path.Combine(pythonDir, "Lib", "site-packages");
                
                if (Directory.Exists(sitePackages))
                {
                    return sitePackages;
                }
                
                return string.Empty;
            }
            catch
            {
                // Final fallback
                try
                {
                    string pythonDir = Path.GetDirectoryName(pythonPath);
                    return Path.Combine(pythonDir, "Lib", "site-packages");
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
    }
}
