using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SingularityGroup.HotReload.Newtonsoft.Json;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace SingularityGroup.HotReload.Editor.Cli {
    class StartArgs {
        public string hotreloadTempDir;
        // aka method patch temp dir
        public string cliTempDir;
        public string executableTargetDir;
        public string executableSourceDir;
        public string cliArguments;
        public string unityProjDir;
    }
    
    [InitializeOnLoad]
    public static class HotReloadCli {
        internal static readonly ICliController controller;
        
        //InitializeOnLoad ensures controller gets initialized on unity thread
        static HotReloadCli() {
            controller =
    #if UNITY_EDITOR_OSX
                new OsxCliController();
    #elif UNITY_EDITOR_LINUX
                new LinuxCliController();
    #elif UNITY_EDITOR_WIN
                new WindowsCliController();
    #else
                new FallbackCliController();
    #endif
        }
        
        public static async Task StartAsync(string dataPath, bool exposeServerToNetwork) {
            await Prepare(dataPath).ConfigureAwait(false);
            await ThreadUtility.SwitchToThreadPool();
            StartArgs args;
            if(TryGetStartArgs(dataPath, exposeServerToNetwork, out args)) {
                await controller.Start(args);
            }
        }
        
        public static Task StopAsync() {
            return controller.Stop();
        }
        
        /// <summary>
        /// Stop the server, then start it again.
        /// </summary>
        /// <remarks>
        /// Used when the user changes server options that can only be applied by restarting it.<br/>
        /// Should be called on the thread pool.
        /// </remarks>
        public static async Task RestartAsync(string dataPath, bool exposeServerToNetwork) {
            await Prepare(dataPath).ConfigureAwait(false);
            await ThreadUtility.SwitchToThreadPool();
            
            StartArgs args;
            if(TryGetStartArgs(dataPath, exposeServerToNetwork, out args)) {
                await controller.Stop().ConfigureAwait(false);
                await controller.Start(args).ConfigureAwait(false);
            }
        }
        
        static bool TryGetStartArgs(string dataPath, bool exposeServerToNetwork, out StartArgs args) {
            string serverDir;
            if(!CliUtils.TryFindServerDir(out serverDir)) {
                Debug.LogWarning($"Failed to start the Hot Reload Server. " +
                                 $"Unable to locate the 'Server' directory. " +
                                 $"Make sure the 'Server' directory is " +
                                 $"somewhere in the Assets folder inside a 'HotReload' folder or in the HotReload package");
                args = null;
                return false;
            }
            
            var hotReloadTmpDir = CliUtils.GetHotReloadTempDir();
            var cliTempDir = CliUtils.GetCliTempDir();
            // Versioned path so that we only need to extract the binary once. User can have multiple projects
            //  on their machine using different HotReload versions.
            var executableTargetDir = CliUtils.GetExecutableTargetDir();
            Directory.CreateDirectory(executableTargetDir); // ensure exists
            var executableSourceDir = Path.Combine(serverDir, controller.PlatformName);
            var unityProjDir = Path.GetDirectoryName(dataPath);
            var slnPath = ProjectGeneration.ProjectGeneration.GetSolutionFilePath(dataPath);

            if (!File.Exists(slnPath)) {
                Debug.LogWarning($"No .sln file found. Open any c# file to generate it so Hot Reload can work properly");
            }
            
            var searchAssemblies = string.Join(";", CodePatcher.I.GetAssemblySearchPaths());
            var cliArguments = $@"-u ""{unityProjDir}"" -s ""{slnPath}"" -t ""{cliTempDir}"" -a ""{searchAssemblies}""";
            if (exposeServerToNetwork) {
                // server will listen on local network interface (default is localhost only)
                cliArguments += " -e true";
            }
            args = new StartArgs {
                hotreloadTempDir = hotReloadTmpDir,
                cliTempDir = cliTempDir,
                executableTargetDir = executableTargetDir,
                executableSourceDir = executableSourceDir,
                cliArguments = cliArguments,
                unityProjDir = unityProjDir,
            };
            return true;
        }
        
        static async Task Prepare(string dataPath) {
            await ThreadUtility.SwitchToMainThread();
            
            if(!File.Exists(ProjectGeneration.ProjectGeneration.GetSolutionFilePath(dataPath))) {
                await ProjectGeneration.ProjectGeneration.EnsureSlnAndCsprojFiles(dataPath);
            }
            await PrepareBuildInfo();
            PrepareSystemPathsFile();
        }
        
        static async Task PrepareBuildInfo() {
            // When starting server make sure it starts with correct player data state.
            // (this fixes issue where Unity is in background and not sending files state).
            // Always write player data because you can be on any build target and want to connect with a downloaded android build.
            var buildInfo = await BuildInfoHelper.GenerateBuildInfoAsync();
            var json = buildInfo.ToJson();
            var cliTempDir = CliUtils.GetCliTempDir();
            Directory.CreateDirectory(cliTempDir);
            File.WriteAllText(Path.Combine(cliTempDir, "playerdata.json"), json);
        }
        
        static void PrepareSystemPathsFile() {
            var lvl = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup);
#if UNITY_2020_3_OR_NEWER
            var dirs = UnityEditor.Compilation.CompilationPipeline.GetSystemAssemblyDirectories(lvl);
#else
            var t = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Scripting.ScriptCompilation.MonoLibraryHelpers");
            var m = t.GetMethod("GetSystemReferenceDirectories");
            var dirs = m.Invoke(null, new object[] { lvl });
#endif
            Directory.CreateDirectory(PackageConst.LibraryCachePath);
            File.WriteAllText(PackageConst.LibraryCachePath + "/systemAssemblies.json", JsonConvert.SerializeObject(dirs));
        }
    }
}
