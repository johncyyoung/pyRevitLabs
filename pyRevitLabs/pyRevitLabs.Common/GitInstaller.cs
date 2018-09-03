using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LibGit2Sharp;
using NLog;

namespace pyRevitLabs.Common {
    public enum UpdateStatus {
        UpToDate,
        FastForward,
        NonFastForward,
        Conflicts,
    }

    public static class GitInstaller {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // git identity defaults
        private const string commiterName = "eirannejad";
        private const string commiterEmail = "eirannejad@gmail.com";
        private static Identity commiterId = new Identity(commiterName, commiterEmail);


        // public methods
        // clone a repo to given destination
        // @handled
        public static Repository Clone(string repoPath, string branchName, string destPath, bool checkout = true) {
            // build options and clone
            var cloneOps = new CloneOptions() { Checkout = checkout, BranchName = branchName };

            try {
                // attempt at cloning the repo
                logger.Debug(String.Format("Cloning {0}:{1} to {2}", repoPath, branchName, destPath));
                Repository.Clone(repoPath, destPath, cloneOps);

                // make repository object and return
                return new Repository(destPath);
            }
            catch (Exception ex) {
                throw new pyRevitException(ex.Message, ex);
            }
        }

        // checkout a repo branch. Looks up remotes for that branch if the local doesn't exist
        // @handled
        public static void CheckoutBranch(string repoPath, string branchName) {
            try {
                var repo = new Repository(repoPath);

                // get local branch, or make one (and fetch from remote) if doesn't exist
                Branch targetBranch = repo.Branches[branchName];
                if (targetBranch == null) {
                    // lookup remotes for the branch otherwise
                    foreach (Remote remote in repo.Network.Remotes) {
                        Branch remoteBranch = repo.Branches[remote.Name + "/" + branchName];
                        if (remoteBranch != null) {
                            // create a local branch, with remote branch as tracking; update; and checkout
                            Branch localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
                            repo.Branches.Update(localBranch, b => b.UpstreamBranch = "refs/heads/" + branchName);
                        }
                    }
                }

                // now checkout the branch
                Commands.Checkout(repo, branchName);
            }
            catch (Exception ex) {
                throw new pyRevitException(ex.Message, ex);
            }
        }

        // rebase current branch and pull from master
        // @handled
        public static UpdateStatus ForcedUpdate(string repoPath) {
            try {
                var repo = new Repository(repoPath);
                var options = new PullOptions();
                options.FetchOptions = new FetchOptions();

                // before updating, let's first
                // forced checkout to overwrite possible local changes
                // Re: https://github.com/eirannejad/pyRevit/issues/229
                var checkoutOptions = new CheckoutOptions();
                checkoutOptions.CheckoutModifiers = CheckoutModifiers.Force;
                Commands.Checkout(repo, repo.Head, checkoutOptions);

                // now let's pull from the tracked remote
                var res = Commands.Pull(repo, new Signature("GitInstaller", commiterEmail, new DateTimeOffset(DateTime.Now)), options);

                // process the results and let user know
                if (res.Status == MergeStatus.FastForward)
                    return UpdateStatus.FastForward;
                else if (res.Status == MergeStatus.NonFastForward)
                    return UpdateStatus.NonFastForward;
                else if (res.Status == MergeStatus.Conflicts)
                    return UpdateStatus.Conflicts;

                return UpdateStatus.UpToDate;
            }
            catch (Exception ex) {
                throw new pyRevitException(ex.Message, ex);
            }
        }

        // rebase current branch to a specific commit by commit hash
        // @handled
        public static void RebaseToCommit(string repoPath, string commitHash) {
            try {
                var repo = new Repository(repoPath);

                // trying to find commit in current branch
                foreach (Commit cmt in repo.Commits) {
                    if (cmt.Id.ToString().StartsWith(commitHash)) {
                        RebaseToCommit(repo, cmt);
                        break;
                    }
                }
            }
            catch (Exception ex) {
                throw new pyRevitException(ex.Message, ex);
            }

            // if it gets here with no errors, it means commit could not be found
            // I'm avoiding throwing an exception inside my own try:catch
            throw new pyRevitException(String.Format("Can not find commit with hash \"{0}\"", commitHash));
        }

        // rebase current branch to a specific tag
        // @handled
        public static void RebaseToTag(string repoPath, string tagName) {
            try {
                var repo = new Repository(repoPath);

                // try to find the tag commit hash and rebase to that commit
                foreach (Tag tag in repo.Tags) {
                    if (tag.FriendlyName.ToLower() == tagName.ToLower()) {
                        // rebase using commit hash
                        RebaseToCommit(repoPath, tag.Target.Id.ToString());
                        return;
                    }
                }
            }
            catch (Exception ex) {
                throw new pyRevitException(ex.Message, ex);
            }

            // if it gets here with no errors, it means commit could not be found
            // I'm avoiding throwing an exception inside my own try:catch
            throw new pyRevitException(String.Format("Can not find commit targetted by tag \"{0}\"", tagName));
        }

        // check to see if a directory is a git repo
        // @handled
        public static bool IsGitRepo(string repoPath) {
            return Repository.IsValid(repoPath);
        }

        // private methods
        // rebase current branch to a specific commit
        // @handled
        private static void RebaseToCommit(Repository repo, Commit commit) {
            var tempBranch = repo.CreateBranch("rebasetemp", commit);
            repo.Rebase.Start(repo.Head, repo.Head, tempBranch, commiterId, new RebaseOptions());
            repo.Branches.Remove(tempBranch);
        }
    }
}
