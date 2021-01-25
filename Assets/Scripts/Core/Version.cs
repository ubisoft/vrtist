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
