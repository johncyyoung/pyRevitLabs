using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

using pyRevitManager.Properties;
using pyRevitLabs.TargetApps.Revit;
using pyRevitLabs.Language.Properties;

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
        pyrevit update [--all] [<repo_path>]
        pyrevit attach (--all | <revit_version>) [--allusers] [<repo_path>]
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
        pyrevit dynamocompat (get | enable | disable) [--allusers]
        pyrevit allowremotedll (get | enable | disable)
        pyrevit checkupdates (get | enable | disable) [--allusers]
        pyrevit autoupdate (get | enable | disable) [--allusers]
        pyrevit rocketmode (get | enable | disable) [--allusers]
        pyrevit logs (get | none | verbose | debug) [--allusers]
        pyrevit filelogging (get | enable | disable) [--allusers]
        pyrevit loadbeta (get | enable | disable) [--allusers]
        pyrevit usagelogging get [--allusers]
        pyrevit usagelogging enable [--allusers] (file | server) <dest_path>
        pyrevit usagelogging disable [--allusers]
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
                version: String.Format(StringLib.ConsoleVersionFormat,
                                       Assembly.GetExecutingAssembly().GetName().Version.ToString()),
                exit: true,
                help: true
            );

            // print active arguments in debug mode
            if (debugMode)
                foreach (var argument in arguments.OrderBy(x => x.Key)) {
                    if (argument.Value != null && (argument.Value.IsTrue || argument.Value.IsString))
                        Console.WriteLine("{0} = {1}", argument.Key, argument.Value);
                }


            // now call methods based on inputs
            // =======================================================================================================
            // $ pyrevit install
            // =======================================================================================================
            if (arguments["install"].IsTrue) {
                pyRevit.Install(
                    destPath: arguments["<dest_path>"].Value as string,
                    repoPath: arguments["<repo_path>"] != null ? arguments["<repo_path>"].Value as string : null,
                    branchName: arguments["--branch"] != null ? arguments["--branch"].Value as string : null,
                    coreOnly: arguments["--core"].IsTrue,
                    purge: arguments["--purge"].IsTrue
                    );
            }


            // =======================================================================================================
            // $ pyrevit checkupdate (get | enable | disable) [--allusers]
            // =======================================================================================================
            if (arguments["checkupdates"].IsTrue) {
                if (arguments["get"].IsTrue)
                    Console.WriteLine(
                        String.Format("Check Updates is {0}.",
                        pyRevit.GetCheckUpdates(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetCheckUpdates(arguments["enable"].IsTrue, allUsers: arguments["--allusers"].IsTrue);
            }

            
            // =======================================================================================================
            // $ pyrevit autoupdate (get | enable | disable) [--allusers]
            // =======================================================================================================
            if (arguments["autoupdate"].IsTrue) {
                if (arguments["get"].IsTrue)
                    Console.WriteLine(
                        String.Format("Auto Update is {0}.",
                        pyRevit.GetAutoUpdate(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetAutoUpdate(arguments["enable"].IsTrue, allUsers: arguments["--allusers"].IsTrue);
            }

            
            // =======================================================================================================
            // $ pyrevit rocketmode (get | enable | disable) [--allusers]
            // =======================================================================================================
            if (arguments["rocketmode"].IsTrue) {
                if (arguments["get"].IsTrue)
                    Console.WriteLine(
                        String.Format("Rocket Mode is {0}.",
                        pyRevit.GetRocketMode(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetRocketMode(arguments["enable"].IsTrue, allUsers: arguments["--allusers"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit logs(get | none | verbose | debug) [--allusers]
            // =======================================================================================================
            if (arguments["logs"].IsTrue) {
                if (arguments["get"].IsTrue)
                    Console.WriteLine(String.Format("Logging Level is {0}.", pyRevit.GetLoggingLevel().ToString()));
                else {
                    if (arguments["none"].IsTrue)
                        pyRevit.SetLoggingLevel(PyRevitLogLevels.None, allUsers: arguments["--allusers"].IsTrue);
                    else if (arguments["verbose"].IsTrue)
                        pyRevit.SetLoggingLevel(PyRevitLogLevels.Verbose, allUsers: arguments["--allusers"].IsTrue);
                    else if (arguments["debug"].IsTrue)
                        pyRevit.SetLoggingLevel(PyRevitLogLevels.Debug, allUsers: arguments["--allusers"].IsTrue);
                }
            }


            // =======================================================================================================
            // $ pyrevit filelogging (get | enable | disable) [--allusers]
            // =======================================================================================================
            if (arguments["filelogging"].IsTrue) {
                if (arguments["get"].IsTrue)
                    Console.WriteLine(
                        String.Format("File Logging is {0}.",
                        pyRevit.GetFileLogging(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetFileLogging(arguments["enable"].IsTrue, allUsers: arguments["--allusers"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit loadbeta (get | enable | disable) [--allusers]
            // =======================================================================================================
            if (arguments["loadbeta"].IsTrue) {
                if (arguments["get"].IsTrue)
                    Console.WriteLine(
                        String.Format("Load Beta is {0}.",
                        pyRevit.GetLoadBetaTools(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetLoadBetaTools(arguments["enable"].IsTrue, allUsers: arguments["--allusers"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit usagelogging get [--allusers]
            // =======================================================================================================
            if (arguments["usagelogging"].IsTrue && arguments["get"].IsTrue) {
                Console.WriteLine(
                    String.Format("Usage logging is {0}.",
                    pyRevit.GetUsageReporting(allUsers: arguments["--allusers"].IsTrue) ? "Enabled":"Disabled")
                    );
                Console.WriteLine(String.Format("Log File Path: {0}",
                                  pyRevit.GetUsageLogFilePath(allUsers: arguments["--allusers"].IsTrue)));
                Console.WriteLine(String.Format("Log Server Url: {0}",
                                  pyRevit.GetUsageLogServerUrl(allUsers: arguments["--allusers"].IsTrue)));
            }


            // =======================================================================================================
            // $ pyrevit usagelogging enable [--allusers] (file | server) <dest_path>
            // =======================================================================================================
            if (arguments["usagelogging"].IsTrue && arguments["enable"].IsTrue) {
                if (arguments["file"].IsTrue)
                    pyRevit.EnableUsageReporting(logFilePath: arguments["<dest_path>"].Value as string,
                                                 allUsers: arguments["--allusers"].IsTrue);
                else
                    pyRevit.EnableUsageReporting(logServerUrl: arguments["<dest_path>"].Value as string,
                                                 allUsers: arguments["--allusers"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit usagelogging disable [--allusers]
            // =======================================================================================================
            if (arguments["usagelogging"].IsTrue && arguments["disable"].IsTrue) {
                pyRevit.DisableUsageReporting(allUsers: arguments["--allusers"].IsTrue);
            }
        }

    }
}
