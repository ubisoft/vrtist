/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;

namespace VRtist
{
    // Perform a build
    // Typical command line:
    //   "C:\Program Files\Unity\Editor\Unity.exe" -quit -batchmode -projectPath "C:\Users\UserName\Documents\MyProject" -executeMethod VRtist.Builder.PerformBuild
    // Other options:
    //   --buildDir to override the default build directory name (which is made from the current date).
    public class Builder
    {
        private const string BUILD_DIR_OPTION = "--buildDir";
        private const string ROOT_BUILD_DIR = "Build";
        private const string EXE_NAME = "VRtist.exe";

        public static void PerformBuild()
        {
            string[] args = System.Environment.GetCommandLineArgs();

            BuildPlayerOptions buildOptions = new BuildPlayerOptions();
            buildOptions.scenes = new[] { "Assets/Scenes/Main.unity" };
            buildOptions.target = BuildTarget.StandaloneWindows64;
            buildOptions.options = BuildOptions.None;
            int index = Array.IndexOf(args, BUILD_DIR_OPTION);
            if(-1 != index)
            {
                try
                {
                    buildOptions.locationPathName = $"{ROOT_BUILD_DIR}/{args[index + 1]}/{EXE_NAME}";
                }
                catch(IndexOutOfRangeException)
                {
                    buildOptions.locationPathName = GetDefaultBuildDir();
                }
            }
            else
            {
                buildOptions.locationPathName = GetDefaultBuildDir();
            }

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

        private static string GetDefaultBuildDir()
        {
            DateTime now = DateTime.Now;
            return $"{ROOT_BUILD_DIR}/{now:yyyy_MM_dd-HH_mm_ss}/{EXE_NAME}";
        }
    }
}
