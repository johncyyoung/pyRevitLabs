using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Diagnostics;

using pyRevitLabs.Common;
using pyRevitLabs.TargetApps.Revit;
using pyRevitLabs.Language.Properties;

using pyRevitManager.Properties;

using DocoptNet;

namespace pyRevitManager.Views {

    class pyRevitCLI {
        private const string helpUrl = "https://github.com/eirannejad/pyRevitLabs";
        private const string usage = @"pyrevit command line tool

    Usage:
        pyrevit (-h | --help)
        pyrevit (-V | --version)
        pyrevit help
        pyrevit install <dest_path> [--core] [--branch=<branch_name>] 
        pyrevit install <repo_url> <dest_path> [--core] [--branch=<branch_name>]
        pyrevit register <repo_path> [--allusers]
        pyrevit unregister <repo_path> [--allusers]
        pyrevit uninstall [(--all | <repo_path>)] [--clearconfigs] [--allusers]
        pyrevit setprimary <repo_path>
        pyrevit checkout <branch_name> [<repo_path>]
        pyrevit setcommit <commit_hash> [<repo_path>]
        pyrevit setversion <tag_name> [<repo_path>]
        pyrevit update [--all] [<repo_path>]
        pyrevit attach (--all | <revit_version>) [<repo_path>] [--allusers]
        pyrevit detach (--all | <revit_version>)
        pyrevit setengine latest (--all | --attached | <revit_version>) [<repo_path>] [--allusers]
        pyrevit setengine dynamosafe (--all | --attached | <revit_version>) [<repo_path>] [--allusers]
        pyrevit setengine <engine_version> (--all | --attached | <revit_version>) [<repo_path>] [--allusers]
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
        --attached                  All Revits that are configured to load pyRevit.
        --allusers                  Make changes to manifest files for all users (%programdata%).
        --authgroup=<auth_groups>   User groups authorized to use the extension.
        --branch=<branch_name>      Target git branch name.
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
            // $ pyrevit help
            // =======================================================================================================
            if (arguments["help"].IsTrue)
                Process.Start(helpUrl);


            // =======================================================================================================
            // $ pyrevit install <dest_path> [--core] [--branch=<branch_name>] 
            // $ pyrevit install <repo_url> <dest_path> [--core] [--branch=<branch_name>]
            // =======================================================================================================
            if (arguments["install"].IsTrue)
                pyRevit.Install(
                    destPath: TryGetValue(arguments, "<dest_path>"),
                    repoPath: TryGetValue(arguments, "<repo_url>"),
                    branchName: TryGetValue(arguments, "--branch"),
                    coreOnly: arguments["--core"].IsTrue
                    );


            // =======================================================================================================
            // $ pyrevit register <repo_path> [--allusers]
            // $ pyrevit unregister <repo_path> [--allusers]
            // =======================================================================================================
            if (arguments["register"].IsTrue) {
                string repoPath = TryGetValue(arguments, "<repo_path>");
                if (repoPath != null)
                    pyRevit.RegisterClone(repoPath, allUsers: arguments["--allusers"].IsTrue);
                
                switch (Errors.LatestError) {
                    case ErrorCodes.PathDoesNotExist:
                        Console.WriteLine("Path does not exist."); break;
                    case ErrorCodes.PathIsNotValidGitRepo:
                        Console.WriteLine("Path is not a valid pyRevit repository."); break;
                }
            }

            if (arguments["unregister"].IsTrue) {
                string repoPath = TryGetValue(arguments, "<repo_path>");
                if (repoPath != null)
                    pyRevit.UnregisterClone(repoPath, allUsers: arguments["--allusers"].IsTrue);
            }



            // =======================================================================================================
            // $ pyrevit uninstall [(--all | <repo_path>)] [--clearconfigs] [--allusers]
            // =======================================================================================================
            if (arguments["uninstall"].IsTrue) {
                if (arguments["--all"].IsTrue)
                    pyRevit.UninstallAllClones(
                        clearConfigs: arguments["--clearconfigs"].IsTrue,
                        allUsers: arguments["--allusers"].IsTrue
                        );
                else
                    pyRevit.Uninstall(
                        repoPath: TryGetValue(arguments, "<repo_path>"),
                        clearConfigs: arguments["--clearconfigs"].IsTrue,
                        allUsers: arguments["--allusers"].IsTrue
                        );
            }


            // =======================================================================================================
            // $ pyrevit setprimary <repo_path>
            // =======================================================================================================
            if (arguments["setprimary"].IsTrue)
                pyRevit.SetPrimaryClone(TryGetValue(arguments, "<repo_path>"));


            // =======================================================================================================
            // $ pyrevit checkout <branch_name> [<repo_path>]
            // =======================================================================================================
            if (arguments["checkout"].IsTrue)
                pyRevit.Checkout(
                    TryGetValue(arguments, "<branch_name>"),
                    TryGetValue(arguments, "<repo_path>")
                    );

            // =======================================================================================================
            // $ pyrevit setcommit <commit_hash> [<repo_path>]
            // =======================================================================================================
            if (arguments["setcommit"].IsTrue)
                pyRevit.SetCommit(
                    TryGetValue(arguments, "<commit_hash>"),
                    TryGetValue(arguments, "<repo_path>")
                    );


            // =======================================================================================================
            // $ pyrevit setversion <tag_name> [<repo_path>]
            // =======================================================================================================
            if (arguments["setversion"].IsTrue)
                pyRevit.SetVersion(
                    TryGetValue(arguments, "<tag_name>"),
                    TryGetValue(arguments, "<repo_path>")
                    );


            // =======================================================================================================
            // $ pyrevit update [--all] [<repo_path>]
            // =======================================================================================================
            if (arguments["update"].IsTrue)
                pyRevit.Update(repoPath: TryGetValue(arguments, "<repo_url>"));


            // =======================================================================================================
            // $ pyrevit attach (--all | <revit_version>) [<repo_path>] [--allusers]
            // =======================================================================================================
            if (arguments["attach"].IsTrue) {
                string revitVersion = TryGetValue(arguments, "<revit_version>");
                string repoPath = TryGetValue(arguments, "<repo_path>");

                if (revitVersion != null)
                    pyRevit.Attach(revitVersion, repoPath: repoPath, allUsers: arguments["--allusers"].IsTrue);
                else if (arguments["--all"].IsTrue)
                    pyRevit.AttachAll(repoPath: repoPath, allUsers: arguments["--allusers"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit detach (--all | <revit_version>)
            // =======================================================================================================
            if (arguments["detach"].IsTrue) {
                string revitVersion = TryGetValue(arguments, "<revit_version>");
                if (revitVersion != null)
                    pyRevit.Detach(revitVersion);
                else if (arguments["--all"].IsTrue)
                    pyRevit.DetachAll();
            }


            // =======================================================================================================
            // $ pyrevit setengine latest (--all | <revit_version>) [<repo_path>] [--allusers]
            // $ pyrevit setengine <engine_version> (--all | <revit_version>) [<repo_path>] [--allusers]
            // =======================================================================================================
            if (arguments["setengine"].IsTrue) {
                int engineVersion = -001;
                
                // switch to latest if requested
                if (arguments["latest"].IsTrue)
                    engineVersion = 000;

                // switch to latest if requested
                else if (arguments["dynamosafe"].IsTrue)
                    engineVersion = pyRevit.pyRevitDynamoCompatibleEnginerVer;

                // check to see if engine version is specified
                else {
                    string engineVersionString = TryGetValue(arguments, "<engine_version>");
                    if (engineVersionString != null)
                        engineVersion = int.Parse(engineVersionString);
                }

                if (engineVersion > -1) {
                    string revitVersion = TryGetValue(arguments, "<revit_version>");
                    string repoPath = TryGetValue(arguments, "<repo_path>");

                    if (revitVersion != null)
                        pyRevit.Attach(
                            revitVersion,
                            repoPath: repoPath,
                            engineVer: engineVersion,
                            allUsers: arguments["--allusers"].IsTrue
                            );
                    else if (arguments["--all"].IsTrue) {
                        pyRevit.AttachAll(
                            repoPath: repoPath,
                            engineVer: engineVersion,
                            allUsers: arguments["--allusers"].IsTrue
                            );
                    }
                    else if (arguments["--attached"].IsTrue) {
                        foreach (var revitVer in pyRevit.GetAttachedRevitVersions())
                            pyRevit.Attach(
                                revitVer.Major.ToString(),
                                repoPath: repoPath,
                                engineVer: engineVersion,
                                allUsers: arguments["--allusers"].IsTrue
                                );
                    }
                }
            }

            // =======================================================================================================
            // $ pyrevit extensions install <extension_name> <dest_path>
            // $ pyrevit extensions install <repo_url> <dest_path> [--branch=<branch_name>]
            // =======================================================================================================
            // TODO: Implement extensions install
            if (arguments["extensions"].IsTrue && arguments["install"].IsTrue)
                Console.WriteLine("Not Yet Implemented.");


            // =======================================================================================================
            // $ pyrevit extensions uninstall <extension_name> <dest_path> [--branch=<branch_name>]
            // =======================================================================================================
            // TODO: Implement extensions uninstall
            if (arguments["extensions"].IsTrue && arguments["uninstall"].IsTrue)
                Console.WriteLine("Not Yet Implemented.");


            // =======================================================================================================
            // $ pyrevit extensions paths [--allusers]
            // $ pyrevit extensions paths (add | remove) <extensions_path> [--allusers]
            // =======================================================================================================
            // TODO: Implement extensions paths
            if (arguments["extensions"].IsTrue && arguments["paths"].IsTrue)
                Console.WriteLine("Not Yet Implemented.");


            // =======================================================================================================
            // $ pyrevit extensions <extension_name> (enable | disable) [--allusers]
            // =======================================================================================================
            if (arguments["extensions"].IsTrue) {
                if (arguments["<extension_name>"] != null) {
                    string extensionName = TryGetValue(arguments, "<extension_name>");
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
            // TODO: List attached revits
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
            if (arguments["listrevits"].IsTrue) {
                if (arguments["--installed"].IsTrue)
                    foreach (var revit in RevitConnector.ListInstalledRevits())
                        Console.WriteLine(revit);
                else
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
                    pyRevit.ClearCache(TryGetValue(arguments, "<revit_version>"));
                }
            }


            // =======================================================================================================
            // $ pyrevit clearcache (--all | <revit_version>)
            // =======================================================================================================
            // TODO: Implement allowremotedll
            if (arguments["allowremotedll"].IsTrue)
                Console.WriteLine("Not Yet Implemented.");


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
                    pyRevit.EnableUsageReporting(logFilePath: TryGetValue(arguments, "<dest_path>"),
                                                 allUsers: arguments["--allusers"].IsTrue);
                else
                    pyRevit.EnableUsageReporting(logServerUrl: TryGetValue(arguments, "<dest_path>"),
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
                    pyRevit.SetOutputStyleSheet(
                        TryGetValue(arguments, "<css_path>"),
                        allUsers: arguments["--allusers"].IsTrue
                        );
            }


            // =======================================================================================================
            // $ pyrevit config <option_path> (enable | disable) [--allusers]
            // $ pyrevit config <option_path> [<option_value>] [--allusers]
            // =======================================================================================================
            if (arguments["config"].IsTrue && arguments["<option_path>"] != null) {
                // extract section and option names
                string orignalOptionValue = TryGetValue(arguments, "<option_path>");
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
                            TryGetValue(arguments, "<option_value>"),
                            allUsers: arguments["--allusers"].IsTrue
                            );
                }
            }
        }

        private static string TryGetValue(IDictionary<string, ValueObject> arguments, string key, string defaultValue = null) {
            return arguments[key] != null ? arguments[key].Value as string : defaultValue;
        }
    }

}
