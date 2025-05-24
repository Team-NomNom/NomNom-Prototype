using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SingularityGroup.HotReload.DTO;
using SingularityGroup.HotReload.Editor.Cli;
using UnityEditor;
using UnityEngine;

namespace SingularityGroup.HotReload.Editor {
    [InitializeOnLoad]
    public static class EditorCodePatcher {
        const string sessionFilePath = PackageConst.LibraryCachePath + "/sessionId.txt";
        
        internal static readonly ServerDownloader serverDownloader;

        static Timer timer; 
        static readonly string PatchFilePath = null;
        
        static EditorCodePatcher() {
            UnityHelper.Init();
            //Use synchonization context if possible because it's more reliable.
            ThreadUtility.InitEditor();
            if (!EditorWindowHelper.IsHumanControllingUs()) {
                return;
            }
            
            serverDownloader = new ServerDownloader();
            timer = new Timer(OnIntervalThreaded, (Action) OnIntervalMainThread, 500, 500);

            UpdateHost();
            PatchFilePath = PersistencePaths.GetPatchesFilePath(Application.persistentDataPath);
            var compileChecker = CompileChecker.Create();
            compileChecker.onCompilationFinished += OnCompilationFinished;

            EditorApplication.delayCall += InstallUtility.CheckForNewInstall;
            // When domain reloads, this is a good time to ensure server has up-to-date project information
            EditorApplication.delayCall += TryPostFilesState;
            DetectEditorStart();
            serverDownloader.EnsureDownloaded(HotReloadCli.controller, CancellationToken.None).Forget();
        }

        private static DateTime lastPostFilesState = DateTime.UtcNow;

        /// Post state for player builds.
        /// Only check build target because user can change build settings whenever.
        internal static void TryPostFilesState() {
            // Note: we post files state even when build target is wrong
            // because you might connect with a build downloaded onto the device. 
            if ((DateTime.UtcNow - lastPostFilesState).TotalSeconds > 5) {
                lastPostFilesState = DateTime.UtcNow;
                PostFilesState();
            }
        }

        internal static void PostFilesState() {
            // absoluteFilePath uniquely identifies the Unity project.
            // its the filepath to /MyProject/Assets/ (editor Application.dataPath)
            // Editor POST /files  with a json body { absoluteFilePath, defineSymbols, commitHash, absoluteFilePath }
            Task.Run(async () => {
                try {
                    if (!ServerHealthCheck.I.IsServerHealthy) {
                        //We poll every 5 seconds so should be fine to return here
                        return;
                    }

                    await ThreadUtility.SwitchToMainThread();
                    // When in play mode in editor, it's highly unlikely the user wants to connect a player build.
                    // So we skip the request and avoid chance of user seeing curl error in unity console.
                    if (EditorApplication.isPlaying) {
                        return;
                    }
                    var buildInfo = BuildInfoHelper.GenerateBuildInfoMainThread();
                    await RequestHelper.PostFilesState(buildInfo);
                } catch (Exception ex) {
                    ThreadUtility.LogException(ex);
                }
            });
        }

        // CheckEditorStart distinguishes between domain reload and first editor open
        // We have some separate logic on editor start (InstallUtility.HandleEditorStart)
        private static void DetectEditorStart() {
            var editorId = EditorAnalyticsSessionInfo.id;
            var currVersion = PackageConst.Version;
            Task.Run(() => {
                try {
                    var lines = File.Exists(sessionFilePath) ? File.ReadAllLines(sessionFilePath) : Array.Empty<string>();

                    long prevSessionId = -1;
                    string prevVersion = null;
                    if (lines.Length >= 2) {
                        long.TryParse(lines[1], out prevSessionId);
                    }
                    if (lines.Length >= 3) {
                        prevVersion = lines[2].Trim();
                    }
                    var updatedFromVersion = (prevSessionId != -1 && currVersion != prevVersion) ? prevVersion : null;

                    if (prevSessionId != editorId && prevSessionId != 0) {
                        // back to mainthread
                        ThreadUtility.RunOnMainThread(() => {
                            InstallUtility.HandleEditorStart(updatedFromVersion);

                            var newEditorId = EditorAnalyticsSessionInfo.id;
                            if (newEditorId != 0) {
                                Task.Run(() => {
                                    try {
                                        // editorId isn't available on first domain reload, must do it here
                                        File.WriteAllLines(sessionFilePath, new[] {
                                            "1", // serialization version
                                            newEditorId.ToString(),
                                            currVersion,
                                        });

                                    } catch (IOException) {
                                        // ignore
                                    }
                                });
                            }
                        });
                    }

                } catch (IOException) {
                    // ignore
                } catch (Exception e) {
                    ThreadUtility.LogException(e);
                }
            });
        }

        public static void UpdateHost() {
            string host;
            if (HotReloadPrefs.RemoteServer) {
                host = HotReloadPrefs.RemoteServerHost;
                RequestHelper.ChangeAssemblySearchPaths(Array.Empty<string>());
            } else {
                host = "127.0.0.1";
            }
            var rootPath = Path.GetFullPath(".");
            RequestHelper.SetServerInfo(new PatchServerInfo(host, null, rootPath, HotReloadPrefs.RemoteServer));
        }

        static void OnIntervalThreaded(object o) {
            ServerHealthCheck.I.CheckHealth();
            TryPostFilesState();
            ThreadUtility.RunOnMainThread((Action)o);
            if(serverDownloader.Progress >= 1f) {
                //In case files get deleted while editor is open
                serverDownloader.EnsureDownloaded(HotReloadCli.controller, CancellationToken.None).Forget();
            }
        }
        
        static void OnIntervalMainThread() {
            if(ServerHealthCheck.I.IsServerHealthy) {
                RequestHelper.PollMethodPatches(resp => HandleResponseReceived(resp));
            }
        }
        
        static void HandleResponseReceived(MethodPatchResponse response) {
            if(response.patches.Length > 0) {
                CodePatcher.I.RegisterPatches(response);
                HotReloadWindow.RegisterPatch(response);
                var window = HotReloadWindow.Current;
                if(window) {
                    window.Repaint();
                }
            }
            if(response.failures.Length > 0) {
                HotReloadWindow.RegisterWarnings(response.failures);
            }
        }

        static void OnCompilationFinished() {
            ServerHealthCheck.I.CheckHealth();
            if(ServerHealthCheck.I.IsServerHealthy) {
                RequestCompile().Forget();
            }
            Task.Run(() => File.Delete(PatchFilePath));
        }
        
        static async Task RequestCompile() {
            await ProjectGeneration.ProjectGeneration.GenerateSlnAndCsprojFiles(Application.dataPath);
            await RequestHelper.RequestCompile();
        }
    }
}