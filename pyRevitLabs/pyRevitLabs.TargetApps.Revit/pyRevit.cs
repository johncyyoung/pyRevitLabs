using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Principal;

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
                return String.Format("Config value not set \"{0}:{1}\"", ConfigSection, ConfigKey);
            }
        }
    }

    public class pyRevitInvalidpyRevitGitCloneException : pyRevitInvalidGitCloneException {
        public pyRevitInvalidpyRevitGitCloneException() { }

        public pyRevitInvalidpyRevitGitCloneException(string invalidClonePath) : base(invalidClonePath) { }

        public override string Message {
            get {
                return string.Format("Path \"{0}\" is not a valid git pyRevit clone.", Path);
            }
        }
    }


    // DATA TYPES ====================================================================================================
    // pyrevit urls
    public static class pyRevitConsts {
        // consts for the official pyRevit repo
        public static string pyRevitOriginalRepoPath = GlobalConfigs.UnderTest ?
            @"https://github.com/eirannejad/rsparam.git" :
            @"https://github.com/eirannejad/pyRevit.git";

        public const string pyRevitExtensionsDefinitionFileUri =
            @"https://github.com/eirannejad/pyRevit/raw/master/extensions/extensions.json";

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
        public const string pyRevitManagerInstalledClonesKey = "clones";
        // extensions
        public const string pyRevitExtensionDisabledKey = "disabled";
        public const string UIExtensionDirPostfix = ".extension";
        public const string LibraryExtensionDirPostfix = ".lib";

    }

    // pyrevit log levels
    public enum PyRevitLogLevels {
        None,
        Verbose,
        Debug
    }

    // pyrevit extension types
    public enum pyRevitExtensionTypes {
        UIExtension,
        LibraryExtension,
    }

    // pyrevit bundle types
    public enum pyRevitBundleTypes {
        Tab,
        Panel,
        LinkButton,
        PushButton,
        ToggleButton,
        SmartButton,
        PullDown,
        Stack3,
        Stack2,
        SplitButton,
        SplitPushButton,
        PanelButton,
        NoButton,
    }

    // pyrevit clone
    public class pyRevitClone {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly List<string> reservedNames = new List<string>() {
            "git", "pyrevit",
            "blog", "docs", "source", "youtube", "support", "env", "clone", "clones",
            "add", "forget", "rename", "delete", "branch", "commit", "version",
            "attach", "attatched", "latest", "dynamosafe", "detached",
            "extend", "extensions", "search", "install", "uninstall", "update", "paths", "revits",
            "config", "configs", "logs", "none", "verbose", "debug", "allowremotedll", "checkupdates",
            "autoupdate", "rocketmode", "filelogging", "loadbeta", "usagelogging", "enable", "disable",
            "file", "server", "outputcss", "seed"
        };

        public pyRevitClone(string name, string repoPath) {
            if (!reservedNames.Contains(name)) {
                Name = name;
                RepoPath = repoPath.NormalizeAsPath();
            }
            else
                throw new pyRevitException(string.Format("Clone name \"{0}\" is a reserved name.", name));
        }

        public override string ToString() {
            return string.Format("\"{0}\" at \"{1}\"", Name, RepoPath);
        }

        public override bool Equals(object obj) {
            var other = obj as pyRevitClone;

            if (RepoPath != other.RepoPath)
                return false;

            return true;
        }

        public override int GetHashCode() {
            return RepoPath.GetHashCode();
        }

        public string Name { get; private set; }
        public string RepoPath { get; private set; }

        // get checkedout branch in git repo
        // @handled @logs
        public string Branch {
            get {
                return GitInstaller.GetCheckedoutBranch(RepoPath);
            }
        }

        // get checkedout branch in git repo
        // @handled @logs
        public string Commit {
            get {
                return GitInstaller.GetHeadCommit(RepoPath);
            }
        }

        // check if path is valid repo
        // @handled @logs
        public static bool VerifyRepoValidity(string repoPath) {
            if (GlobalConfigs.UnderTest)
                return true;

            if (Directory.Exists(repoPath)) {
                if (GitInstaller.IsValidRepo(repoPath)) {
                    // determine repo validity based on directory availability
                    var pyrevitDir = Path.Combine(repoPath, "pyrevitlib", "pyrevit").NormalizeAsPath();
                    if (!Directory.Exists(pyrevitDir)) {
                        throw new pyRevitInvalidpyRevitGitCloneException(repoPath);
                    }

                    logger.Debug(string.Format("Valid pyRevit directory \"{0}\"", repoPath));
                    return true;
                }

                throw new pyRevitInvalidGitCloneException(repoPath);
            }

            throw new pyRevitResourceMissingException(repoPath);
        }

        // safely check if path is valid repo
        public static bool IsValidpyRevitRepo(string repoPath) {
            try { return VerifyRepoValidity(repoPath); } catch { return false; }
        }

        // instance method variant for self checking
        public bool IsValidpyRevitRepo() {
            return IsValidpyRevitRepo(RepoPath);
        }

        public bool Matches(string cloneNameOrPath) {
            if (Name.ToLower() == cloneNameOrPath.ToLower())
                return true;

            try {
                return RepoPath == cloneNameOrPath.NormalizeAsPath();
            }
            catch { }

            return false;
        }

        // rename clone
        public void Rename(string newName) {
            if (newName != null)
                Name = newName;
        }

        // checkout branch in git repo
        // @handled @logs
        public void Checkout(string branchName) {
            if (branchName != null)
                GitInstaller.CheckoutBranch(RepoPath, branchName);
        }

        // rebase clone to specific commit
        // @handled @logs
        public void SetCommit(string commitHash) {
            if (commitHash != null)
                GitInstaller.RebaseToCommit(RepoPath, commitHash);
        }

        // rebase clone to specific tag
        // @handled @logs
        public void SetVersion(string versionTagName) {
            if (versionTagName != null)
                GitInstaller.RebaseToTag(RepoPath, versionTagName);
        }

        // force update given or all registered clones
        // @handled @logs
        public void Update() {
            // current user config
            logger.Debug(string.Format("Updating pyRevit clone {0}", this));
            var res = GitInstaller.ForcedUpdate(RepoPath);
            if (res <= UpdateStatus.Conflicts)
                throw new pyRevitException(string.Format("Error updating clone {0}", this));
        }

        // TODO: add container inclusion check overload
    }

    // pyrevit extension wrapper
    // TODO: implement dependencies
    public class pyRevitExtension {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private dynamic _jsonObj;

        public pyRevitExtension(JObject jsonObj) {
            _jsonObj = jsonObj;
        }

        public pyRevitExtension(string extensionPath) {
            InstallPath = extensionPath;
        }

        public override string ToString() { return _jsonObj.ToString(); }

        public static string MakeConfigName(string extName, pyRevitExtensionTypes extType) {
            return extType ==
                pyRevitExtensionTypes.UIExtension ?
                    extName + pyRevitConsts.UIExtensionDirPostfix : extName + pyRevitConsts.LibraryExtensionDirPostfix;
        }

        public static bool IsExtensionDirectory(string path) {
            return path.EndsWith(pyRevitConsts.UIExtensionDirPostfix)
                    || path.EndsWith(pyRevitConsts.LibraryExtensionDirPostfix);
        }

        private string GetNameFromInstall() {
            return Path.GetFileName(InstallPath)
                       .Replace(pyRevitConsts.UIExtensionDirPostfix, "")
                       .Replace(pyRevitConsts.LibraryExtensionDirPostfix, "");
        }

        public bool BuiltIn { get { return bool.Parse(_jsonObj.builtin); } }
        public bool RocketModeCompatible { get { return bool.Parse(_jsonObj.rocket_mode_compatible); } }

        public string Name {
            get {
                if (_jsonObj != null)
                    return _jsonObj.name;
                else
                    return GetNameFromInstall();
            }
        }

        public string Description { get { return _jsonObj != null ? _jsonObj.description : ""; } }

        public string Author { get { return _jsonObj != null ? _jsonObj.author : ""; } }

        public string AuthorProfile { get { return _jsonObj != null ? _jsonObj.author_url : ""; } }

        public string Url { get { return _jsonObj != null ? _jsonObj.url : ""; } }

        public string Website { get { return _jsonObj != null ? _jsonObj.website : ""; } }

        public string InstallPath { get; private set; }

        public pyRevitExtensionTypes Type {
            get {
                return _jsonObj.type == "extension" ?
                    pyRevitExtensionTypes.UIExtension : pyRevitExtensionTypes.LibraryExtension;
            }
        }

        public string ConfigName {
            get {
                return MakeConfigName(Name, Type);
            }
        }

        // force update extension
        // @handled @logs
        public void Update() {
            logger.Debug(string.Format("Updating extension \"{0}\"", Name));
            logger.Debug(string.Format("Updating extension repo at \"{0}\"", InstallPath));
            var res = GitInstaller.ForcedUpdate(InstallPath);
            if (res <= UpdateStatus.Conflicts)
                throw new pyRevitException(string.Format("Error updating extension \"{0}\" installed at \"{1}\"",
                                                         Name, InstallPath));
        }

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

        // pyrevit cache folder 
        // @reviewed
        public static string GetCacheDirectory(int revitYear) {
            return Path.Combine(pyRevitAppDataPath, revitYear.ToString());
        }

        // INSTALL UNINSTALL UPDATE ==================================================================================
        // check at least one pyRevit clone is available
        public static bool IsInstalled() {
            return GetRegisteredClones().Count > 0;
        }

        // install pyRevit by cloning from git repo
        // @handled @logs
        public static void Clone(string cloneName,
                                 bool coreOnly = false,
                                 string branchName = null,
                                 string repoPath = null,
                                 string destPath = null) {
            string repoSourcePath = repoPath ?? pyRevitConsts.pyRevitOriginalRepoPath;
            string repoBranch = branchName != null ? branchName : pyRevitConsts.pyRevitOriginalRepoMainBranch;
            logger.Debug(string.Format("Repo source determined as \"{0}:{1}\"", repoSourcePath, repoBranch));

            // determine destination path if not provided
            if (destPath == null) {
                destPath = Path.Combine(pyRevitAppDataPath, pyRevitConsts.pyRevitInstallName);
            }
            logger.Debug(string.Format("Destination path determined as \"{0}\"", destPath));

            // start the clone process
            LibGit2Sharp.Repository repo = null;
            if (coreOnly) {
                // TODO: Add core checkout option. Figure out how to checkout certain folders in libgit2sharp
                throw new NotImplementedException("Core checkout option not implemented yet.");
            }
            else {
                repo = GitInstaller.Clone(repoSourcePath, repoBranch, destPath);
            }

            // Check installation
            if (repo != null) {
                // make sure to delete the repo if error occured after cloning
                var clonedPath = repo.Info.WorkingDirectory;
                try {
                    if (pyRevitClone.VerifyRepoValidity(clonedPath)) {
                        logger.Debug(string.Format("Clone successful \"{0}\"", clonedPath));
                        RegisterClone(cloneName, clonedPath);
                    }
                }
                catch (Exception ex) {
                    logger.Debug(string.Format("Exception occured after clone complete. Deleting clone \"{0}\" | {1}",
                                               clonedPath, ex.Message));
                    try {
                        CommonUtils.DeleteDirectory(clonedPath);
                    }
                    catch (Exception delEx) {
                        logger.Error(string.Format("Error post-install cleanup on \"{0}\" | {1}",
                                                   clonedPath, delEx.Message));
                    }

                    // cleanup completed, now baloon up the exception
                    throw ex;
                }
            }
            else
                throw new pyRevitException(string.Format("Error installing pyRevit. Null repo error on \"{0}\"",
                                                         repoPath));
        }

        // uninstall primary or specified clone, has option for clearing configs
        // @handled @logs
        public static void Uninstall(pyRevitClone clone, bool clearConfigs = false) {
            logger.Debug(string.Format("Unregistering clone \"{0}\"", clone));
            UnregisterClone(clone);

            logger.Debug(string.Format("Removing directory \"{0}\"", clone.RepoPath));
            CommonUtils.DeleteDirectory(clone.RepoPath);

            if (clearConfigs)
                DeleteConfigs();
        }

        // uninstall all registered clones
        // @handled @logs
        public static void UninstallAllClones(bool clearConfigs = false) {
            foreach (var clone in GetRegisteredClones())
                Uninstall(clone, clearConfigs: false);

            if (clearConfigs)
                DeleteConfigs();
        }

        // deletes config file
        // @handled @logs
        public static void DeleteConfigs() {
            if (File.Exists(pyRevitConfigFilePath))
                try {
                    File.Delete(pyRevitConfigFilePath);
                }
                catch (Exception ex) {
                    throw new pyRevitException(string.Format("Failed deleting config file \"{0}\" | {1}",
                                                              pyRevitConfigFilePath, ex.Message));
                }
        }

        // force update given or all registered clones
        // @handled @logs
        public static void UpdateAllClones() {
            logger.Debug("Updating all pyRevit clones");
            foreach (var clone in GetRegisteredClones())
                clone.Update();
        }

        // clear cache
        // @handled @logs
        public static void ClearCache(int revitYear) {
            // make sure all revit instances are closed
            if (Directory.Exists(pyRevitAppDataPath)) {
                RevitController.KillRunningRevits(revitYear);
                CommonUtils.DeleteDirectory(GetCacheDirectory(revitYear));
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
                        ClearCache(int.Parse(dirName));
                }
            }
            else
                throw new pyRevitResourceMissingException(pyRevitAppDataPath);
        }

        // managing attachments ======================================================================================
        // attach primary or given clone to revit version
        // @handled @logs
        public static void Attach(int revitYear,
                                  pyRevitClone clone,
                                  int engineVer = 000,
                                  bool allUsers = false) {
            // make the addin manifest file
            logger.Debug(string.Format("Attaching Clone \"{0}\" @ \"{1}\" to Revit {0}",
                                        clone.Name, clone.RepoPath, revitYear));
            Addons.CreateManifestFile(revitYear,
                                      pyRevitConsts.pyRevitAddinFileName,
                                      pyRevitConsts.pyRevitAddinName,
                                      GetEnginePath(clone.RepoPath, engineVer),
                                      pyRevitConsts.pyRevitAddinId,
                                      pyRevitConsts.pyRevitAddinClassName,
                                      pyRevitConsts.pyRevitVendorId,
                                      allusers: allUsers);
        }

        // attach clone to all installed revit versions
        // @handled @logs
        public static void AttachToAll(pyRevitClone clone, int engineVer = 000, bool allUsers = false) {
            foreach (var revit in RevitController.ListInstalledRevits())
                Attach(revit.FullVersion.Major, clone, engineVer: engineVer, allUsers: allUsers);
        }

        // detach from revit version
        // @handled @logs
        public static void Detach(int revitYear) {
            logger.Debug(string.Format("Detaching from Revit {0}", revitYear));
            Addons.RemoveManifestFile(revitYear, pyRevitConsts.pyRevitAddinName);
        }

        // detach from all attached revits
        // @handled @logs
        public static void DetachAll() {
            foreach (var revit in GetAttachedRevits()) {
                Detach(revit.FullVersion.Major);
            }
        }

        public static pyRevitClone GetAttachedClone(int revitYear) {
            logger.Debug(string.Format("Querying clone attached to Revit {0}", revitYear));
            var localManif = Addons.GetManifest(revitYear, pyRevitConsts.pyRevitAddinName, allUsers: false);
            string assemblyPath = null;

            if (localManif != null)
                assemblyPath = localManif.Assembly;
            else {
                var alluserManif = Addons.GetManifest(revitYear, pyRevitConsts.pyRevitAddinName, allUsers: true);
                if (alluserManif != null)
                    assemblyPath = alluserManif.Assembly;
            }

            if (assemblyPath != null)
                foreach (var clone in GetRegisteredClones())
                    if (assemblyPath.Contains(clone.RepoPath))
                        return clone;

            return null;
        }

        // get all attached revit versions
        // @handled @logs
        public static List<RevitProduct> GetAttachedRevits() {
            var attachedRevits = new List<RevitProduct>();

            foreach (var revit in RevitController.ListInstalledRevits()) {
                logger.Debug(string.Format("Checking attachment to Revit \"{0}\"", revit.Version));
                if (Addons.GetManifest(revit.FullVersion.Major, pyRevitConsts.pyRevitAddinName, allUsers: false) != null
                    || Addons.GetManifest(revit.FullVersion.Major, pyRevitConsts.pyRevitAddinName, allUsers: true) != null) {
                    logger.Debug(string.Format("pyRevit is attached to Revit \"{0}\"", revit.Version));
                    attachedRevits.Add(revit);
                }
            }

            return attachedRevits;
        }

        // managing clones ===========================================================================================
        // clones are git clones. pyRevit module likes to know about available clones to
        // perform operations on (switching engines, clones, uninstalling, ...)

        // register a clone in a configs
        // @handled @logs
        public static void RegisterClone(string cloneName, string repoPath) {
            var normalPath = repoPath.NormalizeAsPath();
            logger.Debug(string.Format("Registering clone \"{0}\"", normalPath));
            if (pyRevitClone.VerifyRepoValidity(repoPath)) {
                logger.Debug(string.Format("Clone is valid. Registering \"{0}\"", normalPath));
                var registeredClones = GetRegisteredClones();
                var clone = new pyRevitClone(cloneName, repoPath);
                if (!registeredClones.Contains(clone)) {
                    registeredClones.Add(new pyRevitClone(cloneName, repoPath));
                    SaveRegisteredClones(registeredClones);
                }
                else
                    throw new pyRevitException(
                        string.Format("clone with repo path \"{0}\" already exists.", repoPath)
                        );
            }
        }

        // renames a clone in a configs
        // @handled @logs
        public static void RenameClone(string cloneName, string newName) {
            logger.Debug(string.Format("Renaming clone \"{0}\" to \"{1}\"", cloneName, newName));
            var registeredClones = GetRegisteredClones();
            foreach (var clone in registeredClones)
                if (clone.Name == cloneName)
                    clone.Rename(newName);
            SaveRegisteredClones(registeredClones);
        }

        // unregister a clone from configs
        // @handled @logs
        public static void UnregisterClone(pyRevitClone clone) {
            logger.Debug(string.Format("Unregistering clone \"{0}\"", clone));

            // remove the clone path from list
            var clones = GetRegisteredClones();
            clones.Remove(clone);
            SaveRegisteredClones(clones);
        }

        // unregister all clone from configs
        // @handled @logs
        public static void UnregisterAllClones() {
            logger.Debug("Unregistering all clones...");

            foreach (var clone in GetRegisteredClones())
                UnregisterClone(clone);
        }

        // return list of registered clones
        // @handled @logs
        public static List<pyRevitClone> GetRegisteredClones() {
            var validatedClones = new List<pyRevitClone>();

            // safely get clone list
            var clonesList = GetKeyValueAsDict(pyRevitConsts.pyRevitManagerConfigSectionName,
                                               pyRevitConsts.pyRevitManagerInstalledClonesKey,
                                               defaultValue: new List<string>());

            // verify all registered clones, protect against tampering
            foreach (var cloneKV in clonesList) {
                var clone = new pyRevitClone(cloneKV.Key, cloneKV.Value.NormalizeAsPath());
                if (clone.IsValidpyRevitRepo() && !validatedClones.Contains(clone)) {
                    logger.Debug(string.Format("Verified clone \"{0}={1}\"", clone.Name, clone.RepoPath));
                    validatedClones.Add(clone);
                }
            }

            // rewrite the verified clones list back to config file
            SaveRegisteredClones(validatedClones);

            return validatedClones;
        }

        // return requested registered clone
        // @handled @logs
        public static pyRevitClone GetRegisteredClone(string cloneNameOrRepoPath) {
            foreach (var clone in GetRegisteredClones())
                if (clone.Matches(cloneNameOrRepoPath))
                    return clone;

            throw new pyRevitException(string.Format("Can not find clone \"{0}\"", cloneNameOrRepoPath));
        }

        // managing extensions =======================================================================================
        private static bool CompareExtensionNames(string extName, string searchTerm) {
            var extMatcher = new Regex(searchTerm,
                                       RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            return extMatcher.IsMatch(extName);
        }

        // list registered extensions based on search pattern if provided, if not list all
        // @handled @logs
        public static List<pyRevitExtension> LookupRegisteredExtensions(string searchPattern = null) {
            var pyrevtExts = new List<pyRevitExtension>();
            string extDefJson = null;

            // download and read file
            try {
                logger.Debug(string.Format("Downloding extensions metadata file \"{0}\"...",
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
                    logger.Debug(string.Format("Registered extension \"{0}\"", ext.Name));
                    if (searchPattern != null) {
                        if (CompareExtensionNames(ext.Name, searchPattern)) {
                            logger.Debug(string.Format("\"{0}\" Matched registered extension \"{1}\"",
                                                       searchPattern, ext.Name));
                            pyrevtExts.Add(ext);
                        }
                    }
                    else
                        pyrevtExts.Add(ext);
                }
            }

            return pyrevtExts;
        }

        // return a list of installed extensions found under registered search paths
        // @handled @logs
        public static List<pyRevitExtension> GetInstalledExtensions(string searchPath = null) {
            List<string> searchPaths;
            if (searchPath == null)
                searchPaths = GetExtensionSearchPaths();
            else
                searchPaths = new List<string>() { searchPath };

            var installedExtensions = new List<pyRevitExtension>();
            foreach (var path in searchPaths) {
                logger.Debug(string.Format("Looking for installed extensions under \"{0}\"...", path));
                foreach (var subdir in Directory.GetDirectories(path)) {
                    if (pyRevitExtension.IsExtensionDirectory(subdir)) {
                        logger.Debug(string.Format("Found installed extension \"{0}\"...", subdir));
                        installedExtensions.Add(new pyRevitExtension(subdir));
                    }
                }
            }

            return installedExtensions;
        }

        // find extension installed under registered search paths
        // @handled @logs
        public static pyRevitExtension GetInstalledExtension(string extensionName) {
            logger.Debug(string.Format("Looking up installed extension \"{0}\"...", extensionName));
            foreach (var ext in GetInstalledExtensions())
                if (CompareExtensionNames(ext.Name, extensionName)) {
                    logger.Debug(string.Format("\"{0}\" Matched installed extension \"{1}\"",
                                               extensionName, ext.Name));
                    return ext;
                }

            logger.Debug(string.Format("Installed extension \"{0}\" not found.", extensionName));
            return null;
        }

        // lookup registered extension by name
        // @handled @logs
        public static pyRevitExtension FindExtension(string extensionName) {
            logger.Debug(string.Format("Looking up registered extension \"{0}\"...", extensionName));
            var matchingExts = LookupRegisteredExtensions(extensionName);
            if (matchingExts.Count == 0) {
                return GetInstalledExtension(extensionName);
            }
            else if (matchingExts.Count == 1) {
                logger.Debug(string.Format("Extension found \"{0}\"...", matchingExts[0].Name));
                return matchingExts[0];
            }
            else if (matchingExts.Count > 1)
                Errors.LatestError = ErrorCodes.MoreThanOneItemMatched;

            return null;
        }

        // installs extension from repo url
        // @handled @logs
        public static void InstallExtension(string extensionName, pyRevitExtensionTypes extensionType,
                                            string repoPath, string destPath, string branchName) {
            // make sure extension is not installed already
            var existExt = GetInstalledExtension(extensionName);
            if (existExt != null)
                throw new pyRevitException(string.Format("Extension \"{0}\" is already installed under \"{1}\"",
                                                         existExt.Name, existExt.InstallPath));

            // determine repo folder name
            // Name.extension for UI Extensions
            // Name.lib for Library Extensions
            string extDestDirName = pyRevitExtension.MakeConfigName(extensionName, extensionType);
            string finalExtRepoPath = Path.Combine(destPath, extDestDirName).NormalizeAsPath();

            // determine branch name
            branchName = branchName ?? pyRevitConsts.pyRevitExtensionRepoMainBranch;

            logger.Debug(string.Format("Extension branch name determined as \"{0}\"", branchName));
            logger.Debug(string.Format("Installing extension into \"{0}\"", finalExtRepoPath));

            // start the clone process
            var repo = GitInstaller.Clone(repoPath, branchName, finalExtRepoPath);

            // Check installation
            if (repo != null) {
                // make sure to delete the repo if error occured after cloning
                var clonedPath = repo.Info.WorkingDirectory;
                if (GitInstaller.IsValidRepo(clonedPath)) {
                    logger.Debug(string.Format("Clone successful \"{0}\"", clonedPath));
                    AddExtensionSearchPath(destPath.NormalizeAsPath());
                }
                else {
                    logger.Debug(string.Format("Invalid repo after cloning. Deleting clone \"{0}\"", repoPath));
                    try {
                        CommonUtils.DeleteDirectory(repoPath);
                    }
                    catch (Exception delEx) {
                        logger.Error(string.Format("Error post-install cleanup on \"{0}\" | {1}",
                                                   repoPath, delEx.Message));
                    }
                }
            }
            else
                throw new pyRevitException(string.Format("Error installing extension. Null repo error on \"{0}\"",
                                                         repoPath));

        }

        // installs extension
        // @handled @logs
        public static void InstallExtension(pyRevitExtension ext, string destPath, string branchName) {
            logger.Debug(string.Format("Installing extension \"{0}\"", ext.Name));
            if (Directory.Exists(destPath)) {
                InstallExtension(ext.Name, ext.Type, ext.Url, destPath, branchName);
            }
            else
                throw new pyRevitResourceMissingException(destPath);
        }

        // uninstalls an extension by repo
        // @handled @logs
        public static void RemoveExtension(string repoPath, bool removeSearchPath = false) {
            if (repoPath != null) {
                logger.Debug(string.Format("Uninstalling extension at \"{0}\"", repoPath));
                CommonUtils.DeleteDirectory(repoPath);
                // remove search path if requested
                if (removeSearchPath)
                    RemoveExtensionSearchPath(Path.GetDirectoryName(Path.GetDirectoryName(repoPath)));
            }
            else
                throw new pyRevitResourceMissingException(repoPath);
        }

        // uninstalls an extension
        // @handled @logs
        public static void UninstallExtension(pyRevitExtension ext, bool removeSearchPath = false) {
            RemoveExtension(ext.InstallPath, removeSearchPath: removeSearchPath);
        }

        // uninstalls an extension by name
        // @handled @logs
        public static void UninstallExtension(string extensionName, bool removeSearchPath = false) {
            logger.Debug(string.Format("Uninstalling extension \"{0}\"", extensionName));
            var ext = GetInstalledExtension(extensionName);
            if (ext != null)
                RemoveExtension(ext.InstallPath, removeSearchPath: removeSearchPath);
            else
                throw new pyRevitException(string.Format("Can not find extension \"{0}\"", extensionName));
        }

        // force update all extensions
        // @handled @logs
        public static void UpdateAllInstalledExtensions() {
            logger.Debug("Updating all installed extensions.");
            // update all installed extensions
            foreach (var ext in GetInstalledExtensions())
                ext.Update();
        }

        // enable extension in config
        // @handled @logs
        private static void ToggleExtension(string extName, bool state) {
            var ext = FindExtension(extName);
            if (ext != null) {
                logger.Debug(string.Format("{0} extension \"{1}\"", state ? "Enabling" : "Disabling", ext.Name));
                SetKeyValue(ext.ConfigName, pyRevitConsts.pyRevitExtensionDisabledKey, !state);
            }
            else
                throw new pyRevitException(
                    string.Format("Can not find extension or more than one extension matches \"{0}\"", extName));
        }

        // disable extension in config
        // @handled @logs
        public static void EnableExtension(string extName) {
            ToggleExtension(extName, true);
        }

        // disable extension in config
        // @handled @logs
        public static void DisableExtension(string extName) {
            ToggleExtension(extName, false);
        }

        // get list of registered extension search paths
        // @handled @logs
        public static List<string> GetExtensionSearchPaths() {
            var validatedPaths = new List<string>();
            var searchPaths = GetKeyValueAsList(pyRevitConsts.pyRevitCoreConfigSection,
                                                pyRevitConsts.pyRevitUserExtensionsKey);
            // make sure paths exist
            foreach (var path in searchPaths) {
                var normPath = path.NormalizeAsPath();
                if (Directory.Exists(path) && !validatedPaths.Contains(normPath)) {
                    logger.Debug(string.Format("Verified extension search path \"{0}\"", normPath));
                    validatedPaths.Add(normPath);
                }
            }

            // rewrite verified list
            SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection,
                        pyRevitConsts.pyRevitUserExtensionsKey,
                        validatedPaths);

            return validatedPaths;
        }

        // add extension search path
        // @handled @logs
        public static void AddExtensionSearchPath(string searchPath) {
            if (Directory.Exists(searchPath)) {
                logger.Debug(string.Format("Adding extension search path \"{0}\"", searchPath));
                var searchPaths = GetExtensionSearchPaths();
                searchPaths.Add(searchPath.NormalizeAsPath());
                SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection,
                            pyRevitConsts.pyRevitUserExtensionsKey,
                            searchPaths);
            }
            else
                throw new pyRevitResourceMissingException(searchPath);
        }

        // remove extension search path
        // @handled @logs
        public static void RemoveExtensionSearchPath(string searchPath) {
            var normPath = searchPath.NormalizeAsPath();
            logger.Debug(string.Format("Removing extension search path \"{0}\"", normPath));
            var searchPaths = GetExtensionSearchPaths();
            searchPaths.Remove(normPath);
            SetKeyValue(pyRevitConsts.pyRevitCoreConfigSection,
                        pyRevitConsts.pyRevitUserExtensionsKey,
                        searchPaths);
        }
        
        // managing extension sources ================================================================================
        public static string GetDefaultExtensionsSource() {
            return pyRevitConsts.pyRevitExtensionsDefinitionFileUri;
        }

        // managing init templates ===================================================================================
        public static void GetInitTemplate(pyRevitExtensionTypes extType) {

        }

        public static void GetInitTemplate(pyRevitBundleTypes extType) {

        }

        public static void InitExtension(pyRevitExtensionTypes extType, string destPath) {

        }

        public static void InitBundle(pyRevitBundleTypes bundleType, string destPath) {

        }

        public static void AddInitTemplatePath(string templatesPath) {

        }

        public static void RemoveInitTemplatePath(string templatesPath) {

        }

        public static List<string> GetInitTemplatePaths() {
            return new List<string>();
        }

        // managing configs ==========================================================================================
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
            logger.Debug(string.Format("Enabling usage logging... path: \"{0}\" server: {1}",
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
                    logger.Debug(string.Format("Invalid log path \"{0}\"", logFilePath));

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

        // copy config file into all users directory as seed config file
        // @handled @logs
        public static void SeedConfig(bool makeCurrentUserAsOwner = false, string setupFromTemplate = null) {
            // if setupFromTemplate is not specified: copy current config into Allusers folder
            // if setupFromTemplate is specified: copy setupFromTemplate as the main config
            string sourceFile = setupFromTemplate != null ? setupFromTemplate : pyRevitConfigFilePath;
            string targetFile = setupFromTemplate != null ? pyRevitConfigFilePath : pyRevitSeedConfigFilePath;

            logger.Debug(string.Format("Seeding config file \"{0}\" to \"{1}\"", sourceFile, targetFile));

            try {
                if (File.Exists(sourceFile)) {
                    CommonUtils.ConfirmFile(targetFile);
                    File.Copy(sourceFile, targetFile, true);

                    if (makeCurrentUserAsOwner) {
                        var fs = File.GetAccessControl(targetFile);
                        var currentUser = WindowsIdentity.GetCurrent();
                        try {
                            CommonUtils.SetFileSecurity(targetFile, currentUser.Name);
                        }
                        catch (InvalidOperationException ex) {
                            logger.Error(
                                string.Format(
                                    "You cannot assign ownership to user \"{0}\"." +
                                    "Either you don't have TakeOwnership permissions, " +
                                    "or it is not your user account. | {1}", currentUser.Name, ex.Message
                                    )
                            );
                        }
                    }
                }
            }
            catch (Exception ex) {
                throw new pyRevitException(string.Format("Failed seeding config file. | {0}", ex.Message));
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
            logger.Debug(string.Format("Saving config file \"{0}\"", pyRevitConfigFilePath));
            try {
                cfgFile.Save(pyRevitConfigFilePath);
            }
            catch (Exception ex) {
                throw new pyRevitException(string.Format("Failed to save config to \"{0}\". | {1}",
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
            logger.Debug(string.Format("Try getting config \"{0}:{1}\" ?? {2}",
                                       sectionName, keyName, defaultValue ?? "NULL"));
            if (c.Sections.Contains(sectionName) && c.Sections[sectionName].Keys.Contains(keyName))
                return c.Sections[sectionName].Keys[keyName].Value as string;
            else {
                if (defaultValue == null && throwNotSetException)
                    throw new pyRevitConfigValueNotSet(sectionName, keyName);
                else {
                    logger.Debug(string.Format("Config is not set. Returning default value \"{0}\"",
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
            logger.Debug(string.Format("Try getting config as list \"{0}:{1}\"", sectionName, keyName));
            var stringValue = GetKeyValue(sectionName, keyName, "", throwNotSetException: throwNotSetException);
            return stringValue.ConvertFromTomlListString();
        }

        // get config key value and make a string dictionary out of it
        // @handled @logs
        private static Dictionary<string, string> GetKeyValueAsDict(string sectionName,
                                                                    string keyName,
                                                                    IEnumerable<string> defaultValue = null,
                                                                    bool throwNotSetException = true) {
            logger.Debug(string.Format("Try getting config as dict \"{0}:{1}\"", sectionName, keyName));
            var stringValue = GetKeyValue(sectionName, keyName, "", throwNotSetException: throwNotSetException);
            return stringValue.ConvertFromTomlDictString();
        }

        // updates config key value, creates the config if not set yet
        // @handled @logs
        private static void UpdateKeyValue(string sectionName, string keyName, string stringValue) {
            if (stringValue != null) {
                var c = GetConfigFile();

                if (!c.Sections.Contains(sectionName)) {
                    logger.Debug(string.Format("Adding config section \"{0}\"", sectionName));
                    c.Sections.Add(sectionName);
                }

                if (!c.Sections[sectionName].Keys.Contains(keyName)) {
                    logger.Debug(string.Format("Adding config key \"{0}:{1}\"", sectionName, keyName));
                    c.Sections[sectionName].Keys.Add(keyName);
                }

                logger.Debug(string.Format("Updating config \"{0}:{1} = {2}\"", sectionName, keyName, stringValue));
                c.Sections[sectionName].Keys[keyName].Value = stringValue;

                SaveConfigFile(c);
            }
            else
                logger.Debug(string.Format("Can not set null value for \"{0}:{1}\"", sectionName, keyName));
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
        private static void SetKeyValue(string sectionName, string keyName, IEnumerable<string> listString) {
            UpdateKeyValue(sectionName, keyName, listString.ConvertToTomlListString());
        }

        // sets config key value as string dictionary
        // @handled @logs
        private static void SetKeyValue(string sectionName, string keyName, IDictionary<string, string> dictString) {
            UpdateKeyValue(sectionName, keyName, dictString.ConvertToTomlDictString());
        }

        // updates the config value for registered clones
        // @handled @logs
        private static void SaveRegisteredClones(List<pyRevitClone> clonesList) {
            var newValueDic = new Dictionary<string, string>();
            foreach (var clone in clonesList)
                newValueDic[clone.Name] = clone.RepoPath;

            SetKeyValue(pyRevitConsts.pyRevitManagerConfigSectionName,
                        pyRevitConsts.pyRevitManagerInstalledClonesKey,
                        newValueDic);
        }

        // other private helprs  =====================================================================================
        // finds the engine path for given repo based on repo version and engine location
        // @handled @logs
        private static string GetEnginePath(string repoPath, int engineVer = 000) {
            logger.Debug(string.Format("Finding engine \"{0}\" path for \"{1}\"", engineVer, repoPath));

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
                else
                    throw new pyRevitException(
                        string.Format("Engine \"{0}\" is not available at \"{1}\"", engineVer, fullEnginePath)
                        );
            }

            logger.Debug(string.Format("Determined engine path \"{0}\"", enginesDir ?? "NULL"));
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

            logger.Debug(string.Format("Latest engine path \"{0}\"", latestEnginePath ?? "NULL"));
            return latestEnginePath;
        }
    }
}
