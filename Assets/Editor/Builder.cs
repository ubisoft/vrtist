using System;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;

namespace VRtist
{
    // Perform a build
    // Typical command line:
    // "C:\Program Files\Unity\Editor\Unity.exe" -quit -batchmode -projectPath "C:\Users\UserName\Documents\MyProject" -executeMethod VRtist.Builder.PerformBuild
    //
    // Some options can be added for the build:
    //  --buildDir: to specify the build directory.
    public class Builder
    {
        private const string BUILD_DIR_OPTION = "--buildDir";
        private const string DEFAULT_BUILD_DIR = "Build";
        private const string EXE_NAME = "VRtist.exe";

        public static void PerformBuild()
        {
            // Get command line arguments
            string[] args = System.Environment.GetCommandLineArgs();

            BuildPlayerOptions buildOptions = new BuildPlayerOptions();
            buildOptions.scenes = new[] { "Assets/Scenes/Main.unity" };
            buildOptions.target = BuildTarget.StandaloneWindows64;

            int index = Array.IndexOf(args, BUILD_DIR_OPTION);
            if(-1 != index)
            {
                try
                {
                    buildOptions.locationPathName = args[index + 1];
                }
                catch(IndexOutOfRangeException)
                {
                    buildOptions.locationPathName = DEFAULT_BUILD_DIR;
                }
            }
            else
            {
                buildOptions.locationPathName = DEFAULT_BUILD_DIR;
            }
            DateTime now = DateTime.Now;
            buildOptions.locationPathName += $"/{now:yyyy_MM_dd-HH_mm_ss}/{EXE_NAME}";
            buildOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if(summary.result == BuildResult.Succeeded)
            {
                //Debug.Log("Build succeeded");
                //Debug.Log($"Total time: {summary.totalTime.TotalSeconds} seconds");
                //Debug.Log($"Total size: {summary.totalSize} bytes");
                //Debug.Log($"Path: {summary.outputPath}");
            }
            else
            {
                //Debug.LogError("Build failed");
                //Debug.LogError($"Warnings: {summary.totalWarnings}");
                //Debug.LogError($"Errors: {summary.totalErrors}");
                //Debug.LogError(summary.ToString());

                // Will force an exit code != 0
                throw new Exception("Build Failed");
            }
        }
    }
}
