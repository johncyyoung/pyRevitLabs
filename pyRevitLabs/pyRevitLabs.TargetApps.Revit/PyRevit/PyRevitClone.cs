﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

using pyRevitLabs.Common;
using pyRevitLabs.Common.Extensions;

using Nett;
using NLog;

namespace pyRevitLabs.TargetApps.Revit {
    public class PyRevitEngine {
        public PyRevitEngine(int engineVer, string enginePath) {
            Version = engineVer;
            Path = enginePath;
        }

        public override string ToString() {
            return string.Format("PyRevitEngine Version: \"{0}\" | Path: \"{1}\"", Version, Path);
        }

        public int Version { get; private set; }
        public string Path { get; private set; }

        public string LoaderPath {
            get {
                return System.IO.Path.Combine(Path, PyRevitConsts.DllName).NormalizeAsPath();
            }
        }

    }


    public class PyRevitDeployment {
        public PyRevitDeployment(string name, IEnumerable<string> paths) {
            Name = name;
            Paths = paths.ToList();
        }

        public override string ToString() {
            return string.Format("PyRevitDeployment Name: \"{0}\" | Paths: \"{1}\"",
                                 Name, Paths.ConvertToCommaSeparatedString());
        }

        public string Name { get; private set; }
        public List<string> Paths { get; private set; }
    }

    public class PyRevitClone {
        // private logger and data
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

        // constructors
        public PyRevitClone(string name, string clonePath) {
            // TODO: check repo validity?
            if (!reservedNames.Contains(name)) {
                Name = name;
                ClonePath = clonePath.NormalizeAsPath();
            }
            else
                throw new pyRevitException(string.Format("Name \"{0}\" is reserved.", name));
        }

        // properties
        public string Name { get; private set; }

        public string ClonePath { get; private set; }

        public bool IsRepoDeploy {
            get {
                try {
                    return IsDeployedWithRepo(ClonePath);
                }
                catch { return false; }
            }
        }

        public bool IsValidClone {
            get {
                try {
                    VerifyCloneValidity(ClonePath);
                    return true;
                }
                catch { return false; }
            }
        }

        public bool HasDeployments {
            get { return VerifyHasDeployments(ClonePath); }
        }

        public string Branch { get { return GetBranch(ClonePath); } }

        public string Tag { get { return GetTag(ClonePath); } }

        public string Commit { get { return GetCommit(ClonePath); } }

        // equality checks
        public override bool Equals(object obj) {
            var other = obj as PyRevitClone;

            if (ClonePath != other.ClonePath)
                return false;

            return true;
        }

        public override int GetHashCode() {
            return ClonePath.GetHashCode();
        }

        public bool Matches(string copyNameOrPath) {
            if (Name.ToLower() == copyNameOrPath.ToLower())
                return true;

            try {
                return ClonePath == copyNameOrPath.NormalizeAsPath();
            }
            catch { }

            return false;
        }

        // TODO: add container inclusion check overload

        // static methods ============================================================================================
        // public
        // determine if this is a git repo
        public static bool IsDeployedWithRepo(string clonePath) {
            return CommonUtils.VerifyPath(Path.Combine(clonePath, ".git"));
        }

        public static string GetPyRevitFilePath(string clonePath) {
            var prFile = Path.Combine(clonePath, PyRevitConsts.PyRevitfileFilename);
            if (File.Exists(prFile))
                return prFile;

            return null;
        }

        // check if path is valid pyrevit clone
        // @handled @logs
        public static bool VerifyCloneValidity(string clonePath) {
            var normClonePath = clonePath.NormalizeAsPath();
            logger.Debug(string.Format("Checking pyRevit copy validity \"{0}\"", normClonePath));
            if (CommonUtils.VerifyPath(normClonePath)) {
                // say yes if under test
                if (!GlobalConfigs.AllClonesAreValid) {
                    // determine clone validity based on directory availability
                    var pyrevitDir = Path.Combine(normClonePath, "pyrevitlib", "pyrevit").NormalizeAsPath();
                    if (!CommonUtils.VerifyPath(pyrevitDir)) {
                        throw new pyRevitInvalidpyRevitCloneException(normClonePath);
                    }

                    // if is a repo, and repo is NOT valid, throw an exception
                    if (IsDeployedWithRepo(normClonePath) && !GitInstaller.IsValidRepo(normClonePath))
                        throw new pyRevitInvalidGitCloneException(normClonePath);
                }
                logger.Debug(string.Format("Valid pyRevit clone \"{0}\"", normClonePath));
                return true;
            }

            throw new pyRevitResourceMissingException(normClonePath);
        }

        // get engine from clone path
        // returns latest with default engineVer value
        // @handled @logs
        public static PyRevitEngine GetEngine(string clonePath, int engineVer = 000) {
            logger.Debug(string.Format("Finding engine \"{0}\" path in \"{1}\"", engineVer, clonePath));
            var enginesDir = FindEnginesDirectory(clonePath);
            return FindEngine(enginesDir, engineVer: engineVer);
        }

        // get all engines from clone path
        // returns latest with default engineVer value
        // @handled @logs
        public static List<PyRevitEngine> GetEngines(string clonePath) {
            logger.Debug(string.Format("Finding engines in \"{0}\"", clonePath));
            var enginesDir = FindEnginesDirectory(clonePath);
            return FindEngines(enginesDir);
        }

        // extract deployment config from pyRevitfile inside the clone
        public static List<PyRevitDeployment> GetDeployments(string clonePath) {
            var deps = new List<PyRevitDeployment>();

            var prFile = GetPyRevitFilePath(clonePath);
            try {
                TomlTable table = Toml.ReadFile(prFile);
                var depCfgs = table.Get<TomlTable>("deployments");
                foreach (var entry in depCfgs) {
                    logger.Debug(string.Format("\"{0}\" : \"{1}\"", entry.Key, entry.Value));
                    deps.Add(
                        new PyRevitDeployment(entry.Key,
                                              new List<string>(((TomlArray)entry.Value).To<string>()))
                        );
                }
            }
            catch (Exception ex) {
                logger.Debug(string.Format("Error parsing clone \"{0}\" deployment configs at \"{1}\" | {2}",
                                           clonePath, prFile, ex.Message));
            }

            return deps;
        }

        public static bool VerifyHasDeployments(string clonePath) {
            return GetDeployments(clonePath).Count > 0;
        }

        // get checkedout branch in git repo
        // @handled @logs
        public static string GetBranch(string clonePath) {
            VerifyCloneValidity(clonePath);
            return GitInstaller.GetCheckedoutBranch(clonePath);
        }

        // get checkedout version in git repo
        // @handled @logs
        public static string GetTag(string clonePath) {
            // TODO: implement get version
            throw new NotImplementedException();
        }

        // get checkedout branch in git repo
        // @handled @logs
        public static string GetCommit(string clonePath) {
            VerifyCloneValidity(clonePath);
            return GitInstaller.GetHeadCommit(clonePath);
        }

        // checkout branch in git repo
        // @handled @logs
        public static void SetBranch(string clonePath, string branchName) {
            VerifyCloneValidity(clonePath);
            if (branchName != null)
                GitInstaller.CheckoutBranch(clonePath, branchName);
        }

        // rebase clone to specific tag
        // @handled @logs
        public static void SetTag(string clonePath, string tagName) {
            VerifyCloneValidity(clonePath);
            if (tagName != null)
                GitInstaller.RebaseToTag(clonePath, tagName);
        }

        // rebase clone to specific commit
        // @handled @logs
        public static void SetCommit(string clonePath, string commitHash) {
            VerifyCloneValidity(clonePath);
            if (commitHash != null)
                GitInstaller.RebaseToCommit(clonePath, commitHash);
        }

        // static
        // private
        // find latest engine path
        // @handled @logs
        private static PyRevitEngine FindLatestEngine(string enginesDir) {
            return FindEngine(enginesDir, engineVer: 000);
        }

        // find engine path with given version
        // @handled @logs
        private static PyRevitEngine FindEngine(string enginesDir, int engineVer = 000) {
            // engines are stored in directory named XXX based on engine version (e.g. 273)
            // return latest if zero
            if (engineVer == 000) {
                PyRevitEngine latestEnginerVer = new PyRevitEngine(000, null);

                foreach (var engine in FindEngines(enginesDir)) {
                    if (engine.Version > latestEnginerVer.Version)
                        latestEnginerVer = engine;
                }

                logger.Debug(string.Format("Latest engine path \"{0}\"", latestEnginerVer.Path ?? "NULL"));
                return latestEnginerVer;
            }
            else {
                foreach (var engine in FindEngines(enginesDir)) {
                    if (engineVer == engine.Version) {
                        logger.Debug(string.Format("Engine path \"{0}\"", engine.Path ?? "NULL"));
                        return engine;
                    }
                }
            }

            throw new pyRevitException(string.Format("Engine \"{0}\" is not available at \"{1}\"", engineVer, enginesDir));
        }

        // find all engines under a given engine path
        // @handled @logs
        private static List<PyRevitEngine> FindEngines(string enginesDir) {
            // engines are stored in directory named XXX based on engine version (e.g. 273)
            var engines = new List<PyRevitEngine>();
            var engineFinder = new Regex(@"\d\d\d");

            if (CommonUtils.VerifyPath(enginesDir)) {
                foreach (string engineDir in Directory.GetDirectories(enginesDir)) {
                    var engineDirName = Path.GetFileName(engineDir);
                    if (engineFinder.IsMatch(engineDirName)) {
                        var engineVer = int.Parse(engineDirName);
                        logger.Debug(string.Format("Engine found \"{0}\":\"{1}\"", engineDirName, engineDir));
                        engines.Add(new PyRevitEngine(engineVer, engineDir));
                    }
                }

            }
            else
                throw new pyRevitResourceMissingException(enginesDir);

            return engines;
        }

        // find engine path based on repo directory configs
        // @handled @logs
        private static string FindEnginesDirectory(string clonePath) {
            // determine repo version based on directory availability
            string enginesDir = Path.Combine(clonePath, "bin", "engines");
            if (!CommonUtils.VerifyPath(enginesDir)) {
                enginesDir = Path.Combine(clonePath, "pyrevitlib", "pyrevit", "loader", "addin");
                if (!CommonUtils.VerifyPath(enginesDir))
                    throw new pyRevitInvalidGitCloneException(clonePath);
            }

            return enginesDir;
        }

        // instance methods ==========================================================================================
        // public instance methods
        // rename clone
        public void Rename(string newName) {
            if (newName != null)
                Name = newName;
        }

        public List<PyRevitEngine> GetEngines() => GetEngines(ClonePath);

        public PyRevitEngine GetEngine(int engineVer = 000) => GetEngine(ClonePath, engineVer: engineVer);

        public List<PyRevitDeployment> GetDeployments() => GetDeployments(ClonePath);

        public void SetBranch(string branchName) => SetBranch(ClonePath, branchName);

        public void SetTag(string tagName) => SetTag(ClonePath, tagName);

        public void SetCommit(string commitHash) => SetCommit(ClonePath, commitHash);
    }
}
