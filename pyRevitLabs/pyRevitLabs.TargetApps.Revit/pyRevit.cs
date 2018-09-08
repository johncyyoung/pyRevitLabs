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
    // EXCEPTIONS ====================================================================================================
    public class pyRevitConfigValueNotSet : pyRevitException {
        public pyRevitConfigValueNotSet(string sectionName, string keyName) {
            ConfigSection = sectionName;
            ConfigKey = keyName;
        }

        public string ConfigSection { get; set; }
        public string ConfigKey { get; set; }

        public override string Message {
            get {
                return String.Format("Config value not set {0}:{1}", ConfigSection, ConfigKey);
            }
        }
    }

    // DATA TYPES ====================================================================================================
    // pyrevit urls
    public static class pyRevitConsts {
        // consts for the official pyRevit repo
        public const string pyRevitOriginalRepoPath =
            "https://github.com/eirannejad/pyRevit.git";

        public const string pyRevitExtensionsDefinitionFileUri =
            "https://github.com/eirannejad/pyRevit/raw/master/extensions/extensions.json";

        // urls
        public const string pyRevitBlogsUrl = @"https://eirannejad.github.io/pyRevit/";
        public const string pyRevitDocsUrl = @"https://pyrevit.readthedocs.io/en/latest/";
        public const string pyRevitSourceRepoUrl = @"https://github.com/eirannejad/pyRevit";
        public const string pyRevitYoutubeUrl = @"https://www.youtube.com/pyrevit";
        public const string pyRevitSupportRepoUrl = @"https://www.patreon.com/pyrevit";

            // repo info
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
    }

    // pyrevit log levels
    public enum PyRevitLogLevels {
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



        // STANDARD PATHS ============================================================================================
        // pyRevit %appdata% path
        // @reviewed
        public static string pyRevitAppDataPath {
            get {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                        pyRevitConsts.pyRevitAppdataDirName
                    );
            }
        }

        // pyRevit %programdata% path
        // @reviewed
        public static string pyRevitProgramDataPath {
            get {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                        pyRevitConsts.pyRevitAppdataDirName
                    );
            }
        }

        // pyRevit config file path
        // @reviewed
        public static string pyRevitConfigFilePath {
            get {
                return Path.Combine(pyRevitAppDataPath, pyRevitConsts.pyRevitConfigFileName);
            }
        }

        // pyRevit config file path
        // @reviewed
        public static string pyRevitSeedConfigFilePath {
            get {
                return Path.Combine(pyRevitProgramDataPath, pyRevitConsts.pyRevitConfigFileName);
            }
        }

        // INSTALL UNINSTALL =========================================================================================
        // install pyRevit by cloning from git repo
        // @handled @logs
        public static void Install(bool coreOnly = false,
                                   string branchName = null,
                                   string repoPath = null,
                                   string destPath = null) {
            string repoSourcePath = repoPath ?? pyRevitConsts.pyRevitOriginalRepoPath;
            string repoBranch = branchName != null ? branchName : pyRevitConsts.pyRevitOriginalRepoMainBranch;
            logger.Debug(string.Format("Repo source determined as {0}:{1}", repoSourcePath, repoBranch));

            // determine destination path if not provided
            if (destPath == null) {
                destPath = Path.Combine(pyRevitAppDataPath, pyRevitConsts.pyRevitInstallName);
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
                // make sure to delete the repo if error occured after cloning
                var clonedPath = repo.Info.WorkingDirectory;
                try {
                    logger.Debug(string.Format("Clone successful {0}", clonedPath));
                    SetPrimaryClone(clonedPath);
                }
                catch (Exception ex) {
                    logger.Debug(string.Format("Exception occured after clone complete. Deleting clone {0} | {1}",
                                               clonedPath, ex.Message));
                    try {
                        CommonUtils.DeleteDirectory(clonedPath);
                    }
                    catch (Exception delEx) {
                        logger.Error(string.Format("Error post-install cleanup on {0} | {1}",
                                                   clonedPath, delEx.Message));
                    }

                    // cleanup completed, now baloon up the exception
                    throw ex;
                }
            }
        }

        // uninstall primary or specified clone, has option for clearing configs
        // @handled @logs
        public static void Uninstall(string repoPath = null, bool clearConfigs = false) {
            // use primary clone as default if repoPath is not provided
            repoPath = (repoPath == null) ? GetPrimaryClone() : repoPath;

            if (repoPath != null) {
                logger.Debug(string.Format("Unregistering clone {0}", repoPath));
                UnregisterClone(repoPath);

                logger.Debug(string.Format("Removing directory {0}", repoPath));
                CommonUtils.DeleteDirectory(repoPath);

                if (clearConfigs)
                    DeleteConfigs();
            }
            else
                throw new pyRevitException("Primary clone is not set and clone path is not provided.");
        }

        // @handled @logs
        public static void UninstallAllClones(bool clearConfigs = false) {
            foreach (string clonePath in GetRegisteredClones())
                Uninstall(clonePath, clearConfigs: false);

            if (clearConfigs)
                DeleteConfigs();
        }

        // @handled @logs
        public static void DeleteConfigs() {
            if (File.Exists(pyRevitConfigFilePath))
                try {
                    File.Delete(pyRevitConfigFilePath);
                }
                catch (Exception ex) {
                    throw new pyRevitException(string.Format("Failed deleting config file {0} | {1}",
                                                              pyRevitConfigFilePath, ex.Message));
                }
        }

        // list registered extensions based on search pattern if provided, if not list all
        // @handled @logs
        public static List<pyRevitExtension> LookupRegisteredExtensions(string searchPattern = null) {
            var pyrevtExts = new List<pyRevitExtension>();
            string extDefJson = null;

            // download and read file
            try {
                logger.Debug(string.Format("Downloding extensions metadata file {0}...",
                                           pyRevitConsts.pyRevitExtensionsDefinitionFileUri));
                extDefJson = CommonUtils.DownloadFile(
                    pyRevitConsts.pyRevitExtensionsDefinitionFileUri,
                    Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "pyrevitextensions.json")
                    );
            }
            catch (Exception ex) {
                throw new pyRevitException(
                    string.Format("Error downloading extension metadata file. | {0}", ex.Message)
                    );
            }

            logger.Debug("Parsing extension metadata file...");
            dynamic extensionsObj;
            if (extDefJson != null) {
                try {
                    extensionsObj = JObject.Parse(File.ReadAllText(extDefJson));
                }
                catch (Exception ex) {
                    throw new pyRevitException(string.Format("Error parsing extension metadata. | {0}", ex.Message));
                }

                // make extension list
                foreach (JObject extObj in extensionsObj.extensions) {
                    var ext = new pyRevitExtension(extObj);
                    logger.Debug(string.Format("Registered extension {0}", ext.Name));
                    if (searchPattern != null) {
                        var extMatcher = new Regex(searchPattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
                        if (extMatcher.IsMatch(ext.Name)) {
                            logger.Debug(string.Format("Matching extension {0}", ext.Name));
                            pyrevtExts.Add(ext);
                        }
                    }
                    else
                        pyrevtExts.Add(ext);
                }
            }

            return pyrevtExts;
        }

        // lookup registered extension by name
        // @handled @logs
        public static pyRevitExtension LookupExtension(string extensionName) {
            logger.Debug(string.Format("Looking up registered extension \"{0}\"...", extensionName));
            var matchingExts = LookupRegisteredExtensions(extensionName);
            if (matchingExts.Count == 1) {
                logger.Debug(string.Format("Extension found \"{0}\"...", matchingExts[0].Name));
                return matchingExts[0];
            }
            else if (matchingExts.Count > 1)
                Errors.LatestError = ErrorCodes.MoreThanOneItemMatched;

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
                branchName = branchName ?? pyRevitConsts.pyRevitExtensionRepoMainBranch;

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

        // checkout branch in git repo
        // @handled @logs
        public static void Checkout(string branchName, string repoPath = null) {
            if (repoPath == null)
                repoPath = GetPrimaryClone();

            GitInstaller.CheckoutBranch(repoPath, branchName);
        }

        // rebase clone to specific commit
        // @handled @logs
        public static void SetCommit(string commitHash, string repoPath = null) {
            if (repoPath == null)
                repoPath = GetPrimaryClone();

            GitInstaller.RebaseToCommit(repoPath, commitHash);
        }

        // rebase clone to specific tag
        // @handled @logs
        public static void SetVersion(string versionTagName, string repoPath = null) {
            if (repoPath == null)
                repoPath = GetPrimaryClone();

            GitInstaller.RebaseToTag(repoPath, versionTagName);
        }

        // force update given or all registered clones
        // @handled @logs
        public static void Update(string repoPath = null, bool allClones = false) {
            if (allClones) {
                foreach (var clonePath in GetRegisteredClones())
                    GitInstaller.ForcedUpdate(clonePath);
            }
            else {
                if (repoPath == null)
                    repoPath = GetPrimaryClone();

                // current user config
                GitInstaller.ForcedUpdate(repoPath);
            }
        }

        // clear cache
        // @handled @logs
        public static void ClearCache(string revitVersion) {
            // make sure all revit instances are closed
            if (Directory.Exists(pyRevitAppDataPath)) {
                // TODO: implement kill by revit version?
                RevitController.KillAllRunningRevits();
                CommonUtils.DeleteDirectory(Path.Combine(pyRevitAppDataPath, revitVersion));
            }
            else
                throw new pyRevitResourceMissingException(pyRevitAppDataPath);
        }

        // clear all caches
        // @handled @logs
        public static void ClearAllCaches() {
            var cacheDirFinder = new Regex(@"\d\d\d\d");
            if (Directory.Exists(pyRevitAppDataPath)) {
                foreach (string subDir in Directory.GetDirectories(pyRevitAppDataPath)) {
                    var dirName = Path.GetFileName(subDir);
                    if (cacheDirFinder.IsMatch(dirName))
                        ClearCache(dirName);
                }
            }
            else
                throw new pyRevitResourceMissingException(pyRevitAppDataPath);
        }

        // attach primary or given clone to revit version
        // @handled @logs
        public static void Attach(string revitVersion,
                                  string repoPath = null,
                                  int engineVer = 000,
                                  bool allUsers = false) {
            // use primary repo if none is specified
            if (repoPath == null)
                repoPath = GetPrimaryClone();

            // make the addin manifest file
            logger.Debug(string.Format("Attaching to Revit {0} - {1}", revitVersion, repoPath));
            Addons.CreateManifestFile(revitVersion.ConvertToVersion(),
                                      pyRevitConsts.pyRevitAddinFileName,
                                      pyRevitConsts.pyRevitAddinName,
                                      GetEnginePath(repoPath, engineVer),
                                      pyRevitConsts.pyRevitAddinId,
                                      pyRevitConsts.pyRevitAddinClassName,
                                      pyRevitConsts.pyRevitVendorId,
                                      allusers: allUsers);
        }

        // attach clone to all installed revit versions
        // @handled @logs
        public static void AttachAll(string repoPath = null, int engineVer = 000, bool allUsers = false) {
            foreach (var revit in RevitController.ListInstalledRevits())
                Attach(revit.Version.Major.ToString(), repoPath: repoPath, engineVer: engineVer, allUsers: allUsers);
        }

        // detach from revit version
        // @handled @logs
        public static void Detach(string revitVersion) {
            logger.Debug(string.Format("Detaching from Revit {0}", revitVersion));
            Addons.RemoveManifestFile(revitVersion.ConvertToVersion(), pyRevitConsts.pyRevitAddinName);
        }

        // detach from all attached revits
        // @handled @logs
        public static void DetachAll() {
            foreach (var revitVersion in GetAttachedRevitVersions()) {
                Detach(revitVersion.ToString());
            }
        }

        public static string GetAttachedClone(Version revitVersion) {
            logger.Debug(string.Format("Querying clone attached to Revit {0}", revitVersion));
            var localManif = Addons.GetManifest(revitVersion, pyRevitConsts.pyRevitAddinName, allUsers: false);
            if (localManif != null)
                return localManif.Assembly;
            else {
                var alluserManif = Addons.GetManifest(revitVersion, pyRevitConsts.pyRevitAddinName, allUsers: true);
                if (alluserManif != null)
                    return alluserManif.Assembly;
            }

            return null;
        }

        // get all attached revit versions
        // @handled @logs
        public static List<Version> GetAttachedRevitVersions() {
            var attachedRevits = new List<Version>();

            foreach (var revit in RevitController.ListInstalledRevits()) {
                if (Addons.GetManifest(revit.Version, pyRevitConsts.pyRevitAddinName, allUsers: false) != null
                    || Addons.GetManifest(revit.Version, pyRevitConsts.pyRevitAddinName, allUsers: true) != null)
                    attachedRevits.Add(revit.Version);
            }

            return attachedRevits;
        }

        public static void GetExtentions() {

        }

        public static void GetThirdPartyExtentions() {

        }

        public static void EnableExtension(string extName) {
            SetKeyValue(extName, pyRevitConsts.pyRevitExtensionDisabledKey, false);
        }

        public static void DisableExtension(string extName) {
            SetKeyValue(extName, pyRevitConsts.pyRevitExtensionDisabledKey, true);
        }

        // managing clones ===========================================================================================
        // clones are git clones. pyRevit module likes to know about available clones to
        // perform operations on (switching engines, clones, uninstalling, ...)

        // register a clone in a configs
        // @handled @logs
        public static void RegisterClone(string newClonePath) {
            var normalPath = newClonePath.NormalizeAsPath();
            logger.Debug(string.Format("Registering clone {0}", normalPath));
            if (!Directory.Exists(normalPath))
                throw new pyRevitResourceMissingException(normalPath);
            else if (!GitInstaller.IsGitRepo(normalPath))
                throw new pyRevitInvalidGitCloneException();
            else {
                logger.Debug(string.Format("Clone is valid. Registering {0}", normalPath));
                var validClones = GetRegisteredClones();
                validClones.Add(normalPath);
                SetKeyValue(pyRevitConsts.pyRevitManagerConfigSectionName,
                            pyRevitConsts.pyRevitManagerInstalledClonesKey,
                            new List<string>(validClones));
            }
        }

        // unregister a clone from configs
        // @handled @logs
        public static void UnregisterClone(string existigClonePath, bool replacePrimary = true) {
            var normalPath = existigClonePath.NormalizeAsPath();
            logger.Debug(string.Format("Unregistering clone {0}", normalPath));

            // remove the clone path from list
            var remainingClones = new List<string>();
            foreach (string clonePath in GetRegisteredClones()) {
                if (normalPath != clonePath.NormalizeAsPath())
                    remainingClones.Add(clonePath.NormalizeAsPath());
            }

            // set a new primary if requested
            if (replacePrimary && remainingClones.Count > 0) {
                logger.Debug(string.Format("Setting new primary {0}", remainingClones[0]));
                SetPrimaryClone(remainingClones[0]);
            }
            else {
                // otherwise make sure if this clone is primary, remove it
                var primaryClone = GetPrimaryClone();
                if (primaryClone != null
                    && normalPath == primaryClone.NormalizeAsPath())
                    ClearPrimaryClone();
            }

            UpdateRegisteredClonesList(remainingClones);
        }

        // check if primary clone is configured
        // @handled @logs
        public static bool IsPrimaryCloneConfigured() {
            try {
                GetPrimaryClone();
                return true;
            }
            catch {
                return false;
            }
        }

        // returns primary clone from configs
        // @handled @logs
        public static string GetPrimaryClone() {
            // throws exception if null
            var primaryClone = GetKeyValue(pyRevitConsts.pyRevitManagerConfigSectionName,
                                           pyRevitConsts.pyRevitManagerPrimaryCloneKey);

            // otherwise test
            if (Directory.Exists(primaryClone)) {
                var fullClonePath = primaryClone.NormalizeAsPath();
                if (fullClonePath != primaryClone) {
                    logger.Debug(string.Format("Updating primary clone to full path {0}", primaryClone));
                    SetPrimaryClone(primaryClone);
                }
                return fullClonePath;
            }

            // throw an exception if path is empty or not a valid repo
            throw new pyRevitInvalidGitCloneException(primaryClone);
        }

        // set primary clone
        // @handled @logs
        public static void SetPrimaryClone(string newClonePath) {
            // make sure this clone is registered
            newClonePath = newClonePath.NormalizeAsPath();
            RegisterClone(newClonePath);

            // set primary clone config
            logger.Debug(string.Format("Setting primary clone {0}", newClonePath));
            SetKeyValue(pyRevitConsts.pyRevitManagerConfigSectionName,
                        pyRevitConsts.pyRevitManagerPrimaryCloneKey,
                        newClonePath);
        }

        // remove primay clone
        // @handled @logs
        public static void ClearPrimaryClone() {
            logger.Debug("Clearing primary clone config.");
            SetKeyValue(pyRevitConsts.pyRevitManagerConfigSectionName,
                        pyRevitConsts.pyRevitManagerPrimaryCloneKey,
                        "");
        }

        // return list of registered clones
        // @handled @logs
        public static HashSet<string> GetRegisteredClones() {
            var validatedClones = new HashSet<string>();

            // safely get clone list
            var clonesList = GetKeyValueAsList(pyRevitConsts.pyRevitManagerConfigSectionName,
                                               pyRevitConsts.pyRevitManagerInstalledClonesKey,
                                               defaultValue: new List<string>());

            // verify all registered clones, protect against tampering
            foreach (string clone in clonesList) {
                if (Directory.Exists(clone)) {
                    logger.Debug(string.Format("Verified clone {0}", clone));
                    validatedClones.Add(clone.NormalizeAsPath());
                }
            }
            // rewrite the verified clones list back to config file
            UpdateRegisteredClonesList(validatedClones.AsEnumerable());

            return validatedClones;
        }

        // pyrevit config getter/setter
        // usage logging config
        // @handled @logs
        public static bool GetUsageReporting() {
            return bool.Parse(GetKeyValue(pyRevitConsts.pyRevitUsageLoggingSection,
                                          pyRevitConsts.pyRevitUsageLoggingStatusKey));
        }

        public static string GetUsageLogFilePath() {
            return GetKeyValue(pyRevitConsts.pyRevitUsageLoggingSection,
                               pyRevitConsts.pyRevitUsageLogFilePathKey);
        }

        public static string GetUsageLogServerUrl() {
            return GetKeyValue(pyRevitConsts.pyRevitUsageLoggingSection,
                               pyRevitConsts.pyRevitUsageLogServerUrlKey);
        }

        public static void EnableUsageReporting(string logFilePath = null, string logServerUrl = null) {
            logger.Debug(string.Format("Enabling usage logging... path: {0} server: {1}",
                                       logFilePath, logServerUrl));
            SetKeyValue(pyRevitConsts.pyRevitUsageLoggingSection,
                        pyRevitConsts.pyRevitUsageLoggingStatusKey,
                        true);

            if (logFilePath != null)
                if (Directory.Exists(logFilePath))
                    SetKeyValue(pyRevitConsts.pyRevitUsageLoggingSection,
                                pyRevitConsts.pyRevitUsageLogFilePathKey,
                                logFilePath);
                else
                    logger.Debug(string.Format("Invalid log path {0}", logFilePath));

            if (logServerUrl != null)
                SetKeyValue(pyRevitConsts.pyRevitUsageLoggingSection,
                            pyRevitConsts.pyRevitUsageLogServerUrlKey,
                            logServerUrl);
        }

        public static void DisableUsageReporting() {
            logger.Debug("Disabling usage reporting...");
            SetKeyValue(pyRevitConsts.pyRevitUsageLoggingSection,
                        pyRevitConsts.pyRevitUsageLoggingStatusKey,
                        false);
        }

        // update checking config
        // @handled @logs
        public static bool GetCheckUpdates() {
            return bool.Parse(GetKeyValue(pyRevitConsts.pyRevitCoreConfigSection,
                                             pyRevitConsts.pyRevitCheckUpdatesKey));
        }

        public static void SetCheckUpdates(bool state) {
            SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitCheckUpdatesKey, state);
        }

        // auto update config
        // @handled @logs
        public static bool GetAutoUpdate() {
            return bool.Parse(GetKeyValue(pyRevitConsts.pyRevitCoreConfigSection,
                                          pyRevitConsts.pyRevitAutoUpdateKey));
        }

        public static void SetAutoUpdate(bool state) {
            SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitAutoUpdateKey, state);
        }

        // rocket mode config
        // @handled @logs
        public static bool GetRocketMode() {
            return bool.Parse(GetKeyValue(pyRevitConsts.pyRevitCoreConfigSection,
                                          pyRevitConsts.pyRevitRocketModeKey));
        }

        public static void SetRocketMode(bool state) {
            SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitRocketModeKey, state);
        }

        // logging level config
        // @handled @logs
        public static PyRevitLogLevels GetLoggingLevel() {
            bool verbose = bool.Parse(GetKeyValue(pyRevitConsts.pyRevitCoreConfigSection,
                                                  pyRevitConsts.pyRevitVerboseKey));
            bool debug = bool.Parse(GetKeyValue(pyRevitConsts.pyRevitCoreConfigSection,
                                                pyRevitConsts.pyRevitDebugKey));

            if (verbose && !debug)
                return PyRevitLogLevels.Verbose;
            else if (debug)
                return PyRevitLogLevels.Debug;

            return PyRevitLogLevels.None;
        }

        public static void SetLoggingLevel(PyRevitLogLevels level) {
            if (level == PyRevitLogLevels.None) {
                SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitVerboseKey, false);
                SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitDebugKey, false);
            }

            if (level == PyRevitLogLevels.Verbose) {
                SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitVerboseKey, true);
                SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitDebugKey, false);
            }

            if (level == PyRevitLogLevels.Debug) {
                SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitVerboseKey, true);
                SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitDebugKey, true);
            }
        }

        // file logging config
        // @handled @logs
        public static bool GetFileLogging() {
            return bool.Parse(GetKeyValue(pyRevitConsts.pyRevitCoreConfigSection,
                                          pyRevitConsts.pyRevitFileLoggingKey));
        }

        public static void SetFileLogging(bool state) {
            SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitFileLoggingKey, state);
        }

        // load beta config
        // @handled @logs
        public static bool GetLoadBetaTools() {
            return bool.Parse(GetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitLoadBetaKey));
        }

        public static void SetLoadBetaTools(bool state) {
            SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitLoadBetaKey, state);
        }

        // output style sheet config
        // @handled @logs
        public static string GetOutputStyleSheet() {
            return GetKeyValue(pyRevitConsts.pyRevitCoreConfigSection, pyRevitConsts.pyRevitOutputStyleSheet);
        }

        public static void SetOutputStyleSheet(string outputCSSFilePath) {
            if (File.Exists(outputCSSFilePath))
                SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection,
                            pyRevitConsts.pyRevitOutputStyleSheet,
                            outputCSSFilePath);
        }

        // generic configuration public access  ======================================================================
        // @handled @logs
        public static string GetConfig(string sectionName, string keyName) {
            return GetKeyValue(sectionName, keyName);
        }

        // @handled @logs
        public static void SetConfig(string sectionName, string keyName, bool boolValue) {
            SetKeyValue(sectionName, keyName, boolValue);
        }

        // @handled @logs
        public static void SetConfig(string sectionName, string keyName, int intValue) {
            SetKeyValue(sectionName, keyName, intValue);
        }

        // @handled @logs
        public static void SetConfig(string sectionName, string keyName, string stringValue) {
            SetKeyValue(sectionName, keyName, stringValue);
        }

        // @handled @logs
        public static void SetConfig(string sectionName, string keyName, IEnumerable<string> stringListValue) {
            SetKeyValue(sectionName, keyName, stringListValue);
        }

        // @handled @logs
        public static void SeedConfig() {
            try {
                if (File.Exists(pyRevitConfigFilePath)) {
                    CommonUtils.ConfirmFile(pyRevitSeedConfigFilePath);
                    File.Copy(pyRevitConfigFilePath, pyRevitSeedConfigFilePath, true);
                }
            }
            catch (Exception ex) {
                throw new pyRevitException(string.Format("Failed seeding config file. | {0} ", ex.Message));
            }
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

        // save config file to standard location
        // @handled @logs
        private static void SaveConfigFile(IniFile cfgFile) {
            logger.Debug(string.Format("Saving config file {0}", pyRevitConfigFilePath));
            try {
                cfgFile.Save(pyRevitConfigFilePath);
            }
            catch (Exception ex) {
                throw new pyRevitException(string.Format("Failed to save config to {0}. | {1}",
                                                         pyRevitConfigFilePath, ex.Message));
            }
        }

        // get config key value
        // @handled @logs
        private static string GetKeyValue(string sectionName,
                                          string keyName,
                                          string defaultValue = null,
                                          bool throwNotSetException = true) {
            var c = GetConfigFile();
            logger.Debug(string.Format("Try getting config {0}:{1} ?? {2}",
                                       sectionName, keyName, defaultValue ?? "NULL"));
            if (c.Sections.Contains(sectionName) && c.Sections[sectionName].Keys.Contains(keyName))
                return c.Sections[sectionName].Keys[keyName].Value as string;
            else {
                if (defaultValue == null && throwNotSetException)
                    throw new pyRevitConfigValueNotSet(sectionName, keyName);
                else {
                    logger.Debug(string.Format("Config is not set. Returning default value {0}",
                                               defaultValue ?? "NULL"));
                    return defaultValue;
                }
            }
        }

        // get config key value and make a string list out of it
        // @handled @logs
        private static List<string> GetKeyValueAsList(string sectionName,
                                                      string keyName,
                                                      IEnumerable<string> defaultValue = null,
                                                      bool throwNotSetException = true) {
            logger.Debug(string.Format("Try getting config as list {0}:{1}", sectionName, keyName));
            var stringValue = GetKeyValue(sectionName, keyName, "", throwNotSetException: throwNotSetException);
            return stringValue.ConvertFromCommaSeparated();
        }

        // updates config key value, creates the config if not set yet
        // @handled @logs
        private static void UpdateKeyValue(string sectionName, string keyName, string stringValue) {
            if (stringValue != null) {
                var c = GetConfigFile();

                if (!c.Sections.Contains(sectionName)) {
                    logger.Debug(string.Format("Adding config section {0}", sectionName));
                    c.Sections.Add(sectionName);
                }

                if (!c.Sections[sectionName].Keys.Contains(keyName)) {
                    logger.Debug(string.Format("Adding config key {0}:{1}", sectionName, keyName));
                    c.Sections[sectionName].Keys.Add(keyName);
                }

                logger.Debug(string.Format("Updating config {0}:{1} = {2}", sectionName, keyName, stringValue));
                c.Sections[sectionName].Keys[keyName].Value = stringValue;

                SaveConfigFile(c);
            }
            else
                logger.Debug(string.Format("Can not set null value for {0}:{1}", sectionName, keyName));
        }

        // sets config key value as bool
        // @handled @logs
        private static void SetKeyValue(string sectionName, string keyName, bool boolVaue) {
            UpdateKeyValue(sectionName, keyName, boolVaue.ToString());
        }

        // sets config key value as int
        // @handled @logs
        private static void SetKeyValue(string sectionName, string keyName, int intValue) {
            UpdateKeyValue(sectionName, keyName, intValue.ToString());
        }

        // sets config key value as string
        // @handled @logs
        private static void SetKeyValue(string sectionName, string keyName, string stringValue) {
            UpdateKeyValue(sectionName, keyName, stringValue);
        }

        // sets config key value as string list
        // @handled @logs
        private static void SetKeyValue(string sectionName, string keyName, IEnumerable<string> stringListValue) {
            UpdateKeyValue(sectionName, keyName, stringListValue.ConvertToCommaSeparated());
        }

        // updates the config value for registered clones
        // @handled @logs
        private static void UpdateRegisteredClonesList(IEnumerable<string> clonePaths) {
            SetKeyValue(pyRevitConsts.pyRevitManagerConfigSectionName,
                        pyRevitConsts.pyRevitManagerInstalledClonesKey,
                        clonePaths);
        }

        // other private helprs  =====================================================================================
        // finds the engine path for given repo based on repo version and engine location
        // @handled @logs
        private static string GetEnginePath(string repoPath, int engineVer = 000) {
            logger.Debug(string.Format("Finding engine {0} path for {1}", engineVer, repoPath));

            // determine repo version based on directory availability
            string enginesDir = Path.Combine(repoPath, "bin", "engines");
            if (!Directory.Exists(enginesDir)) {
                enginesDir = Path.Combine(repoPath, "pyrevitlib", "pyrevit", "loader", "addin");
                if (!Directory.Exists(enginesDir))
                    throw new pyRevitInvalidGitCloneException(repoPath);
            }

            // now determine engine path; latest or requested
            if (engineVer == 000) {
                enginesDir = FindLatestEngine(enginesDir);
            }
            else {
                string fullEnginePath = Path.Combine(enginesDir,
                                                     engineVer.ToString(),
                                                     pyRevitConsts.pyRevitDllName).NormalizeAsPath();
                if (File.Exists(fullEnginePath))
                    enginesDir = fullEnginePath;
            }

            logger.Debug(string.Format("Determined engine path {0}", enginesDir ?? "NULL"));
            return enginesDir;
        }

        // find latest engine path
        // @handled @logs
        private static string FindLatestEngine(string enginesDir) {
            // engines are stored in directory named XXX based on engine version (e.g. 273)
            var engineFinder = new Regex(@"\d\d\d");
            int latestEnginerVer = 000;
            string latestEnginePath = null;

            if (Directory.Exists(enginesDir)) {
                foreach (string subDir in Directory.GetDirectories(enginesDir)) {
                    var engineDir = Path.GetFileName(subDir);
                    if (engineFinder.IsMatch(engineDir)) {
                        var engineVer = int.Parse(engineDir);
                        if (engineVer > latestEnginerVer)
                            latestEnginerVer = engineVer;
                    }
                }

                string fullEnginePath = Path.Combine(enginesDir,
                                                     latestEnginerVer.ToString(),
                                                     pyRevitConsts.pyRevitDllName).NormalizeAsPath();
                if (File.Exists(fullEnginePath))
                    latestEnginePath = fullEnginePath;
            }
            else
                throw new pyRevitResourceMissingException(enginesDir);

            logger.Debug(string.Format("Latest engine path {0}", latestEnginePath ?? "NULL"));
            return latestEnginePath;
        }
    }
}
