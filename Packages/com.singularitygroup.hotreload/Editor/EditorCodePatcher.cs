using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SingularityGroup.HotReload.DTO;
using SingularityGroup.HotReload.Editor.Cli;
using SingularityGroup.HotReload.Editor.Demo;
using SingularityGroup.HotReload.RuntimeDependencies;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SingularityGroup.HotReload.Editor {
    [InitializeOnLoad]
    static class EditorCodePatcher {
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
            EditorApplication.delayCall += TryPrepareBuildInfo;
            DetectEditorStart();
            serverDownloader.CheckIfDownloaded(HotReloadCli.controller);
            SingularityGroup.HotReload.Demo.Demo.I = new EditorDemo();
            RecordActiveDaysForRateApp();
        }

        private static DateTime lastPrepareBuildInfo = DateTime.UtcNow;

        /// Post state for player builds.
        /// Only check build target because user can change build settings whenever.
        internal static void TryPrepareBuildInfo() {
            // Note: we post files state even when build target is wrong
            // because you might connect with a build downloaded onto the device. 
            if ((DateTime.UtcNow - lastPrepareBuildInfo).TotalSeconds > 5) {
                lastPrepareBuildInfo = DateTime.UtcNow;
                HotReloadCli.PrepareBuildInfoAsync().Forget();
            }
        }

        internal static void RecordActiveDaysForRateApp() {
            var unixDay = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 86400);
            var activeDays = GetActiveDaysForRateApp();
            if (activeDays.Count < Constants.DaysToRateApp && activeDays.Add(unixDay.ToString())) {
                HotReloadPrefs.ActiveDays = string.Join(",", activeDays);
            }
        }
        
        internal static HashSet<string> GetActiveDaysForRateApp() {
            if (string.IsNullOrEmpty(HotReloadPrefs.ActiveDays)) {
                return new HashSet<string>();
            }
            return new HashSet<string>(HotReloadPrefs.ActiveDays.Split(','));
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
            ServerHealthCheck.instance.CheckHealth();
            ThreadUtility.RunOnMainThread((Action)o);
            if(serverDownloader.Progress >= 1f && !serverDownloader.CheckIfDownloaded(HotReloadCli.controller)) {
                //In case files get deleted while editor is open
                ThreadUtility.RunOnMainThread(() => {
                    if(HotReloadPrefs.DontShowPromptForDownload) {
                        serverDownloader.EnsureDownloaded(HotReloadCli.controller, CancellationToken.None).Forget();
                    }
                });
            }
        }
        
        static void OnIntervalMainThread() {
            TryPrepareBuildInfo();
            if(ServerHealthCheck.I.IsServerHealthy) {
                RequestHelper.PollMethodPatches(resp => HandleResponseReceived(resp));
            }
        }
        
        static void HandleResponseReceived(MethodPatchResponse response) {
            if(response.patches.Length > 0) {
                LogBurstHint(response);
                CodePatcher.I.RegisterPatches(response, persist: true);
                var window = HotReloadWindow.Current;
                if(window) {
                    window.Repaint();
                }
            }
            if(response.failures.Length > 0) {
                HotReloadWindow.RegisterWarnings(response.failures);
            }
            HandleRemovedUnityMehtods(response.removedMethod);
        }

        static void HandleRemovedUnityMehtods(SMethod[] removedMethod) {
            foreach(var sMethod in removedMethod) {
                try {
                    var candidates = CodePatcher.I.SymbolResolver.Resolve(sMethod.assemblyName.Replace(".dll", ""));
                    var asm = candidates[0];
                    var module = asm.GetLoadedModules()[0];
                    var oldMethod = module.ResolveMethod(sMethod.metadataToken);
                    UnityEventHelper.RemoveUnityEventMethod(oldMethod);
                } catch(Exception ex) {
                    Log.Warning("Encountered exception in RemoveUnityEventMethod: {0} {1}", ex.GetType().Name, ex.Message);
                }
            }
        }
        
        [Conditional("UNITY_2022_2_OR_NEWER")]
        static void LogBurstHint(MethodPatchResponse response) {
            if(HotReloadPrefs.LoggedBurstHint) {
                return;
            }
            foreach (var patch in response.patches) {
                if(patch.unityJobs.Length > 0) {
                    Debug.LogWarning("A unity job was hot reloaded. " +
                                     "This will cause a harmless warning that can be ignored. " +
                                     $"More info about this can be found here: {Constants.TroubleshootingURL}");
                    HotReloadPrefs.LoggedBurstHint = true;
                    break;
                }
            }
        }

        static void OnCompilationFinished() {
            ServerHealthCheck.instance.CheckHealth();
            if(ServerHealthCheck.I.IsServerHealthy) {
                RequestCompile().Forget();
            }
            Task.Run(() => File.Delete(PatchFilePath));
        }
        
        static async Task RequestCompile() {
            await RequestHelper.RequestClearPatches();
            await ProjectGeneration.ProjectGeneration.GenerateSlnAndCsprojFiles(Application.dataPath);
            await RequestHelper.RequestCompile();
        }
    }
}