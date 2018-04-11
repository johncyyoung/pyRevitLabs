using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LibGit2Sharp;

namespace pyRevitLabs.Common {
    public static class GitInstaller {

        public static Repository Clone(string repoPath, string branch, string destPath) {
            // build options and clone
            var cops = new CloneOptions() { Checkout = true, BranchName = branch};
            Repository.Clone(repoPath, destPath, cops);

            // make repository and return
            return new Repository(destPath);
        }

        //static void Update(string repoPath, ) {
        //    var repo = new Repository(repoPath);
        //    var options = new PullOptions();
        //    options.FetchOptions = new FetchOptions();

        //    // before updating, let's first
        //    // forced checkout to overwrite possible local changes
        //    // Re: https://github.com/eirannejad/pyRevit/issues/229
        //    var checkoutOptions = new CheckoutOptions();
        //    checkoutOptions.CheckoutModifiers = CheckoutModifiers.Force;
        //    Commands.Checkout(repo, repo.Head, checkoutOptions);

        //    // now let's pull from the tracked remote
        //    Console.WriteLine(String.Format("Updating repo at: {0}", repoPath));
        //    var res = Commands.Pull(repo, new Signature("pyRevitCoreUpdater", commiterEmail, new DateTimeOffset(DateTime.Now)), options);

        //    // process the results and let user know
        //    if (res.Status == MergeStatus.FastForward)
        //        Console.WriteLine("Successfully updated repo to HEAD");
        //    else if (res.Status == MergeStatus.UpToDate)
        //        Console.WriteLine("Repo is already up to date.");
        //    else if (res.Status == MergeStatus.Conflicts)
        //        Console.WriteLine("There are conflicts to be resolved. Use the git tool to resolve conflicts.");
        //    else
        //        Console.WriteLine("Failed updating repo to HEAD");
        //}

        //static void ChangeVersion(List<string> rebaseArgs) {
        //    if (rebaseArgs.Count == 2) {
        //        var repoPath = rebaseArgs[0];
        //        var commitHash = rebaseArgs[1];

        //        try {
        //            var repo = new Repository(repoPath);

        //            // trying to find commit in current branch
        //            Commit desCommit = null;
        //            foreach (Commit cmt in repo.Commits) {
        //                if (cmt.Id.ToString().StartsWith(commitHash)) {
        //                    desCommit = cmt;
        //                    break;
        //                }
        //            }

        //            if (desCommit != null) {
        //                Console.WriteLine(String.Format("Target commit found: {0}", desCommit.Id.ToString()));
        //                Console.WriteLine("Attempting rebase...");
        //                var tempBranch = repo.CreateBranch("rebasetemp", desCommit);
        //                repo.Rebase.Start(repo.Head, repo.Head, tempBranch, commiterId, new RebaseOptions());
        //                repo.Branches.Remove(tempBranch);
        //                Console.WriteLine(String.Format("Rebase successful. Repo is now at commit: {0}", repo.Head.Tip.Id.ToString()));
        //            }
        //            else {
        //                Console.WriteLine("Could not find target commit.");
        //            }
        //        }
        //        catch (Exception ex) {
        //            Console.WriteLine(String.Format("EXCEPTION: {0}", ex.Message));
        //        }

        //    }
        //    else {
        //        Console.WriteLine("Provide repo path and target commit hash.");
        //    }
        //}
    }
}
