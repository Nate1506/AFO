using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SayItAgain.EditorTools
{
    /// <summary>Windows 64-bit build with only the Say It Again scenes (lobby first). Team scenes stay out of it.</summary>
    public static class SayItAgainBuild
    {
        public const string OutputDir = "../Builds/SayItAgain_Win64";
        static readonly string[] Scenes =
        {
            "Assets/SayItAgain/Scenes/Lobby.unity",
            "Assets/SayItAgain/Scenes/Gameplay_Test.unity",
        };

        [MenuItem("Tools/Say It Again/Build Windows")]
        public static void BuildWindows()
        {
            PlayerSettings.productName = "Say It Again";
            // Networking and voice must keep running when the window loses focus.
            PlayerSettings.runInBackground = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;

            Directory.CreateDirectory(OutputDir);
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = Path.Combine(OutputDir, "SayItAgain.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[SayItAgain] Build {s.result}: {s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errors, {s.totalTime.TotalSeconds:F0}s -> {Path.GetFullPath(options.locationPathName)}");
        }

        /// <summary>Starts the build on the next editor tick so a remote caller is not blocked.</summary>
        public static void BuildWindowsDeferred() => EditorApplication.delayCall += BuildWindows;
    }
}
