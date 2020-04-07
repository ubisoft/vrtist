using System;
using System.Text.RegularExpressions;


namespace VRtist
{
    public class Version
    {
        public static string version = "0.1.0";      // our version
        public static string syncVersion = "0.1.0";  // supported sync version

        private static Regex versionRegex = new Regex(@"(?<major>\d+)\.(?<minor>\d+)\.(?<debug>\d+)(\.(?<other>.+))?", RegexOptions.Compiled);

        public static bool UnpackVersionNumber(string v, out int major, out int minor, out int debug, out string other)
        {
            major = minor = debug = -1;
            other = "";

            MatchCollection matches = versionRegex.Matches(v);
            if(matches.Count != 1) { return false; }

            GroupCollection groups = matches[0].Groups;
            major = Int32.Parse(groups["major"].Value);
            minor = Int32.Parse(groups["minor"].Value);
            debug = Int32.Parse(groups["debug"].Value);
            other = groups["other"].Value;
            return true;
        }

        public static bool CheckSyncCompatibility(string number)
        {
            int major, minor, debug;
            string other;
            UnpackVersionNumber(number, out major, out minor, out debug, out other);

            int syncMajor, syncMinor, syncDebug;
            string syncOther;
            UnpackVersionNumber(syncVersion, out syncMajor, out syncMinor, out syncDebug, out syncOther);

            return major == syncMajor;
        }
    }
}
