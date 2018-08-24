using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using pyRevitLabs.Common;

using MadMilkman.Ini;

namespace pyRevitLabs.TargetApps.Revit {
    public enum PyRevitLogLevels {
        None,
        Verbose,
        Debug
    }

    public static class pyRevit {
        // consts for the official pyRevit repo
        public const string pyRevitOriginalRepoPath = "https://github.com/eirannejad/pyRevit.git";
        public const string pyRevitOriginalRepoMainBranch = "master";

        // consts for creating pyRevit addon manifest file
        public const string pyRevitAddinName = "PyRevitLoader";
        public const string pyRevitAddinId = "B39107C3-A1D7-47F4-A5A1-532DDF6EDB5D";
        public const string pyRevitAddinClassName = "PyRevitLoader.PyRevitLoaderApplication";
        public const string pyRevitVendorId = "eirannejad";

        // consts for recording pyrevit.exe config in the pyRevit configuration file
        public const string pyRevitAppdataDirName = "pyRevit";
        public const string pyRevitConfigFileName = "pyRevit_config.ini";

        public const string pyRevitCoreConfigSection = "core";
        public const string pyRevitCheckUpdatesKey = "checkupdates";
        public const string pyRevitAutoUpdateKey = "autoupdate";
        public const string pyRevitVerboseKey = "verbose";
        public const string pyRevitDebugKey = "debug";
        public const string pyRevitFileLoggingKey = "filelogging";
        public const string pyRevitStartupLogTimeoutKey = "startuplogtimeout";
        public const string pyRevitUserExtensionsKey = "userextensions";
        public const string pyRevitCompileCSharpKey = "compilecsharp";
        public const string pyRevitCompileVBKey = "compilevb";
        public const string pyRevitLoadBetaKey = "loadbeta";
        public const string pyRevitRocketModeKey = "rocketmode";
        public const string pyRevitBinaryCacheKey = "bincache";
        public const string pyRevitMinDriveSpaceKey = "minhostdrivefreespace";
        public const string pyRevitRequiredHostBuildKey = "requiredhostbuild";

        public const string pyRevitUsageLoggingSection = "usagelogging";
        public const string pyRevitUsageLoggingStatusKey = "active";
        public const string pyRevitUsageLogFilePathKey = "logfilepath";
        public const string pyRevitUsageLogServerUrlKey = "logserverurl";

        public const string pyRevitManagerConfigSectionName = "Manager";
        public const string pyRevitManagerInstalledClonesKey = "Clones";

        // pyRevit config file path
        public static string pyRevitConfigFilePath {
            get {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), pyRevitAppdataDirName, pyRevitConfigFileName);
            }
        }

        // install pyRevit by cloning from git repo
        public static void Install(
            string destPath,
            string repoPath,
            string branchName,
            bool coreOnly = false,
            bool purge = false,
            bool allUsers = false) {

            string repoSourcePath = repoPath ?? pyRevitOriginalRepoPath;
            string branchToCheckout = branchName ?? pyRevitOriginalRepoMainBranch;

            try {
                // start the clone process
                var repo = GitInstaller.Clone(repoSourcePath, branchToCheckout, destPath);

                // record the installation path in config file
                UpdateClonesList(repo.Info.WorkingDirectory, allUsers: allUsers);

                if (coreOnly) {
                    // TODO: Add core checkout option. Figure out how to checkout certain folders in libgit2sharp
                }

                if (purge) {

                }

                // TODO: implement addon manifest file creation? or use pyrevit attach?
            }
            catch (Exception ex) {
                Console.WriteLine(String.Format("Error Installing pyRevit. | {0}", ex.ToString()));
            }
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

        public static bool GetUsageReporting(bool allUsers = false) {
            return Boolean.Parse(GetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLoggingStatusKey, allUsers));
        }

        public static string GetUsageLogFilePath(bool allUsers = false) {
            return GetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLogFilePathKey, allUsers);
        }

        public static string GetUsageLogServerUrl(bool allUsers = false) {
            return GetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLogServerUrlKey, allUsers);
        }

        public static void EnableUsageReporting(string logFilePath = null, string logServerUrl = null, bool allUsers = false) {
            SetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLoggingStatusKey, true, allUsers);

            if (logFilePath != null)
                SetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLogFilePathKey, logFilePath, allUsers);

            if (logServerUrl != null)
                SetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLogServerUrlKey, logServerUrl, allUsers);
        }

        public static void DisableUsageReporting(bool allUsers = false) {
            SetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLoggingStatusKey, false, allUsers);
        }

        public static bool GetCheckUpdates(bool allUsers = false) {
            return Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitCheckUpdatesKey, allUsers));
        }

        public static void SetCheckUpdates(bool state, bool allUsers = false) {
            SetKeyValue(pyRevitCoreConfigSection, pyRevitCheckUpdatesKey, state, allUsers);
        }

        public static bool GetAutoUpdate(bool allUsers = false) {
            return Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitAutoUpdateKey, allUsers));
        }

        public static void SetAutoUpdate(bool state, bool allUsers = false) {
            SetKeyValue(pyRevitCoreConfigSection, pyRevitAutoUpdateKey, state, allUsers);
        }

        public static bool GetRocketMode(bool allUsers = false) {
            return Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitRocketModeKey, allUsers));
        }

        public static void SetRocketMode(bool state, bool allUsers = false) {
            SetKeyValue(pyRevitCoreConfigSection, pyRevitRocketModeKey, state, allUsers);
        }

        public static PyRevitLogLevels GetLoggingLevel(bool allUsers = false) {
            bool verbose = Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitVerboseKey, allUsers));
            bool debug = Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitDebugKey, allUsers));

            if (verbose && !debug)
                return PyRevitLogLevels.Verbose;
            else if (debug)
                return PyRevitLogLevels.Debug;

            return PyRevitLogLevels.None;
        }

        public static void SetLoggingLevel(PyRevitLogLevels level, bool allUsers = false) {
            if (level == PyRevitLogLevels.None) {
                SetKeyValue(pyRevitCoreConfigSection, pyRevitVerboseKey, false, allUsers);
                SetKeyValue(pyRevitCoreConfigSection, pyRevitDebugKey, false, allUsers);
            }

            if (level == PyRevitLogLevels.Verbose) {
                SetKeyValue(pyRevitCoreConfigSection, pyRevitVerboseKey, true, allUsers);
                SetKeyValue(pyRevitCoreConfigSection, pyRevitDebugKey, false, allUsers);
            }

            if (level == PyRevitLogLevels.Debug) {
                SetKeyValue(pyRevitCoreConfigSection, pyRevitVerboseKey, true, allUsers);
                SetKeyValue(pyRevitCoreConfigSection, pyRevitDebugKey, true, allUsers);
            }
        }

        public static bool GetFileLogging(bool allUsers = false) {
            return Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitFileLoggingKey, allUsers));
        }

        public static void SetFileLogging(bool state, bool allUsers = false) {
            SetKeyValue(pyRevitCoreConfigSection, pyRevitFileLoggingKey, state, allUsers);
        }

        public static bool GetLoadBetaTools(bool allUsers = false) {
            return Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitLoadBetaKey, allUsers));
        }

        public static void SetLoadBetaTools(bool state, bool allUsers = false) {
            SetKeyValue(pyRevitCoreConfigSection, pyRevitLoadBetaKey, state, allUsers);
        }


        // generic configuration public access
        public static void GetConfig(string paramName, bool allUsers = false) {

        }

        public static void SetConfig(string paramName, string paramValue, bool allUsers = false) {

        }


        // configurations private access methods
        private static IniFile GetConfigFile(bool allUsers) {
            // TODO: implement allusers
            IniFile cfgFile = new IniFile();
            cfgFile.Load(pyRevitConfigFilePath);
            return cfgFile;
        }

        private static void SaveConfigFile(IniFile cfgFile, bool allUsers) {
            // TODO: implement allusers
            cfgFile.Save(pyRevitConfigFilePath);
        }

        private static void UpdateKeyValue(string section, string key, string value, bool allUsers) {
            var c = GetConfigFile(allUsers);

            if (!c.Sections.Contains(section))
                c.Sections.Add(section);

            if (!c.Sections[section].Keys.Contains(key))
                c.Sections[section].Keys.Add(key);

            c.Sections[section].Keys[key].Value = value;

            SaveConfigFile(c, allUsers);
        }

        private static string GetKeyValue(string section, string key, bool allUsers) {
            var c = GetConfigFile(allUsers);
            return c.Sections[section].Keys[key].Value;
        }

        private static List<string> GetKeyValueAsList(string section, string key, bool allUsers) {
            var c = GetConfigFile(allUsers);
            return new List<string>(c.Sections[section].Keys[key].Value.Split(','));
        }

        private static void SetKeyValue(string section, string key, bool value, bool allUsers) {
            UpdateKeyValue(section, key, value.ToString(), allUsers);
        }

        private static void SetKeyValue(string section, string key, string value, bool allUsers) {
            UpdateKeyValue(section, key, value, allUsers);
        }

        private static void SetKeyValue(string section, string key, List<string> valueList, bool allUsers) {
            UpdateKeyValue(section, key, String.Join(",", valueList), allUsers);
        }

        private static void UpdateClonesList(string newClonePath, bool allUsers = false) {
            var validClones = new HashSet<string>();

            // get existing values
            List<string> recordedClones;
            try {
                recordedClones  = GetKeyValueAsList(pyRevitManagerConfigSectionName, pyRevitManagerInstalledClonesKey, allUsers: allUsers);
            }
            catch {
                recordedClones = new List<string>();
            }

            foreach (var clonePath in recordedClones)
                if (Directory.Exists(clonePath))
                    validClones.Add(clonePath);

            if (Directory.Exists(newClonePath))
                validClones.Add(newClonePath);

            SetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerInstalledClonesKey, new List<string>(validClones), allUsers: allUsers);
        }
    }
}
