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
        pyrevit install <repo_url> [--branch=<branch_name>] <dest_path>
        pyrevit uninstall [--all] [--clearconfigs] [<repo_path>]
        pyrevit setprimary <repo_path>
        pyrevit checkout <branch_name> [<repo_path>]
        pyrevit setcommit <commit_hash> [<repo_path>]
        pyrevit setversion <tag_name> [<repo_path>]
        pyrevit update [--all] [<repo_path>]
        pyrevit attach (--all | <revit_version>) [--allusers] [<repo_path>]
        pyrevit detach (--all | <revit_version>)
        pyrevit setengine <enginer_version> (--all | <revit_version>)
        pyrevit extensions install <extension_name> <dest_path>
        pyrevit extensions install <repo_url> <dest_path> [--branch=<branch_name>]
        pyrevit extensions uninstall <extension_name> <dest_path> [--branch=<branch_name>]
        pyrevit extensions paths [--allusers]
        pyrevit extensions paths (add | remove) <extensions_path> [--allusers]
        pyrevit extensions <extension_name> (enable | disable) [--allusers]
        pyrevit open
        pyrevit info
        pyrevit listrevits [--installed]
        pyrevit killrevits
        pyrevit clearcache (--all | <revit_version>)
        pyrevit dynamocompat [(enable | disable)] [--allusers]
        pyrevit allowremotedll [(enable | disable)]
        pyrevit checkupdates [(enable | disable)] [--allusers]
        pyrevit autoupdate [(enable | disable)] [--allusers]
        pyrevit rocketmode [(enable | disable)] [--allusers]
        pyrevit logs [(none | verbose | debug)] [--allusers]
        pyrevit filelogging [(enable | disable)] [--allusers]
        pyrevit loadbeta [(enable | disable)] [--allusers]
        pyrevit usagelogging [--allusers]
        pyrevit usagelogging enable [--allusers] (file | server) <dest_path>
        pyrevit usagelogging disable [--allusers]
        pyrevit outputcss [<css_path>] [--allusers]
        pyrevit config <option_path> (enable | disable) [--allusers]
        pyrevit config <option_path> [<option_value>] [--allusers]
        

    Options:
        -h --help                   Show this screen.
        -V --version                Show version.
        --core                      Install original pyRevit core only (no defualt tools).
        --all                       All applicable items.
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
                    repoPath: arguments["<repo_url>"] != null ? arguments["<repo_url>"].Value as string : null,
                    branchName: arguments["--branch"] != null ? arguments["--branch"].Value as string : null,
                    coreOnly: arguments["--core"].IsTrue,
                    purge: arguments["--purge"].IsTrue
                    );
            }


            // =======================================================================================================
            // $ pyrevit checkout <branch_name> [<repo_path>]
            // =======================================================================================================
            if (arguments["checkout"].IsTrue) {
                pyRevit.Checkout(
                    arguments["<branch_name>"] != null ? arguments["<branch_name>"].Value as string : null,
                    arguments["<repo_path>"] != null ? arguments["<repo_path>"].Value as string : null
                    );
            }

            // =======================================================================================================
            // $ pyrevit setcommit <commit_hash> [<repo_path>]
            // =======================================================================================================
            if (arguments["setcommit"].IsTrue) {
                pyRevit.SetCommit(
                    arguments["<commit_hash>"] != null ? arguments["<commit_hash>"].Value as string : null,
                    arguments["<repo_path>"] != null ? arguments["<repo_path>"].Value as string : null
                    );
            }


            // =======================================================================================================
            // $ pyrevit setversion <tag_name> [<repo_path>]
            // =======================================================================================================
            if (arguments["setversion"].IsTrue) {
                pyRevit.SetVersion(
                    arguments["<tag_name>"] != null ? arguments["<tag_name>"].Value as string : null,
                    arguments["<repo_path>"] != null ? arguments["<repo_path>"].Value as string : null
                    );
            }


            // =======================================================================================================
            // $ pyrevit update [--all] [<repo_path>]
            // =======================================================================================================
            if (arguments["update"].IsTrue) {
                pyRevit.Update(
                    repoPath: arguments["<repo_url>"] != null ? arguments["<repo_url>"].Value as string : null
                    );
            }


            // =======================================================================================================
            // $ pyrevit setprimary <repo_path>
            // =======================================================================================================
            if (arguments["setprimary"].IsTrue) {
                pyRevit.SetPrimaryClone(arguments["<repo_path>"].Value as string);
            }


            // =======================================================================================================
            // $ pyrevit extensions <extension_name> (enable | disable) [--allusers]
            // =======================================================================================================
            if (arguments["extensions"].IsTrue) {
                if (arguments["<extension_name>"] != null) {
                    string extensionName = arguments["<extension_name>"].Value as string;
                    if (arguments["enable"].IsTrue)
                        pyRevit.EnableExtension(extensionName, allUsers: arguments["--allusers"].IsTrue);
                    else if (arguments["disable"].IsTrue)
                        pyRevit.DisableExtension(extensionName, allUsers: arguments["--allusers"].IsTrue);                
                }
            }


            // =======================================================================================================
            // $ pyrevit open
            // =======================================================================================================
            if (arguments["open"].IsTrue) {
                string primaryRepo = pyRevit.GetPrimaryClone();
                System.Diagnostics.Process.Start("explorer.exe", primaryRepo);
            }


            // =======================================================================================================
            // $ pyrevit info
            // =======================================================================================================
            if (arguments["info"].IsTrue) {
                var defaultConsoleColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(String.Format("Primary Repository: {0}", pyRevit.GetPrimaryClone()));
                Console.ForegroundColor = defaultConsoleColor;
                Console.WriteLine("\nRegistered Repositories:");
                foreach (string clone in pyRevit.GetClones()) {
                    Console.WriteLine(clone);
                }
            }


            // =======================================================================================================
            // $ pyrevit listrevits [--installed]
            // =======================================================================================================
            // TODO: implement --installed
            if (arguments["listrevits"].IsTrue) {
                foreach (var revit in RevitConnector.ListRunningRevits())
                    Console.WriteLine(revit);
            }


            // =======================================================================================================
            // $ pyrevit listrevits
            // =======================================================================================================
            if (arguments["killrevits"].IsTrue) {
                RevitConnector.KillAllRunningRevits();
            }


            // =======================================================================================================
            // $ pyrevit clearcache (--all | <revit_version>)
            // =======================================================================================================
            if (arguments["clearcache"].IsTrue) {
                if (arguments["--all"].IsTrue) {
                    pyRevit.ClearAllCaches();
                }
                else if (arguments["<revit_version>"] != null) {
                    pyRevit.ClearCache(arguments["<revit_version>"].Value as string);
                }
            }


            // =======================================================================================================
            // $ pyrevit checkupdate [(enable | disable)] [--allusers]
            // =======================================================================================================
            if (arguments["checkupdates"].IsTrue) {
                if (arguments["enable"].IsFalse && arguments["disable"].IsFalse)
                    Console.WriteLine(
                        String.Format("Check Updates is {0}.",
                        pyRevit.GetCheckUpdates(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetCheckUpdates(arguments["enable"].IsTrue, allUsers: arguments["--allusers"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit autoupdate [(enable | disable)] [--allusers]
            // =======================================================================================================
            if (arguments["autoupdate"].IsTrue) {
                if (arguments["enable"].IsFalse && arguments["disable"].IsFalse)
                    Console.WriteLine(
                        String.Format("Auto Update is {0}.",
                        pyRevit.GetAutoUpdate(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetAutoUpdate(arguments["enable"].IsTrue, allUsers: arguments["--allusers"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit rocketmode [(enable | disable)] [--allusers]
            // =======================================================================================================
            if (arguments["rocketmode"].IsTrue) {
                if (arguments["enable"].IsFalse && arguments["disable"].IsFalse)
                    Console.WriteLine(
                        String.Format("Rocket Mode is {0}.",
                        pyRevit.GetRocketMode(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetRocketMode(arguments["enable"].IsTrue, allUsers: arguments["--allusers"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit logs [(none | verbose | debug)] [--allusers]
            // =======================================================================================================
            if (arguments["logs"].IsTrue) {
                if (arguments["none"].IsFalse && arguments["verbose"].IsFalse && arguments["debug"].IsFalse)
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
            // $ pyrevit filelogging [(enable | disable)] [--allusers]
            // =======================================================================================================
            if (arguments["filelogging"].IsTrue) {
                if (arguments["enable"].IsFalse && arguments["disable"].IsFalse)
                    Console.WriteLine(
                        String.Format("File Logging is {0}.",
                        pyRevit.GetFileLogging(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetFileLogging(arguments["enable"].IsTrue, allUsers: arguments["--allusers"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit loadbeta [(enable | disable)] [--allusers]
            // =======================================================================================================
            if (arguments["loadbeta"].IsTrue) {
                if (arguments["enable"].IsFalse && arguments["disable"].IsFalse)
                    Console.WriteLine(
                        String.Format("Load Beta is {0}.",
                        pyRevit.GetLoadBetaTools(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetLoadBetaTools(arguments["enable"].IsTrue, allUsers: arguments["--allusers"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit usagelogging [--allusers]
            // =======================================================================================================
            if (arguments["usagelogging"].IsTrue
                    && arguments["enable"].IsFalse
                    && arguments["disable"].IsFalse) {
                Console.WriteLine(
                    String.Format("Usage logging is {0}.",
                    pyRevit.GetUsageReporting(allUsers: arguments["--allusers"].IsTrue) ? "Enabled" : "Disabled")
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


            // =======================================================================================================
            // $ pyrevit outputcss [<css_path>] [--allusers]
            // =======================================================================================================
            if (arguments["outputcss"].IsTrue) {
                if (arguments["<css_path>"] == null)
                    Console.WriteLine(
                        String.Format("Output Style Sheet is set to: {0}",
                        pyRevit.GetOutputStyleSheet(allUsers: arguments["--allusers"].IsTrue)
                        ));
                else
                    pyRevit.SetOutputStyleSheet(arguments["<css_path>"].Value as string, allUsers: arguments["--allusers"].IsTrue);
            }

            // =======================================================================================================
            // $ pyrevit config <section_name> <option_path> [<option_value>] [--allusers]
            // $ pyrevit config (enable | disable) <section_name> <option_path> [--allusers]
            // =======================================================================================================
            if (arguments["config"].IsTrue && arguments["<option_path>"] != null) {
                // extract section and option names
                string orignalOptionValue = arguments["<option_path>"].Value as string;
                if (orignalOptionValue.Split(':').Count() == 2) {
                    string configSection = orignalOptionValue.Split(':')[0];
                    string configOption = orignalOptionValue.Split(':')[1];

                    // if no value provided, read the value
                    if (arguments["<option_value>"] == null
                                && arguments["enable"].IsFalse
                                && arguments["disable"].IsFalse)
                        Console.WriteLine(
                            String.Format("{0} = {1}",
                            configOption,
                            pyRevit.GetConfig(configSection, configOption, allUsers: arguments["--allusers"].IsTrue)
                            ));
                    // if enable | disable
                    else if (arguments["enable"].IsTrue)
                        pyRevit.SetConfig(configSection, configOption, true, allUsers: arguments["--allusers"].IsTrue);
                    else if (arguments["disable"].IsTrue)
                        pyRevit.SetConfig(configSection, configOption, false, allUsers: arguments["--allusers"].IsTrue);
                    // if custom value 
                    else if (arguments["<option_value>"] != null)
                        pyRevit.SetConfig(
                            configSection,
                            configOption,
                            arguments["<option_value>"].Value as string,
                            allUsers: arguments["--allusers"].IsTrue
                            );
                }
            }
        }
    }
}
