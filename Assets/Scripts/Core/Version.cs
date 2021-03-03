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
using System.IO;
using System.Text.RegularExpressions;


namespace VRtist
{
    public class Version
    {
        private static readonly string VERSION_PATH = "version.txt";

        // Our version
        private static string _version = "";
        public static string VersionString
        {
            get
            {
                if (_version.Length == 0)
                {
                    if (File.Exists(VERSION_PATH))
                        _version = File.ReadAllText(VERSION_PATH);
                    else
                        _version = "dev-build";
                }
                return _version;
            }
        }

        // Supported sync version (Mixer)
        public static string syncVersion = "v0.1.0";

        private static readonly Regex versionRegex = new Regex(@"v?(?<major>\d+)\.(?<minor>\d+)\.(?<debug>\d+)(\.(?<other>.+))?", RegexOptions.Compiled);

        public static bool UnpackVersionNumber(string v, out int major, out int minor, out int debug, out string other)
        {
            major = minor = debug = -1;
            other = "";

            MatchCollection matches = versionRegex.Matches(v);
            if (matches.Count != 1) { return false; }

            GroupCollection groups = matches[0].Groups;
            major = Int32.Parse(groups["major"].Value);
            minor = Int32.Parse(groups["minor"].Value);
            debug = Int32.Parse(groups["debug"].Value);
            other = groups["other"].Value;
            return true;
        }

        public static bool CheckSyncCompatibility(string number)
        {
            UnpackVersionNumber(number, out int major, out _, out _, out _);
            UnpackVersionNumber(syncVersion, out int syncMajor, out _, out _, out _);
            return major == syncMajor;
        }
    }
}
