using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LibGit2Sharp;

namespace pyRevitLabs.Common {
    public static class GitInstaller {
        // git identity defaults
        private const string commiterName = "eirannejad";
        private const string commiterEmail = "eirannejad@gmail.com";
        private static Identity commiterId = new Identity(commiterName, commiterEmail);


        // public methods
        // clone a repo to given destination
        public static Repository Clone(string repoPath, string branch, string destPath) {
            // build options and clone
            var cops = new CloneOptions() { Checkout = true, BranchName = branch };
            Repository.Clone(repoPath, destPath, cops);

            // make repository and return
            return new Repository(destPath);
        }

        // checkout a repo branch. Looks up remotes for that branch if the local doesn't exist
        public static void CheckoutBranch(string repoPath, string branchName) {
            var repo = new Repository(repoPath);

            // get local branch
            Branch targetBranch = repo.Branches[branchName];
            if (targetBranch == null) {
                // lookup remotes for the branch otherwise
                foreach(Remote remote in repo.Network.Remotes) {
                    Branch remoteBranch = repo.Branches[remote.Name + "/" + branchName];
                    if (remoteBranch != null) {
                        // create a local branch, with remote branch as tracking; update; and checkout
                        Branch localBranch = repo.CreateBranch(branchName, remoteBranch.Tip);
                        repo.Branches.Update(localBranch, b => b.UpstreamBranch = "refs/heads/" + branchName);
                        Commands.Checkout(repo, branchName);
                    }
                }
            }
            else {
                Commands.Checkout(repo, branchName);
            }
        }

        // rebase current branch and pull from master
        public static void ForcedUpdate(string repoPath) {
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
            Console.WriteLine(String.Format("Updating repo at: {0}", repoPath));
            var res = Commands.Pull(repo, new Signature("GitInstaller", commiterEmail, new DateTimeOffset(DateTime.Now)), options);

            // process the results and let user know
            if (res.Status == MergeStatus.FastForward)
                Console.WriteLine("Successfully updated repo to HEAD");
            else if (res.Status == MergeStatus.UpToDate)
                Console.WriteLine("Repo is already up to date.");
            else if (res.Status == MergeStatus.Conflicts)
                Console.WriteLine("There are conflicts to be resolved. Use the git tool to resolve conflicts.");
            else
                Console.WriteLine("Failed updating repo to HEAD");
        }

        // rebase current branch to a specific commit by commit hash
        public static void RebaseToCommit(string repoPath, string commitHash) {
            var repo = new Repository(repoPath);

            // trying to find commit in current branch
            Commit targetCommit = null;
            foreach (Commit cmt in repo.Commits) {
                if (cmt.Id.ToString().StartsWith(commitHash)) {
                    targetCommit = cmt;
                    break;
                }
            }

            if (targetCommit != null) {
                Console.WriteLine(String.Format("Target commit found: {0}", targetCommit.Id.ToString()));
                Console.WriteLine("Attempting rebase...");
                RebaseToCommit(repo, targetCommit);
                Console.WriteLine(String.Format("Rebase successful. Repo is now at commit: {0}", repo.Head.Tip.Id.ToString()));
            }
            else {
                Console.WriteLine("Could not find target commit.");
            }
        }

        // rebase current branch to a specific tag
        public static void RebaseToTag(string repoPath, string tagName) {
            var repo = new Repository(repoPath);

            // try to find the tag commit hash and rebase to that commit
            string targetCommit;
            foreach(Tag tag in repo.Tags) {
                if (tag.FriendlyName.ToLower() == tagName.ToLower()) {
                    targetCommit = tag.Target.Id.ToString();
                    RebaseToCommit(repoPath, targetCommit);
                }
            }
        }


        // private methods
        // rebase current branch to a specific commit
        private static void RebaseToCommit(Repository repo, Commit commit) {
            var tempBranch = repo.CreateBranch("rebasetemp", commit);
            repo.Rebase.Start(repo.Head, repo.Head, tempBranch, commiterId, new RebaseOptions());
            repo.Branches.Remove(tempBranch);
        }
    }
}
