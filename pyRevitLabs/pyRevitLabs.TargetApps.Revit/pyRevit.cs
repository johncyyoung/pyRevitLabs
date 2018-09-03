using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using pyRevitLabs.Common;
using pyRevitLabs.Common.Extensions;

using MadMilkman.Ini;
using Newtonsoft.Json.Linq;
using NLog;

namespace pyRevitLabs.TargetApps.Revit {
    // DATA TYPES ====================================================================================================
    // pyrevit log levels
    public enum PyRevitLogLevels {
        NotSet,
        None,
        Verbose,
        Debug
    }

    // pyrevit extension wrapper
    public class pyRevitExtension {
        private dynamic _jsonObj;

        public pyRevitExtension(JObject jsonObj) {
            _jsonObj = jsonObj;
        }

        public override string ToString() { return _jsonObj.ToString(); }

        public string Name { get { return _jsonObj.name; } }
        public string Url { get { return _jsonObj.url; } }
    }

    // MODEL =========================================================================================================
    // main pyrevit functionality class
    public static class pyRevit {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // consts for the official pyRevit repo
        public const string pyRevitOriginalRepoPath =
            //"https://github.com/eirannejad/pyRevit.git";
            "https://github.com/eirannejad/rsparam.git";

        public const string pyRevitExtensionsDefinitionFileUri =
            "https://github.com/eirannejad/pyRevit/raw/master/extensions/extensions.json";

        public const string pyRevitInstallName = "pyRevit";
        public const string pyRevitOriginalRepoMainBranch = "master";
        public const string pyRevitExtensionRepoMainBranch = "master";

        // consts for creating pyRevit addon manifest file
        public const string pyRevitAddinFileName = "pyRevit";
        public const string pyRevitAddinName = "PyRevitLoader";
        public const string pyRevitAddinId = "B39107C3-A1D7-47F4-A5A1-532DDF6EDB5D";
        public const string pyRevitAddinClassName = "PyRevitLoader.PyRevitLoaderApplication";
        public const string pyRevitVendorId = "eirannejad";
        public const string pyRevitDllName = "pyRevitLoader.dll";

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
        public const int pyRevitDynamoCompatibleEnginerVer = 273;
        // usage logging configs
        public const string pyRevitUsageLoggingSection = "usagelogging";
        public const string pyRevitUsageLoggingStatusKey = "active";
        public const string pyRevitUsageLogFilePathKey = "logfilepath";
        public const string pyRevitUsageLogServerUrlKey = "logserverurl";
        // pyrevit.exe specific configs
        public const string pyRevitManagerConfigSectionName = "environment";
        public const string pyRevitManagerPrimaryCloneKey = "primaryclone";
        public const string pyRevitManagerInstalledClonesKey = "clones";
        // extensions
        public const string pyRevitExtensionDisabledKey = "disabled";

        // STANDARD PATHS ============================================================================================
        // pyRevit %appdata% path
        // @reviewed
        public static string pyRevitAppDataPath {
            get {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), pyRevitAppdataDirName
                    );
            }
        }

        // pyRevit %programdata% path
        // @reviewed
        public static string pyRevitProgramDataPath {
            get {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), pyRevitAppdataDirName
                    );
            }
        }

        // pyRevit config file path
        // @reviewed
        public static string pyRevitConfigFilePath {
            get {
                return Path.Combine(pyRevitAppDataPath, pyRevitConfigFileName);
            }
        }

        // pyRevit config file path
        // @reviewed
        public static string pyRevitSeedConfigFilePath {
            get {
                return Path.Combine(pyRevitProgramDataPath, pyRevitConfigFileName);
            }
        }

        // INSTALL UNINSTALL =========================================================================================
        // install pyRevit by cloning from git repo
        // @handled @logs
        public static void Install(bool coreOnly = false,
                                   string branchName = null,
                                   string repoPath = null,
                                   string destPath = null) {
            string repoSourcePath = repoPath ?? pyRevitOriginalRepoPath;
            string repoBranch = branchName != null ? branchName : pyRevitOriginalRepoMainBranch;
            logger.Debug(string.Format("Repo source determined as {0}:{1}", repoSourcePath, repoBranch));

            // determine destination path if not provided
            if (destPath == null) {
                destPath = Path.Combine(pyRevitAppDataPath, pyRevitInstallName);
            }
            logger.Debug(string.Format("Destination path determined as {0}", destPath));

            // start the clone process
            LibGit2Sharp.Repository repo = null;
            if (coreOnly) {
                // TODO: Add core checkout option. Figure out how to checkout certain folders in libgit2sharp
                throw new NotImplementedException("Core checkout option not implemented yet.");
            }
            else {
                repo = GitInstaller.Clone(repoSourcePath, repoBranch, destPath);
            }

            // record the installation path in config file
            if (repo != null) {
                logger.Debug("Clone successful. Registering repo as primary...");
                SetPrimaryClone(repo.Info.WorkingDirectory);
            }
        }

        // uninstall primary or specified clone, has option for clearing configs
        public static void Uninstall(string repoPath = null, bool clearConfigs = false) {
            if (repoPath == null)
                repoPath = GetPrimaryClone();

            if (repoPath != null) {
                UnregisterClone(repoPath);
                CommonUtils.DeleteDirectory(repoPath);
            }

            if (clearConfigs)
                DeleteConfigs();
        }

        public static void UninstallAllClones(bool clearConfigs = false) {
            foreach (string clonePath in GetClones())
                Uninstall(clonePath, clearConfigs: false);

            if (clearConfigs)
                DeleteConfigs();
        }

        public static void DeleteConfigs() {
            if (File.Exists(pyRevitConfigFilePath))
                File.Delete(pyRevitConfigFilePath);
        }

        public static List<pyRevitExtension> LookupRegisteredExtensions(string searchPattern = null) {
            // download and read file
            string extDefJson = CommonUtils.DownloadFile(
                pyRevitExtensionsDefinitionFileUri,
                Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "pyrevitextensions.json")
                );
            dynamic extensionsObj = JObject.Parse(File.ReadAllText(extDefJson));

            // make extension list
            var pyrevtExts = new List<pyRevitExtension>();
            foreach (JObject extObj in extensionsObj.extensions) {
                var ext = new pyRevitExtension(extObj);
                if (searchPattern != null) {
                    var extMatcher = new Regex(searchPattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                    if (extMatcher.IsMatch(ext.Name))
                        pyrevtExts.Add(ext);
                }
                else
                    pyrevtExts.Add(ext);
            }

            return pyrevtExts;
        }

        public static pyRevitExtension LookupExtension(string extensionName) {
            var matchingExts = LookupRegisteredExtensions(extensionName);
            if (matchingExts.Count == 1)
                return matchingExts[0];

            return null;
        }

        public static void InstallExtension(string extensionName, string destPath, string branchName) {
            if (extensionName != null) {
                pyRevitExtension extension = LookupExtension(extensionName);
                if (extension != null) {
                    InstallExtensionFromRepo(extension.Url, branchName, destPath);
                }
            }
        }

        public static void InstallExtensionFromRepo(string repoUrl, string destPath, string branchName) {
            if (Directory.Exists(destPath)) {
                branchName = branchName ?? pyRevitExtensionRepoMainBranch;

                try {
                    // start the clone process
                    var repo = GitInstaller.Clone(repoUrl, branchName, destPath);
                    // TODO: add extension path to config
                }
                catch (Exception ex) {
                    logger.Error(string.Format("Error Installing pyRevit. | {0}", ex.ToString()));
                }
            }
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
        }

        public static void ClearCache(string revitVersion) {
            CommonUtils.DeleteDirectory(Path.Combine(pyRevitAppDataPath, revitVersion));
        }

        public static void ClearAllCaches() {
            var cacheDirFinder = new Regex(@"\d\d\d\d");
            foreach (string subDir in Directory.GetDirectories(pyRevitAppDataPath)) {
                var dirName = Path.GetFileName(subDir);
                if (cacheDirFinder.IsMatch(dirName))
                    ClearCache(dirName);
            }
        }

        public static void Attach(string revitVersion, string repoPath = null, int engineVer = 000, bool allUsers = false) {
            // use primary repo if none is specified
            if (repoPath == null)
                repoPath = GetPrimaryClone();

            // make the addin manifest file
            Addons.CreateManifestFile(revitVersion.ConvertToVersion(),
                                      pyRevitAddinFileName,
                                      pyRevitAddinName,
                                      GetEnginePath(repoPath, engineVer),
                                      pyRevitAddinId,
                                      pyRevitAddinClassName,
                                      pyRevitVendorId,
                                      allusers: allUsers);
        }

        public static void AttachAll(string repoPath = null, int engineVer = 000, bool allUsers = false) {
            foreach (var revit in RevitConnector.ListInstalledRevits())
                Attach(revit.Version.Major.ToString(), repoPath: repoPath, engineVer: engineVer, allUsers: allUsers);
        }

        public static void Detach(string revitVersion) {
            Addons.RemoveManifestFile(revitVersion.ConvertToVersion(), pyRevitAddinName);
        }

        public static void DetachAll() {
            foreach (var revit in RevitConnector.ListInstalledRevits())
                Addons.RemoveManifestFile(revit.Version, pyRevitAddinName);
        }

        public static List<Version> GetAttachedRevitVersions() {
            var attachedRevits = new List<Version>();

            foreach (var revit in RevitConnector.ListInstalledRevits()) {
                if (Addons.GetManifestFile(revit.Version, pyRevitAddinName, allUsers: false) != null
                    || Addons.GetManifestFile(revit.Version, pyRevitAddinName, allUsers: true) != null)
                    attachedRevits.Add(revit.Version);
            }

            return attachedRevits;
        }

        public static void GetExtentions() {

        }

        public static void GetThirdPartyExtentions() {

        }

        public static void EnableExtension(string extName) {
            SetKeyValue(extName, pyRevitExtensionDisabledKey, false);
        }

        public static void DisableExtension(string extName) {
            SetKeyValue(extName, pyRevitExtensionDisabledKey, true);
        }

        public static bool IsPrimaryCloneConfigured() {
            try {
                GetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerPrimaryCloneKey);
                return true;
            }
            catch {
                return false;
            }
        }

        public static string GetPrimaryClone() {
            var primaryClone = GetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerPrimaryCloneKey);
            if (primaryClone != null && Directory.Exists(primaryClone)) {
                primaryClone = Path.GetFullPath(primaryClone);
                SetPrimaryClone(primaryClone);
                return primaryClone;
            }
            else
                return null;
        }

        public static void SetPrimaryClone(string newClonePath) {
            // make sure this clone is registered
            newClonePath = Path.GetFullPath(newClonePath);
            RegisterClone(newClonePath);
            SetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerPrimaryCloneKey, newClonePath);
        }

        public static void ClearPrimaryClone() {
            SetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerPrimaryCloneKey, "");
        }

        public static HashSet<string> GetClones() {
            var validatedClones = new HashSet<string>();

            // safely get clone list
            List<string> clonesList;
            try {
                clonesList = GetKeyValueAsList(pyRevitManagerConfigSectionName,
                                               pyRevitManagerInstalledClonesKey);
            }
            catch {
                clonesList = new List<string>();
            }

            // verify all registered clones, protect against tampering
            foreach (string clone in clonesList) {
                if (Directory.Exists(clone))
                    validatedClones.Add(Path.GetFullPath(clone));
            }
            // rewrite the verified clones list back to config file
            UpdateRegisteredClonesList(validatedClones.AsEnumerable());

            return validatedClones;
        }

        // pyrevit config getter/setter
        public static bool GetUsageReporting() {
            return Boolean.Parse(GetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLoggingStatusKey));
        }

        public static string GetUsageLogFilePath() {
            return GetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLogFilePathKey);
        }

        public static string GetUsageLogServerUrl() {
            return GetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLogServerUrlKey);
        }

        public static void EnableUsageReporting(string logFilePath = null, string logServerUrl = null) {
            SetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLoggingStatusKey, true);

            if (logFilePath != null)
                SetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLogFilePathKey, logFilePath);

            if (logServerUrl != null)
                SetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLogServerUrlKey, logServerUrl);
        }

        public static void DisableUsageReporting() {
            SetKeyValue(pyRevitUsageLoggingSection, pyRevitUsageLoggingStatusKey, false);
        }

        public static bool GetCheckUpdates() {
            return Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitCheckUpdatesKey));
        }

        public static void SetCheckUpdates(bool state) {
            SetKeyValue(pyRevitCoreConfigSection, pyRevitCheckUpdatesKey, state);
        }

        public static bool GetAutoUpdate() {
            return Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitAutoUpdateKey));
        }

        public static void SetAutoUpdate(bool state) {
            SetKeyValue(pyRevitCoreConfigSection, pyRevitAutoUpdateKey, state);
        }

        public static bool GetRocketMode() {
            return Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitRocketModeKey));
        }

        public static void SetRocketMode(bool state) {
            SetKeyValue(pyRevitCoreConfigSection, pyRevitRocketModeKey, state);
        }

        public static PyRevitLogLevels GetLoggingLevel() {
            try {
                bool verbose = Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitVerboseKey));
                bool debug = Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitDebugKey));

                if (verbose && !debug)
                    return PyRevitLogLevels.Verbose;
                else if (debug)
                    return PyRevitLogLevels.Debug;

                return PyRevitLogLevels.None;
            }
            catch {
                return PyRevitLogLevels.NotSet;
            }
        }

        public static void SetLoggingLevel(PyRevitLogLevels level) {
            if (level == PyRevitLogLevels.None) {
                SetKeyValue(pyRevitCoreConfigSection, pyRevitVerboseKey, false);
                SetKeyValue(pyRevitCoreConfigSection, pyRevitDebugKey, false);
            }

            if (level == PyRevitLogLevels.Verbose) {
                SetKeyValue(pyRevitCoreConfigSection, pyRevitVerboseKey, true);
                SetKeyValue(pyRevitCoreConfigSection, pyRevitDebugKey, false);
            }

            if (level == PyRevitLogLevels.Debug) {
                SetKeyValue(pyRevitCoreConfigSection, pyRevitVerboseKey, true);
                SetKeyValue(pyRevitCoreConfigSection, pyRevitDebugKey, true);
            }
        }

        public static bool GetFileLogging() {
            return Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitFileLoggingKey));
        }

        public static void SetFileLogging(bool state) {
            SetKeyValue(pyRevitCoreConfigSection, pyRevitFileLoggingKey, state);
        }

        public static bool GetLoadBetaTools() {
            return Boolean.Parse(GetKeyValue(pyRevitCoreConfigSection, pyRevitLoadBetaKey));
        }

        public static void SetLoadBetaTools(bool state) {
            SetKeyValue(pyRevitCoreConfigSection, pyRevitLoadBetaKey, state);
        }

        public static string GetOutputStyleSheet() {
            return GetKeyValue(pyRevitCoreConfigSection, pyRevitOutputStyleSheet);
        }

        public static void SetOutputStyleSheet(string outputCSSFilePath) {
            if (File.Exists(outputCSSFilePath))
                SetKeyValue(pyRevitCoreConfigSection, pyRevitOutputStyleSheet, outputCSSFilePath);
        }

        // generic configuration public access  ======================================================================
        public static string GetConfig(string sectionName, string paramName) {
            return GetKeyValue(sectionName, paramName);
        }

        public static void SetConfig(string sectionName, string paramName, string paramValue) {
            SetKeyValue(sectionName, paramName, paramValue);
        }

        public static void SetConfig(string sectionName, string paramName, bool paramState) {
            SetKeyValue(sectionName, paramName, paramState);
        }

        public static void SeedConfig() {
            if (File.Exists(pyRevitConfigFilePath)) {
                CommonUtils.ConfirmFile(pyRevitSeedConfigFilePath);
                File.Copy(pyRevitConfigFilePath, pyRevitSeedConfigFilePath, true);
            }
        }

        // managing clones ===========================================================================================
        // clones are git clones. pyRevit module likes to know about available clones to
        // perform operations on (switching engines, clones, uninstalling, ...)

        // register a clone in a configs
        // @handled
        public static void RegisterClone(string newClonePath) {
            logger.Debug(string.Format("Request to register: {0}", newClonePath));
            if (!Directory.Exists(newClonePath))
                throw new pyRevitResourceMissingException(newClonePath);
            else if (!GitInstaller.IsGitRepo(newClonePath))
                throw new pyRevitInvalidGitCloneException();
            else {
                var validClones = GetClones();
                validClones.Add(Path.GetFullPath(newClonePath));
                SetKeyValue(pyRevitManagerConfigSectionName,
                            pyRevitManagerInstalledClonesKey,
                            new List<string>(validClones));
            }
        }

        // unregister a clone from configs
        public static void UnregisterClone(string existigClonePath) {
            // make sure if this clone is primary, remove it
            var normalizedExClonePath = existigClonePath.NormalizeAsPath();
            var primaryClone = GetPrimaryClone();
            if (primaryClone != null
                && normalizedExClonePath == primaryClone.NormalizeAsPath())
                ClearPrimaryClone();

            // remove the clone path from list
            var remainingClones = new List<string>();
            foreach (string clonePath in GetClones()) {
                if (normalizedExClonePath != clonePath.NormalizeAsPath())
                    remainingClones.Add(Path.GetFullPath(clonePath));
            }

            SetKeyValue(pyRevitManagerConfigSectionName,
                        pyRevitManagerInstalledClonesKey,
                        new List<string>(remainingClones));
        }

        // configurations private access methods  ====================================================================
        private static IniFile GetConfigFile() {
            // INI formatting
            var cfgOps = new IniOptions();
            cfgOps.KeySpaceAroundDelimiter = true;
            IniFile cfgFile = new IniFile(cfgOps);

            // default to current user config
            string configFile = pyRevitConfigFilePath;
            // make sure the file exists and if not create an empty one
            CommonUtils.ConfirmFile(configFile);

            // load the config file
            cfgFile.Load(configFile);
            return cfgFile;
        }

        private static void SaveConfigFile(IniFile cfgFile) {
            cfgFile.Save(pyRevitConfigFilePath);
        }

        private static void UpdateKeyValue(string section, string key, string value) {
            var c = GetConfigFile();

            if (!c.Sections.Contains(section))
                c.Sections.Add(section);

            if (!c.Sections[section].Keys.Contains(key))
                c.Sections[section].Keys.Add(key);

            c.Sections[section].Keys[key].Value = value;

            SaveConfigFile(c);
        }

        private static string GetKeyValue(string sectionName, string keyName) {
            var c = GetConfigFile();
            return c.Sections[sectionName].Keys[keyName].Value;
        }

        private static List<string> GetKeyValueAsList(string section, string key) {
            var c = GetConfigFile();
            return c.Sections[section].Keys[key].Value.ConvertFromCommaSeparated();
        }

        private static void SetKeyValue(string section, string key, bool value) {
            UpdateKeyValue(section, key, value.ToString());
        }

        private static void SetKeyValue(string section, string key, int value) {
            UpdateKeyValue(section, key, value.ToString());
        }

        private static void SetKeyValue(string section, string key, string value) {
            UpdateKeyValue(section, key, value);
        }

        private static void SetKeyValue(string section, string key, IEnumerable<string> valueList) {
            UpdateKeyValue(section, key, valueList.ConvertToCommaSeparated());
        }

        private static void UpdateRegisteredClonesList(IEnumerable<string> clonePaths) {
            SetKeyValue(pyRevitManagerConfigSectionName, pyRevitManagerInstalledClonesKey, clonePaths);
        }

        // other private helprs  =====================================================================================
        private static string GetEnginePath(string repoPath, int engineVer = 000) {
            string enginesDir = Path.Combine(repoPath, "bin", "engines");
            if (!Directory.Exists(enginesDir))
                enginesDir = Path.Combine(repoPath, "pyrevitlib", "pyrevit", "loader", "addin");

            if (Directory.Exists(enginesDir)) {
                if (engineVer == 000)
                    return FindLatestEngine(enginesDir);
                else {
                    string fullEnginePath = Path.GetFullPath(Path.Combine(enginesDir, engineVer.ToString(), pyRevitDllName));
                    if (File.Exists(fullEnginePath))
                        return fullEnginePath;
                }
            }

            return null;
        }

        private static string FindLatestEngine(string enginesDir) {
            var engineFinder = new Regex(@"\d\d\d");
            int latestEnginerVer = 000;
            foreach (string subDir in Directory.GetDirectories(enginesDir)) {
                var engineDir = Path.GetFileName(subDir);
                if (engineFinder.IsMatch(engineDir)) {
                    var engineVer = int.Parse(engineDir);
                    if (engineVer > latestEnginerVer)
                        latestEnginerVer = engineVer;
                }
            }

            string fullEnginePath = Path.GetFullPath(Path.Combine(enginesDir, latestEnginerVer.ToString(), pyRevitDllName));
            if (File.Exists(fullEnginePath))
                return fullEnginePath;

            return null;
        }
    }
}
