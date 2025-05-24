using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SingularityGroup.HotReload.Editor.Cli;
using SingularityGroup.HotReload.Newtonsoft.Json;
using UnityEngine;

namespace SingularityGroup.HotReload.Editor {
    internal class ServerDownloader {
        public float Progress {get; private set;}

        class Config {
            public Dictionary<string, string> customServerExecutables;
        }
        
        public async Task EnsureDownloaded(ICliController cliController, CancellationToken cancellationToken) {
            var targetDir = CliUtils.GetExecutableTargetDir();
            var targetPath = Path.Combine(targetDir, cliController.BinaryFileName);
            if(File.Exists(targetPath)) {
                Progress = 1f;
                return;
            }
            Progress = 0f;
            await ThreadUtility.SwitchToThreadPool(cancellationToken);

            Directory.CreateDirectory(targetDir);
            if(TryUseUserDefinedBinaryPath(cliController, targetPath)) {
                Progress = 1f;
                return;
            }

            var attempt = 0;
            string tmpPath = null;
            while(tmpPath == null) {
                try {
                    if(File.Exists(targetPath)) {
                        Progress = 1f;
                        return;
                    }
                    tmpPath = await Download(cliController, cancellationToken).ConfigureAwait(false);
                } catch {
                    //ignored
                }
                if(tmpPath == null) {
                    await Task.Delay(ExponentialBackoff.GetTimeout(attempt), cancellationToken).ConfigureAwait(false);
                }
                attempt++;
            }
            
            const int ERROR_ALREADY_EXISTS = 0xB7;
            try {
                File.Move(tmpPath, targetPath);
            } catch(IOException ex) when((ex.HResult & 0x0000FFFF) == ERROR_ALREADY_EXISTS) {
                //another downloader came first
                try {
                    File.Delete(tmpPath); 
                } catch {
                    //ignored 
                }
            }
            Progress = 1f;
        }

        static bool TryUseUserDefinedBinaryPath(ICliController cliController, string targetPath) {
            if (!File.Exists(PackageConst.ConfigFileName)) {
                return false;
            } 
            
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(PackageConst.ConfigFileName));
            var customExecutables = config?.customServerExecutables;
            if (customExecutables == null) {
                return false;
            } 
            if(!customExecutables.TryGetValue(cliController.PlatformName, out var customBinaryPath)) {
                return false;
            }
            
            if (!File.Exists(customBinaryPath)) {
                Debug.LogWarning($"unable to find server binary for platform '{cliController.PlatformName}' at '{customBinaryPath}'. " +
                                 $"Will proceed with downloading the binary (default behavior)");
                return false;
            } 
            
            try {
                File.Copy(customBinaryPath, targetPath);
                return true;
            } catch(IOException ex) {
                Debug.LogWarning($"encountered exception when copying server binary in the specified custom executable path '{customBinaryPath}':\n{ex}");
                return false;
            }
        }

        async Task<string> Download(ICliController cliController, CancellationToken cancellationToken) {
            var version = PackageConst.Version.Replace('.', '-');
            var url = $"https://hot-reload.b-cdn.net/releases/{version}/server/{cliController.PlatformName}/{cliController.BinaryFileName}";
            var tmpFile = CliUtils.GetTempDownloadFilePath();
            var tmpDir = Path.GetDirectoryName(tmpFile);
            Directory.CreateDirectory(tmpDir);
            using(var client = new HttpClient()) {
                client.Timeout = TimeSpan.FromMinutes(10);
                var success = await client.DownloadAsync(url, tmpFile, new Progress<float>(f => Progress = f), cancellationToken).ConfigureAwait(false);
                if(!success) {
                    return null;
                }
            }
            return tmpFile;
        }
    }
    
    static class HttpClientExtensions {
        public static async Task<bool> DownloadAsync(this HttpClient client, string requestUri, string destinationFilePath, IProgress<float> progress, CancellationToken cancellationToken = default(CancellationToken)) {
            // Get the http headers first to examine the content length
            using (var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)) {
                if(response.StatusCode != HttpStatusCode.OK) {
                    return false;
                }
                var contentLength = response.Content.Headers.ContentLength;
    
                using (var fs = new FileStream(destinationFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                using (var download = await response.Content.ReadAsStreamAsync()) {
    
                    // Ignore progress reporting when no progress reporter was 
                    // passed or when the content length is unknown
                    if (progress == null || !contentLength.HasValue) {
                        await download.CopyToAsync(fs);
                        return true;
                    }
    
                    // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
                    var relativeProgress = new Progress<long>(totalBytes => progress.Report((float)totalBytes / contentLength.Value));
                    // Use extension method to report progress while downloading
                    await download.CopyToAsync(fs, 81920, relativeProgress, cancellationToken);
                    progress.Report(1);
                    return true;
                }
            }
        }
        
        static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long> progress, CancellationToken cancellationToken) {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (!source.CanRead)
                throw new ArgumentException("Has to be readable", nameof(source));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite)
                throw new ArgumentException("Has to be writable", nameof(destination));
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
    
            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0) {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }
    }
    
}
