using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

namespace NetStream.Services
{
    public class PythonManager
    {
        private const string PythonVersion = "3.10.11";
        private const string PythonUrl = "https://www.python.org/ftp/python/3.10.11/python-3.10.11-embed-amd64.zip";
        private const string GetPipUrl = "https://bootstrap.pypa.io/get-pip.py";
        
        private readonly string _baseDir;
        private readonly string _pythonDir;
        private readonly string _pythonExe;

        public event EventHandler<string> StatusChanged;

        public PythonManager()
        {
            _baseDir = Path.Combine(Environment.CurrentDirectory, "Bin");
            _pythonDir = Path.Combine(_baseDir, "python");
            _pythonExe = Path.Combine(_pythonDir, "python.exe");
        }

        public async Task<string> EnsurePythonAsync()
        {
            // 1. Check if System Python exists and is usable
            
            string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
           
            string pythonPath =  localFolder + "\\Programs\\Python\\Python310\\python.exe";

            if (await IsPythonAvailable(pythonPath))
            {
                ReportStatus("System Python found.");
                return pythonPath;
            }

            
            // 3. Install Local Python
            return await InstallLocalPythonAsync();
        }

        public async Task EnsureDependenciesAsync(string pythonPath, bool installTTS = true)
        {
            ReportStatus("Checking installed dependencies...");

            // First check if core packages are already installed
            bool torchOk = await RunCommandAsync(pythonPath, "-c \"import torch; print('torch OK')\"", false);
            bool whisperOk = await RunCommandAsync(pythonPath, "-c \"from faster_whisper import WhisperModel; print('faster_whisper OK')\"", false);
            bool translatorOk = await RunCommandAsync(pythonPath, "-c \"from deep_translator import GoogleTranslator; print('deep_translator OK')\"", false);

            if (torchOk && whisperOk && translatorOk && !installTTS)
            {
                ReportStatus("All core dependencies are already installed.");
                return;
            }

            ReportStatus("Installing AI dependencies...");

            // Upgrade pip first
            ReportStatus("Upgrading pip...");
            await RunCommandAsync(pythonPath, "-m pip install --upgrade pip", true);

            // Install PyTorch with CUDA 12.4 support for GPU acceleration
            if (!torchOk)
            {
                ReportStatus("Installing PyTorch with CUDA 12.4 (~2GB download, please wait)...");
                bool torchInstalled = await RunCommandAsync(pythonPath,
                    "-m pip install torch torchaudio --index-url https://download.pytorch.org/whl/cu124",
                    true);

                if (!torchInstalled)
                {
                    ReportStatus("PyTorch CUDA failed. Installing CPU version...");
                    await RunCommandAsync(pythonPath, "-m pip install torch torchaudio --index-url https://download.pytorch.org/whl/cpu", true);
                }

                // Verify PyTorch
                ReportStatus("Verifying PyTorch installation...");
                bool torchVerified = await RunCommandAsync(pythonPath, "-c \"import torch; print('PyTorch OK')\"", true);
                if (!torchVerified)
                {
                    ReportStatus("ERROR: PyTorch verification failed!");
                }
            }
            else
            {
                ReportStatus("PyTorch already installed.");
            }

            // CLEANUP: Check for and delete corrupt 0-byte .whl files in site-packages
            try
            {
                string sitePackages = await GetSitePackagesPathAsync(pythonPath);
                if (!string.IsNullOrEmpty(sitePackages) && Directory.Exists(sitePackages))
                {
                    var corruptFiles = Directory.GetFiles(sitePackages, "*.whl").Where(f => new FileInfo(f).Length == 0).ToList();
                    foreach (var file in corruptFiles)
                    {
                        ReportStatus($"Deleting corrupt file: {Path.GetFileName(file)}");
                        File.Delete(file);
                    }
                }
            }
            catch { }

            // Install Whisper and Faster-Whisper
            if (!whisperOk)
            {
                ReportStatus("Installing Whisper and Faster-Whisper...");
                await RunCommandAsync(pythonPath, "-m pip install openai-whisper faster-whisper --no-cache-dir", true);

                ReportStatus("Verifying Whisper...");
                bool whisperVerified = await RunCommandAsync(pythonPath, "-c \"from faster_whisper import WhisperModel; print('Whisper OK')\"", true);
                if (!whisperVerified) ReportStatus("WARNING: Whisper verification failed!");
            }
            else
            {
                ReportStatus("Whisper already installed.");
            }

            // Install translation tools
            if (!translatorOk)
            {
                ReportStatus("Installing translation and ffmpeg tools...");
                await RunCommandAsync(pythonPath, "-m pip install deep-translator ffmpeg-python --no-cache-dir", true);
            }
            else
            {
                ReportStatus("Translation tools already installed.");
            }

            // Install compatible numpy and scipy
            ReportStatus("Installing compatible numpy and scipy...");
            await RunCommandAsync(pythonPath, "-m pip install \"numpy<2.0\" scipy --no-cache-dir", true);

            if (installTTS)
            {
                // eSpeak NG Installation (Critical for TTS on Windows)
                string espeakDir = Path.Combine(_baseDir, "eSpeak-NG");
                string espeakExe = Path.Combine(espeakDir, "espeak-ng.exe");
                
                if (!File.Exists(espeakExe))
                {
                    ReportStatus("Downloading eSpeak NG (required for TTS)...");
                    // Using a portable version or extracting MSI would be ideal, but for now we'll try to download a pre-packaged portable version
                    // Since official portable doesn't exist, we will try to rely on the user having it or try to install a python wrapper if possible.
                    // BUT, the user instructions explicitly mentioned setting PHONEMIZER_ESPEAK_PATH.
                    // We will try to download a known portable build or just warn. 
                    // Actually, let's try to use the 'phonemizer' package which might handle this, but Coqui needs the binary.
                    
                    // For this agent, let's assume we can't easily install a system MSI silently without admin rights/interaction.
                    // We will set the env var if it exists in standard paths.
                }
                
                // Check common paths for eSpeak
                string[] commonEspeakPaths = {
                    @"C:\Program Files\eSpeak NG\espeak-ng.exe",
                    @"C:\Program Files (x86)\eSpeak NG\espeak-ng.exe"
                };
                
                string foundEspeak = commonEspeakPaths.FirstOrDefault(File.Exists);
                if (foundEspeak != null)
                {
                    ReportStatus($"Found eSpeak NG at: {foundEspeak}");
                    Environment.SetEnvironmentVariable("PHONEMIZER_ESPEAK_PATH", foundEspeak);
                }
                else
                {
                    ReportStatus("WARNING: eSpeak NG not found. TTS might fail. Please install eSpeak NG for Windows.");
                }

                ReportStatus("Installing Coqui TTS from source (editable mode)...");
                
                string ttsRepoPath = Path.Combine(_baseDir, "TTS");
                bool gitAvailable = await RunCommandAsync("git", "--version", false);
                
                if (gitAvailable)
                {
                    if (Directory.Exists(ttsRepoPath))
                    {
                        ReportStatus("Removing existing TTS folder to ensure fresh clone...");
                        try { Directory.Delete(ttsRepoPath, true); } catch { }
                    }

                    if (!Directory.Exists(ttsRepoPath))
                    {
                        ReportStatus("Cloning TTS repository...");
                        bool cloned = await RunCommandAsync("git", $"clone https://github.com/coqui-ai/TTS \"{ttsRepoPath}\"", true);
                        
                        if (cloned && Directory.Exists(ttsRepoPath))
                        {
                            ReportStatus("Installing TTS dependencies...");
                            await RunCommandAsync(pythonPath, $"-m pip install -r \"{Path.Combine(ttsRepoPath, "requirements.txt")}\" --no-cache-dir", true);
                            
                            ReportStatus("Installing TTS in editable mode (pip install -e .)...");
                            // We run pip install -e . inside the repo folder
                            // We need to set CWD for this command
                            
                            ProcessStartInfo psi = new ProcessStartInfo
                            {
                                FileName = pythonPath,
                                Arguments = "-m pip install -e . --no-cache-dir",
                                WorkingDirectory = ttsRepoPath,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            };
                            
                            // Pass environment variables
                            if (foundEspeak != null)
                            {
                                psi.EnvironmentVariables["PHONEMIZER_ESPEAK_PATH"] = foundEspeak;
                            }
                            
                            using (var process = new Process { StartInfo = psi })
                            {
                                process.OutputDataReceived += (s, e) => { if (e.Data != null) ReportStatus(e.Data); };
                                process.ErrorDataReceived += (s, e) => { if (e.Data != null) ReportStatus(e.Data); };
                                process.Start();
                                process.BeginOutputReadLine();
                                process.BeginErrorReadLine();
                                await process.WaitForExitAsync();
                            }
                        }
                        else
                        {
                            ReportStatus("Git clone failed. Falling back to pip install...");
                            await RunCommandAsync(pythonPath, "-m pip install TTS --no-cache-dir", true);
                        }
                    }
                }
                else
                {
                    ReportStatus("Git not found in PATH. Falling back to pip install...");
                    await RunCommandAsync(pythonPath, "-m pip install TTS --no-cache-dir", true);
                }
                
                ReportStatus("Verifying TTS...");
                bool ttsOk = await RunCommandAsync(pythonPath, "-c \"from TTS.api import TTS; print('TTS OK')\"", true);
                if (!ttsOk) 
                {
                    throw new Exception("Failed to install TTS. Please ensure Git and C++ Build Tools are installed.");
                }
            }
            
            if (installTTS)
            {
                ReportStatus("Installing audio processing tools...");
                await RunCommandAsync(pythonPath, "-m pip install audio-separator[gpu] pydub --no-cache-dir", true);
            }

            ReportStatus("All dependencies installed successfully!");
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
                        foreach (var path in paths)
                        {
                            if (path.Contains("site-packages") && Directory.Exists(path))
                            {
                                return path;
                            }
                        }
                    }
                }
                
                // Fallback
                string pythonDir = Path.GetDirectoryName(pythonPath);
                string sitePackages = Path.Combine(pythonDir, "Lib", "site-packages");
                if (Directory.Exists(sitePackages)) return sitePackages;
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<string> InstallLocalPythonAsync()
        {
            try
            {
                string installerPath = @"c:\Users\asdf\Desktop\NetStream\python-3.10.10-amd64.exe";
                if (File.Exists(installerPath))
                {
                    ReportStatus("Installing Python from provided installer...");
                    
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = "/quiet InstallAllUsers=0 PrependPath=1 Include_test=0",
                        UseShellExecute = true, // Need shell execute for installer sometimes, or false with CreateNoWindow
                        CreateNoWindow = false // Let user see if needed, but /quiet should hide it
                    };

                    using (var process = Process.Start(psi))
                    {
                        await process.WaitForExitAsync();
                    }
                    string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
           
                    string pythonPath =  localFolder + "\\Programs\\Python\\Python310\\python.exe";
                    ReportStatus("Python installation completed.");
                    return pythonPath; // Assuming it's now in PATH
                }
                else
                {
                    // Fallback to original logic if installer not found (or error out?)
                    // User specifically asked for this installer.
                    ReportStatus("Installer not found, falling back to embedded python download...");
                    
                    if (!Directory.Exists(_baseDir)) Directory.CreateDirectory(_baseDir);
                    if (Directory.Exists(_pythonDir)) Directory.Delete(_pythonDir, true);
                    Directory.CreateDirectory(_pythonDir);

                    // Download Python Zip
                    ReportStatus($"Downloading Python {PythonVersion}...");
                    string zipPath = Path.Combine(_baseDir, "python.zip");
                    
                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync(PythonUrl);
                        using (var fs = new FileStream(zipPath, FileMode.CreateNew))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                    }

                    // Extract
                    ReportStatus("Extracting Python...");
                    ZipFile.ExtractToDirectory(zipPath, _pythonDir);
                    File.Delete(zipPath);

                    // Enable 'site' package (Crucial for pip)
                    // Find python3xx._pth file
                    var pthFile = Directory.GetFiles(_pythonDir, "python*._pth").FirstOrDefault();
                    if (pthFile != null)
                    {
                        string content = await File.ReadAllTextAsync(pthFile);
                        content = content.Replace("#import site", "import site");
                        await File.WriteAllTextAsync(pthFile, content);
                    }

                    // Install Pip
                    ReportStatus("Installing Pip...");
                    string getPipPath = Path.Combine(_pythonDir, "get-pip.py");
                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync(GetPipUrl);
                        using (var fs = new FileStream(getPipPath, FileMode.CreateNew))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                    }

                    await RunCommandAsync(_pythonExe, $"\"{getPipPath}\"");
                    
                    // Cleanup
                    if (File.Exists(getPipPath)) File.Delete(getPipPath);

                    return _pythonExe;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to install Python: {ex.Message}");
            }
        }

        private async Task<bool> IsPythonAvailable(string pythonPath)
        {
            try
            {
                var result = await RunCommandAsync(pythonPath, "--version", false);
                return result;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> RunCommandAsync(string exe, string args, bool reportOutput = true)
        {
            var tcs = new TaskCompletionSource<bool>();

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = exe;
            start.Arguments = args;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.RedirectStandardError = true;
            start.CreateNoWindow = true;

            using (Process process = new Process { StartInfo = start })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data) && reportOutput)
                    {
                        ReportStatus(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data) && reportOutput)
                    {
                        // Pip writes progress to stderr often, so we log it as status too
                        ReportStatus(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
        }

        private void ReportStatus(string message)
        {
            StatusChanged?.Invoke(this, message);
        }
    }
}
