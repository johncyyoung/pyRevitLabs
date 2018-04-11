using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using pyRevitManager.Properties;
using pyRevitLabs.TargetApps.Revit;

using DocoptNet;

namespace pyRevitManager.Views {

    class pyRevitCLI {
        private const string usage = @"pyrevit command line tool

    Usage:
        pyrevit (-h | --help)
        pyrevit (-V | --version)
        pyrevit install [--core] [--purge] [--branch=<branch_name>] <dest_path>
        pyrevit install <repo_path> [--branch=<branch_name>] <dest_path>
        pyrevit uninstall [--all] [--clear-configs]
        pyrevit setremote <repo_path>
        pyrevit checkout <branch_name>
        pyrevit setversion <commit_hash_or_tag_name>
        pyrevit update [<repo_path>]
        pyrevit attach (--all | <revit_version>) [--allusers]
        pyrevit detach (--all | <revit_version>)
        pyrevit extensions add [--allusers] <repo_path> [--branch=<branch_name>] [--authgroup=<auth_groups>] <extensions_path>
        pyrevit extensions add [--allusers] <extensions_path>
        pyrevit extensions remove [--allusers] <extension_name>
        pyrevit extensions (enable | disable) [--allusers] <extension_name>
        pyrevit open
        pyrevit info
        pyrevit listrevits [--installed]
        pyrevit killrevits [--allusers]
        pyrevit clearcache (--all | <revit_version>)
        pyrevit admintools (enable | disable) [--allusers]
        pyrevit rocketmode (enable | disable) [--allusers]
        pyrevit dynamocompat (enable | disable) [--allusers]
        pyrevit allowremotedll (enable | disable)
        pyrevit checkupdate (enable | disable) [--allusers]
        pyrevit autoupdate (enable | disable) [--allusers]
        pyrevit rocketmode (enable | disable) [--allusers]
        pyrevit logs (none | verbose | debug) [--allusers]
        pyrevit filelogging (enable | disable) [--allusers]
        pyrevit loadbeta (enable | disable) [--allusers]
        pyrevit usagereport enable [--allusers] (file | server) <dest_path>
        pyrevit usagereport disable [--allusers]
        pyrevit outputstyle [<css_path>]
        pyrevit config [--allusers]
        pyrevit config [--allusers] (enable | disable) <option_name>
        pyrevit config [--allusers] <option_name> [<option_value>]
        

    Options:
        -h --help                   Show this screen.
        -V --version                Show version.
        --core                      Install original pyRevit core only (no defualt tools).
        --purge                     Minimize installation file size.
        --allusers                  Make changes to manifest files for all users (%programdata%).
        --authgroup=<auth_groups>   User groups authorized to use the extension.
    ";

        public static void ProcessArguments(string[] args) {
            // process arguments for hidden debug mode switch
            bool debugMode = false;
            var argsList = new List<string>(args);
            if (argsList.Contains("--debug")) {
                argsList.Remove("--debug");
                debugMode = true;
            }

            // process docopt
            var arguments = new Docopt().Apply(
            usage,
            argsList,
            version: String.Format(Resources.ConsoleVersionFormat, Assembly.GetExecutingAssembly().GetName().Version.ToString()),
            exit: true
            );

            // print active arguments in debug mode
            if (debugMode)
                foreach (var argument in arguments.OrderBy(x => x.Key)) {
                    if (argument.Value != null && (argument.Value.IsTrue || argument.Value.IsString))
                        Console.WriteLine("{0} = {1}", argument.Key, argument.Value);
                }

            // now call methods based on inputs
            // $ pyrevit install
            if (arguments["install"].IsTrue) {
                pyRevit.Install(
                    destPath: arguments["<dest_path>"].Value as string,
                    repoPath: arguments["<repo_path>"] != null ? arguments["<repo_path>"].Value as string : null,
                    branchName: arguments["--branch"] != null ? arguments["--branch"].Value as string : null,
                    coreOnly: arguments["--core"].IsTrue,
                    purge: arguments["--purge"].IsTrue
                    );
            }
        }
    }
}
