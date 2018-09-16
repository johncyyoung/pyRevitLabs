using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pyRevitLabs.Common;
using pyRevitLabs.Common.Extensions;

using NLog;

namespace pyRevitLabs.TargetApps.Revit {
    public class PyRevitClone {
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

        public PyRevitClone(string name, string repoPath) {
            if (!reservedNames.Contains(name)) {
                Name = name;
                RepoPath = repoPath.NormalizeAsPath();
            }
            else
                throw new pyRevitException(string.Format("Clone name \"{0}\" is a reserved name.", name));
        }

        public override string ToString() {
            return string.Format("\"{0}\" | Path: \"{1}\"", Name, RepoPath);
        }

        public override bool Equals(object obj) {
            var other = obj as PyRevitClone;

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
}
