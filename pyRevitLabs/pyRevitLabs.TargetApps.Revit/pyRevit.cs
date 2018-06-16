using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pyRevitLabs.Common;

using MadMilkman.Ini;

namespace pyRevitLabs.TargetApps.Revit {
    public enum PyRevitLogLevels {
        None,
        Verbose,
        Debug
    }

    public static class pyRevit {
        public const string pyRevitOriginalRepoPath = "https://github.com/eirannejad/pyRevit.git";
        public const string pyRevitOriginalRepoMainBranch = "master";
        public const string pyRevitAddinName = "PyRevitLoader";
        public const string pyRevitAddinId = "B39107C3-A1D7-47F4-A5A1-532DDF6EDB5D";
        public const string pyRevitAddinClassName = "PyRevitLoader.PyRevitLoaderApplication";
        public const string pyRevitVendorId = "eirannejad";

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

        public static List<string> DetectInstalls() {
            var installPaths = new List<string>();
            return installPaths;
        }

        public static void Uninstall() {

        }

        public static void UninstallExtension() {

        }

        public static void ClearCache() {

        }

        public static void ClearAllCaches() {

        }

        public static bool Attach(int revitVersion, bool allVersions = true, bool allUsers = false) {
            return true;
        }

        public static bool Detach(int revitVersion, bool allVersions = true) {
            return true;
        }

        public static void GetExtentions() {

        }

        public static void GetThirdPartyExtentions() {

        }

        public static void EnableExtension() {

        }

        public static void DisableExtension() {

        }

        // configurations

        public static void GetConfig(string paramName) {

        }

        public static void SetConfig(string paramName, string paramValue) {

        }

        public static bool AutoUpdate {
            get {
                return true;
            }
            set {

            }
        }

        public static PyRevitLogLevels LogLevel {
            get {
                return PyRevitLogLevels.None;
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
