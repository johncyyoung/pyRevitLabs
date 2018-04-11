using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pyRevitLabs.Common;

namespace pyRevitLabs.TargetApps.Revit {
    public enum pyRevitLogLevels {
        None,
        Verbose,
        Debug
    }

    public static class pyRevit {
        public static string pyRevitOriginalRepoPath = "https://github.com/eirannejad/pyRevit.git";
        public static string pyRevitOriginalRepoMainBranch = "master";

        public static void Install(
            string destPath,
            string repoPath,
            string branchName,
            bool coreOnly = false,
            bool purge = false ) {

            var repo = GitInstaller.Clone(
                repoPath ?? pyRevitOriginalRepoPath,
                branchName ?? pyRevitOriginalRepoMainBranch,
                destPath
                );
        }

        public static bool InstallExtension() {
            return true;
        }

        public static bool Attach(int revitVersion, bool allVersions = true, bool allUsers = false) {
            return true;
        }

        public static bool Detach(int revitVersion, bool allVersions = true) {
            return true;
        }

        public static bool AutoUpdate {
            get {
                return true;
            }
            set {

            }
        }

        public static pyRevitLogLevels LogLevel {
            get {
                return pyRevitLogLevels.None;
            }
            set {

            }
        }

        public static bool FileLogging {
            get {
                return true;
            }
            set {

            }
        }

        public static bool LoadBetaTools {
            get {
                return true;
            }
            set {

            }
        }

        public static bool UsageReporting {
            get {
                return true;
            }
            set {

            }
        }

        public static string UsageReportFile {
            get {
                return "";
            }
            set {

            }
        }

        public static string UsageReportServer {
            get {
                return "";
            }
            set {

            }
        }
    }
}
