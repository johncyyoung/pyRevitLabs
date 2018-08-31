using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

using pyRevitLabs.Common;
using pyRevitLabs.Common.Extensions;

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
        // core configs
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
        public const string pyRevitOutputStyleSheet = "outputstylesheet";
        // usage logging configs
        public const string pyRevitUsageLoggingSection = "usagelogging";
        public const string pyRevitUsageLoggingStatusKey = "active";
        public const string pyRevitUsageLogFilePathKey = "logfilepath";
        public const string pyRevitUsageLogServerUrlKey = "logserverurl";
        // pyrevit.exe specific configs
        public const string pyRevitManagerConfigSectionName = "manager";
        public const string pyRevitManagerInstalledClonesKey = "clones";
        public const string pyRevitManagerPrimaryCloneKey = "primaryclone";
        // extensions
        public const string pyRevitExtensionDisabledKey = "disabled";

        // pyRevit %appdata% path
        public static string pyRevitAppDataPath {
            get {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), pyRevitAppdataDirName);
            }
        }

        // pyRevit %programdata% path
        public static string pyRevitProgramDataPath {
            get {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), pyRevitAppdataDirName);
            }
        }

        // pyRevit config file path
        public static string pyRevitConfigFilePath {
            get {
                return Path.Combine(pyRevitAppDataPath, pyRevitConfigFileName);
            }
        }

        // pyRevit config file path
        public static string pyRevitConfigFilePathAllUsers {
            get {
                return Path.Combine(pyRevitProgramDataPath, pyRevitConfigFileName);
            }
        }

        // install pyRevit by cloning from git repo
        public static void Install(
            string destPath,
            string repoPath,
            string branchName,
            bool coreOnly = false,
            bool allUsers = false) {

            string repoSourcePath = repoPath ?? pyRevitOriginalRepoPath;
            string branchToCheckout = branchName ?? pyRevitOriginalRepoMainBranch;

            try {
                // start the clone process
                var repo = GitInstaller.Clone(repoSourcePath, branchToCheckout, destPath);

                // record the installation path in config file
                SetPrimaryClone(repo.Info.WorkingDirectory, allUsers: allUsers);
                RegisterClone(repo.Info.WorkingDirectory, allUsers: allUsers);


                if (coreOnly) {
                    // TODO: Add core checkout option. Figure out how to checkout certain folders in libgit2sharp
                }

                // TODO: implement addon manifest file creation? or use pyrevit attach?
            }
            catch (Exception ex) {
                Console.WriteLine(String.Format("Error Installing pyRevit. | {0}", ex.ToString()));
            }
        }

        public static void Uninstall(string repoPath = null, bool clearConfigs = false, bool allUsers = true) {
            if (repoPath == null)
                repoPath = GetPrimaryClone(allUsers: allUsers);

            if (repoPath != null) {
                UnregisterClone(repoPath,allUsers: allUsers);
                pyRevitUtils.DeleteDirectory(repoPath);
            }

            if (clearConfigs)
                DeleteConfigs(allUsers: allUsers);
        }

        public static void UninstallAllClones(bool clearConfigs = false, bool allUsers = true) {
            foreach (string clonePath in GetClones())
                Uninstall(clonePath, clearConfigs: false, allUsers: allUsers);

            if (clearConfigs)
                DeleteConfigs(allUsers: allUsers);
        }

        public static void DeleteConfigs(bool allUsers = true) {
            if (File.Exists(pyRevitConfigFilePath))
                File.Delete(pyRevitConfigFilePath);

            if (allUsers && File.Exists(pyRevitConfigFilePathAllUsers))
                File.Delete(pyRevitConfigFilePathAllUsers);
        }

        public static HashSet<string> RecordedInstalls() { return GetClones(); }

        public static void InstallExtension() {
        }

        public static void UninstallExtension() {

        }

        public static void Checkout(string branchName, string repoPath = null) {
            if (repoPath == null)
                repoPath = GetPrimaryClone();

            GitInstaller.CheckoutBranch(repoPath, branchName);
        }

        public static void SetCommit(string commitHash, string repoPath = null) {
            if (repoPath == null)
                repoPath = GetPrimaryClone();

            GitInstaller.RebaseToCommit(repoPath, commitHash);
        }

        public static void SetVersion(string versionTagName, string repoPath = null) {
            if (repoPath == null)
                repoPath = GetPrimaryClone();

            GitInstaller.RebaseToTag(repoPath, versionTagName);
        }

        public static void Update(string repoPath = null, bool allClones = false) {
            // TODO: implement allClones
            if (repoPath != null) {
                GitInstaller.ForcedUpdate(GetPrimaryClone());
                return;
            }

            // if repo path is not provided, check configs for primary repo to update
            if (IsPrimaryCloneConfigured()) {
                // current user config
                GitInstaller.ForcedUpdate(GetPrimaryClone());
                return;
            }
            else if (IsPrimaryCloneConfigured(allUsers: true)) {
                // all users config
                GitInstaller.ForcedUpdate(GetPrimaryClone(allUsers: true));
                return;
            }
        }

        public static void ClearCache(string revitVersion) {
            pyRevitUtils.DeleteDirectory(Path.Combine(pyRevitAppDataPath, revitVersion));
        }

        public static void ClearAllCaches() {
            var cacheDirFinder = new Regex(@"\d\d\d\d");
            foreach (string subDir in Directory.GetDirectories(pyRevitAppDataPath)) {
                var dirName = Path.GetFileName(subDir);
                if (cacheDirFinder.IsMatch(dirName))
                    ClearCache(dirName);
            }
        }

        public static void Attach(int revitVersion, bool allVersions = true, bool allUsers = false) {
        }

        public static void Detach(int revitVersion, bool allVersions = true) {
        }

        public static void GetExtentions() {

        }

        public static void GetThirdPartyExtentions() {

        }

        public static void EnableExtension(string extName, bool allUsers = true) {
            SetKeyValue(extName, pyRevitExtensionDisabledKey, false, allUsers);
        }

        public static void DisableExtension(string extName, bool allUsers = true) {
            SetKeyValue(extName, pyRevitExtensionDisabledKey, true, allUsers);
        }

        public static bool IsPrimaryCloneConfigured(bool allUsers = false) {
            try {
                GetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerPrimaryCloneKey, allUsers: allUsers);
                return true;
            }
            catch {
                return false;
            }
        }

        public static string GetPrimaryClone(bool allUsers = false) {
            var primaryClone = GetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerPrimaryCloneKey, allUsers: allUsers);
            if (primaryClone != null && Directory.Exists(primaryClone)) {
                primaryClone = Path.GetFullPath(primaryClone);
                SetPrimaryClone(primaryClone);
                return primaryClone;
            }
            else
                return null;
        }

        public static void SetPrimaryClone(string newClonePath, bool allUsers = false) {
            if (Directory.Exists(newClonePath)) {
                newClonePath = Path.GetFullPath(newClonePath);
                SetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerPrimaryCloneKey, newClonePath, allUsers: allUsers);
            }
        }

        public static void ClearPrimaryClone(bool allUsers = false) {
            SetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerPrimaryCloneKey, "", allUsers: allUsers);
        }

        public static HashSet<string> GetClones(bool allUsers = false) {
            var validatedClones = new HashSet<string>();
            try {
                // verify all registered clones, protect against tampering
                foreach (string clone in GetKeyValueAsList(pyRevitManagerConfigSectionName, pyRevitManagerInstalledClonesKey, allUsers: allUsers)) {
                    if (Directory.Exists(clone))
                        validatedClones.Add(Path.GetFullPath(clone));
                }
                // rewrite the verified clones list back to config file
                UpdateRegisteredClonesList(validatedClones.AsEnumerable(), allUsers: allUsers);
            }
            catch {
            }

            return validatedClones;
        }

        // pyrevit config getter/setter
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

        public static string GetOutputStyleSheet(bool allUsers = false) {
            return GetKeyValue(pyRevitCoreConfigSection, pyRevitOutputStyleSheet, allUsers);
        }

        public static void SetOutputStyleSheet(string outputCSSFilePath, bool allUsers = false) {
            if (File.Exists(outputCSSFilePath))
                SetKeyValue(pyRevitCoreConfigSection, pyRevitOutputStyleSheet, outputCSSFilePath, allUsers);
        }

        // generic configuration public access
        public static string GetConfig(string sectionName, string paramName, bool allUsers = false) {
            return GetKeyValue(sectionName, paramName, allUsers);
        }

        public static void SetConfig(string sectionName, string paramName, string paramValue, bool allUsers = false) {
            SetKeyValue(sectionName, paramName, paramValue, allUsers);
        }

        public static void SetConfig(string sectionName, string paramName, bool paramState, bool allUsers = false) {
            SetKeyValue(sectionName, paramName, paramState, allUsers);
        }

        // configurations private access methods
        private static IniFile GetConfigFile(bool allUsers) {
            // INI formatting
            var cfgOps = new IniOptions();
            cfgOps.KeySpaceAroundDelimiter = true;
            IniFile cfgFile = new IniFile(cfgOps);

            // read or make the file
            var configFile = allUsers ? pyRevitConfigFilePathAllUsers : pyRevitConfigFilePath;
            pyRevitUtils.ConfirmFile(configFile);

            cfgFile.Load(configFile);
            return cfgFile;
        }

        private static void SaveConfigFile(IniFile cfgFile, bool allUsers) {
            var configFile = allUsers ? pyRevitConfigFilePathAllUsers : pyRevitConfigFilePath;
            cfgFile.Save(configFile);
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

        private static void SetKeyValue(string section, string key, int value, bool allUsers) {
            UpdateKeyValue(section, key, value.ToString(), allUsers);
        }

        private static void SetKeyValue(string section, string key, string value, bool allUsers) {
            UpdateKeyValue(section, key, value, allUsers);
        }

        private static void SetKeyValue(string section, string key, IEnumerable<string> valueList, bool allUsers) {
            UpdateKeyValue(section, key, String.Join(",", valueList), allUsers);
        }

        private static void RegisterClone(string newClonePath, bool allUsers = false) {
            var validClones = GetClones();

            if (Directory.Exists(newClonePath))
                validClones.Add(Path.GetFullPath(newClonePath));

            SetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerInstalledClonesKey, new List<string>(validClones), allUsers: allUsers);
        }

        private static void UnregisterClone(string existigClonePath, bool allUsers = false) {
            // make sure if this clone is primary, remove it
            var normalizedExClonePath = existigClonePath.NormalizeAsPath();
            var primaryClone = GetPrimaryClone();
            if (primaryClone != null
                && normalizedExClonePath == primaryClone.NormalizeAsPath())
                ClearPrimaryClone(allUsers: allUsers);

            // remove the clone path from list
            var remainingClones = new List<string>();
            foreach(string clonePath in GetClones()) {
                if (normalizedExClonePath != clonePath.NormalizeAsPath())
                    remainingClones.Add(Path.GetFullPath(clonePath));
            }

            SetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerInstalledClonesKey, new List<string>(remainingClones), allUsers: allUsers);
        }

        private static void UpdateRegisteredClonesList(IEnumerable<string> clonePaths, bool allUsers = false) {
            SetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerInstalledClonesKey, clonePaths, allUsers: allUsers);
        }

    }
}
