using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Diagnostics;

using pyRevitLabs.Common;
using pyRevitLabs.Common.Extensions;
using pyRevitLabs.TargetApps.Revit;
using pyRevitLabs.Language.Properties;

using DocoptNet;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace pyRevitManager.Views {
    public enum pyRevitManagerLogLevel {
        Quiet,
        InfoMessages,
        Debug,
    }

    class pyRevitCLI {
        private static Logger logger = null;

        private const string helpUrl = "https://github.com/eirannejad/pyRevitLabs";
        private const string usage = @"pyrevit command line tool

    Usage:
        pyrevit (-h | --help)
        pyrevit (-V | --version)
        pyrevit help
        pyrevit blog
        pyrevit docs
        pyrevit source
        pyrevit youtube
        pyrevit support
        pyrevit install [--core] [--branch=<branch_name>] [<dest_path>]
        pyrevit install <repo_url> <dest_path> [--core] [--branch=<branch_name>]
        pyrevit register <repo_path>
        pyrevit unregister <repo_path>
        pyrevit uninstall [(--all | <repo_path>)] [--clearconfigs]
        pyrevit setprimary <repo_path>
        pyrevit checkout <branch_name> [<repo_path>]
        pyrevit setcommit <commit_hash> [<repo_path>]
        pyrevit setversion <tag_name> [<repo_path>]
        pyrevit update [--all] [<repo_path>]
        pyrevit attach (--all | <revit_version>) [<repo_path>] [--allusers]
        pyrevit detach (--all | <revit_version>)
        pyrevit setengine latest (--all | --attached | <revit_version>) [<repo_path>]
        pyrevit setengine dynamosafe (--all | --attached | <revit_version>) [<repo_path>]
        pyrevit setengine <engine_version> (--all | --attached | <revit_version>) [<repo_path>]
        pyrevit extensions list
        pyrevit extensions search [<search_pattern>]
        pyrevit extensions info <extension_name>
        pyrevit extensions help <extension_name>
        pyrevit extensions open <extension_name>
        pyrevit extensions install <extension_name> <dest_path> [--branch=<branch_name>]
        pyrevit extensions install (ui | lib) <extension_name> <repo_url> <dest_path> [--branch=<branch_name>]
        pyrevit extensions uninstall <extension_name> <dest_path> [--branch=<branch_name>]
        pyrevit extensions paths
        pyrevit extensions paths (add | remove) <extensions_path>
        pyrevit extensions (enable | disable) <extension_name>
        pyrevit open
        pyrevit info
        pyrevit revit list [--installed]
        pyrevit revit killall
        pyrevit revit fileinfo <file_path>
        pyrevit clearcache (--all | <revit_version>)
        pyrevit allowremotedll [(enable | disable)]
        pyrevit checkupdates [(enable | disable)]
        pyrevit autoupdate [(enable | disable)]
        pyrevit rocketmode [(enable | disable)]
        pyrevit logs [(none | verbose | debug)]
        pyrevit filelogging [(enable | disable)]
        pyrevit loadbeta [(enable | disable)]
        pyrevit usagelogging
        pyrevit usagelogging enable (file | server) <dest_path>
        pyrevit usagelogging disable
        pyrevit outputcss [<css_path>]
        pyrevit config seed
        pyrevit config <option_path> (enable | disable)
        pyrevit config <option_path> [<option_value>]
        

    Options:
        -h --help                   Show this screen.
        -V --version                Show version.
        --debug                     Print docopt options and logger debug messages.
        --quiet                     Do not print any logger messages.
        --core                      Install original pyRevit core only (no defualt tools).
        --all                       All applicable items.
        --attached                  All Revits that are configured to load pyRevit.
        --authgroup=<auth_groups>   User groups authorized to use the extension.
        --branch=<branch_name>      Target git branch name.
";

        public static void ProcessArguments(string[] args) {
            // process arguments for hidden debug mode switch
            pyRevitManagerLogLevel logLevel = pyRevitManagerLogLevel.InfoMessages;

            // setup logger
            var config = new LoggingConfiguration();
            var consoleTarget = new ConsoleTarget("target1") {
                Layout = @"${level}: ${message} ${exception}"
            };
            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget);
            LogManager.Configuration = config;
            // disable debug by default
            foreach (var rule in LogManager.Configuration.LoggingRules)
                rule.DisableLoggingForLevel(LogLevel.Debug);

            // process arguments for logging level
            var argsList = new List<string>(args);

            if (argsList.Contains("--quiet")) {
                argsList.Remove("--quiet");
                logLevel = pyRevitManagerLogLevel.Quiet;
                LogManager.DisableLogging();
            }

            if (argsList.Contains("--debug")) {
                argsList.Remove("--debug");
                logLevel = pyRevitManagerLogLevel.Debug;
                foreach (var rule in LogManager.Configuration.LoggingRules)
                    rule.EnableLoggingForLevel(LogLevel.Debug);
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
            if (logLevel == pyRevitManagerLogLevel.Debug)
                foreach (var argument in arguments.OrderBy(x => x.Key)) {
                    if (argument.Value != null && (argument.Value.IsTrue || argument.Value.IsString))
                        Console.WriteLine("{0} = {1}", argument.Key, argument.Value);
                }

            // now call methods based on inputs
            // get logger
            logger = LogManager.GetCurrentClassLogger();
            // get active keys for safe command extraction
            var activeKeys = ExtractEnabledKeywords(arguments);
            // =======================================================================================================
            // $ pyrevit blog
            // $ pyrevit docs
            // $ pyrevit source
            // $ pyrevit youtubes
            // $ pyrevit support
            // =======================================================================================================
            if (VerifyCommand(activeKeys, "blog")) {
                Process.Start(pyRevitConsts.pyRevitBlogsUrl);

                ProcessErrorCodes();
            }
            else if (VerifyCommand(activeKeys, "docs")) {
                Process.Start(pyRevitConsts.pyRevitDocsUrl);

                ProcessErrorCodes();
            }
            else if (VerifyCommand(activeKeys, "source")) {
                Process.Start(pyRevitConsts.pyRevitSourceRepoUrl);

                ProcessErrorCodes();
            }
            else if (VerifyCommand(activeKeys, "youtube")) {
                Process.Start(pyRevitConsts.pyRevitYoutubeUrl);

                ProcessErrorCodes();
            }
            else if (VerifyCommand(activeKeys, "support")) {
                Process.Start(pyRevitConsts.pyRevitSupportRepoUrl);

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit help
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "help")) {
                Process.Start(helpUrl);

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit install [--core] [--branch=<branch_name>] [<dest_path>]
            // $ pyrevit install <repo_url> <dest_path> [--core] [--branch=<branch_name>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "install")) {
                try {
                    pyRevit.Install(
                        coreOnly: arguments["--core"].IsTrue,
                        branchName: TryGetValue(arguments, "--branch"),
                        repoPath: TryGetValue(arguments, "<repo_url>"),
                        destPath: TryGetValue(arguments, "<dest_path>")
                        );
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit register <repo_path>
            // $ pyrevit unregister <repo_path>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "register")) {
                string repoPath = TryGetValue(arguments, "<repo_path>");
                if (repoPath != null) {
                    try {
                        pyRevit.RegisterClone(repoPath);
                    }
                    catch (pyRevitException ex) {
                        LogException(ex, logLevel);
                    }
                }

                ProcessErrorCodes();
            }

            else if (VerifyCommand(activeKeys, "unregister")) {
                string repoPath = TryGetValue(arguments, "<repo_path>");
                if (repoPath != null)
                    try {
                        pyRevit.UnregisterClone(repoPath);
                    }
                    catch (pyRevitException ex) {
                        LogException(ex, logLevel);
                    }

                ProcessErrorCodes();
            }



            // =======================================================================================================
            // $ pyrevit uninstall [(--all | <repo_path>)] [--clearconfigs]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "uninstall")) {
                try {
                    if (arguments["--all"].IsTrue)
                        pyRevit.UninstallAllClones(clearConfigs: arguments["--clearconfigs"].IsTrue);
                    else
                        pyRevit.Uninstall(
                            repoPath: TryGetValue(arguments, "<repo_path>"),
                            clearConfigs: arguments["--clearconfigs"].IsTrue
                            );
                }
                catch (pyRevitException ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit setprimary <repo_path>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "setprimary")) {
                try {
                    pyRevit.SetPrimaryClone(TryGetValue(arguments, "<repo_path>"));
                }
                catch (pyRevitException ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit checkout <branch_name> [<repo_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "checkout")) {
                try {
                    pyRevit.Checkout(
                        TryGetValue(arguments, "<branch_name>"),
                        TryGetValue(arguments, "<repo_path>")
                        );
                }
                catch (pyRevitException ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }

            // =======================================================================================================
            // $ pyrevit setcommit <commit_hash> [<repo_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "setcommit")) {
                try {
                    pyRevit.SetCommit(
                        TryGetValue(arguments, "<commit_hash>"),
                        TryGetValue(arguments, "<repo_path>")
                        );
                }
                catch (pyRevitException ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit setversion <tag_name> [<repo_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "setversion")) {
                try {
                    pyRevit.SetVersion(
                        TryGetValue(arguments, "<tag_name>"),
                        TryGetValue(arguments, "<repo_path>")
                        );
                }
                catch (pyRevitException ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit update [--all] [<repo_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "update")) {
                try {
                    pyRevit.Update(repoPath: TryGetValue(arguments, "<repo_url>"));
                }
                catch (pyRevitException ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit attach (--all | <revit_version>) [<repo_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "attach")) {
                string revitVersion = TryGetValue(arguments, "<revit_version>");
                string repoPath = TryGetValue(arguments, "<repo_path>");

                try {
                    if (revitVersion != null)
                        pyRevit.Attach(int.Parse(revitVersion), repoPath: repoPath, allUsers: arguments["--allusers"].IsTrue);
                    else if (arguments["--all"].IsTrue)
                        pyRevit.AttachAll(repoPath: repoPath, allUsers: arguments["--allusers"].IsTrue);
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit detach (--all | <revit_version>)
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "detach")) {
                string revitVersion = TryGetValue(arguments, "<revit_version>");

                try {
                    if (revitVersion != null)
                        pyRevit.Detach(int.Parse(revitVersion));
                    else if (arguments["--all"].IsTrue)
                        pyRevit.DetachAll();
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit setengine latest (--all | <revit_version>) [<repo_path>]
            // $ pyrevit setengine <engine_version> (--all | <revit_version>) [<repo_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "setengine")) {
                int engineVersion = -001;

                // switch to latest if requested
                if (arguments["latest"].IsTrue)
                    engineVersion = 000;

                // switch to latest if requested
                else if (arguments["dynamosafe"].IsTrue)
                    engineVersion = pyRevitConsts.pyRevitDynamoCompatibleEnginerVer;

                // check to see if engine version is specified
                else {
                    string engineVersionString = TryGetValue(arguments, "<engine_version>");
                    if (engineVersionString != null)
                        engineVersion = int.Parse(engineVersionString);
                }

                if (engineVersion > -1) {
                    string revitVersion = TryGetValue(arguments, "<revit_version>");
                    string repoPath = TryGetValue(arguments, "<repo_path>");

                    try {
                        if (revitVersion != null)
                            pyRevit.Attach(
                                int.Parse(revitVersion),
                                repoPath: repoPath,
                                engineVer: engineVersion
                                );
                        else if (arguments["--all"].IsTrue) {
                            pyRevit.AttachAll(
                                repoPath: repoPath,
                                engineVer: engineVersion
                                );
                        }
                        else if (arguments["--attached"].IsTrue) {
                            foreach (var revit in pyRevit.GetAttachedRevits())
                                pyRevit.Attach(
                                    revit.FullVersion.Major,
                                    repoPath: repoPath,
                                    engineVer: engineVersion
                                    );
                        }
                    }
                    catch (Exception ex) {
                        LogException(ex, logLevel);
                    }

                    ProcessErrorCodes();
                }
            }


            // =======================================================================================================
            // $ pyrevit extensions search [<search_pattern>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "search")) {
                string searchPattern = TryGetValue(arguments, "<search_pattern>");
                try {
                    var extList = pyRevit.LookupRegisteredExtensions(searchPattern);
                    Console.WriteLine("==> UI Extensions");
                    foreach (pyRevitExtension ext in extList)
                        Console.WriteLine(String.Format("{0}{1}", ext.Name.PadRight(24), ext.Url));
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }

            // =======================================================================================================
            // $ pyrevit extensions info <extension_name>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "info")) {
                string extName = TryGetValue(arguments, "<extension_name>");

                try {
                    if (extName != null) {
                        var ext = pyRevit.FindExtension(extName);
                        if (ext != null)
                            Console.WriteLine(ext.ToString());
                        else if (Errors.LatestError == ErrorCodes.MoreThanOneItemMatched)
                            logger.Warn(string.Format("More than one extension matches the search pattern \"{0}\"",
                                                      extName));
                    }
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit extensions help <extension_name>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "help")) {
                string extName = TryGetValue(arguments, "<extension_name>");

                try {
                    if (extName != null) {
                        var ext = pyRevit.FindExtension(extName);
                        if (ext != null)
                            Process.Start(ext.Website);
                        else if (Errors.LatestError == ErrorCodes.MoreThanOneItemMatched)
                            logger.Warn(string.Format("More than one extension matches the search pattern \"{0}\"",
                                                      extName));
                    }
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit extensions install <extension_name> <dest_path> [--branch=<branch_name>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "install")) {
                string destPath = TryGetValue(arguments, "<dest_path>");
                string extName = TryGetValue(arguments, "<extension_name>");
                string branchName = TryGetValue(arguments, "--branch");

                try {
                    var ext = pyRevit.FindExtension(extName);
                    if (ext != null) {
                        logger.Debug(string.Format("Matching extension found \"{0}\"", ext.Name));
                        pyRevit.InstallExtension(ext, destPath, branchName);
                    }
                    else {
                        if (Errors.LatestError == ErrorCodes.MoreThanOneItemMatched)
                            throw new pyRevitException(
                                string.Format("More than one extension matches the name \"{0}\"",
                                                extName));
                        else 
                            throw new pyRevitException(
                                string.Format("Not valid extension name or repo url \"{0}\"",
                                                extName));
                    }
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit extensions install (ui | lib) <extension_name> <repo_url> <dest_path> [--branch=<branch_name>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "install", "ui")) {
                string destPath = TryGetValue(arguments, "<dest_path>");
                string extName = TryGetValue(arguments, "<extension_name>");
                string repoUrl = TryGetValue(arguments, "<repo_url>");
                string branchName = TryGetValue(arguments, "--branch");

                try {
                    if (repoUrl.IsValidUrl())
                        pyRevit.InstallExtension(extName, pyRevitExtensionTypes.UIExtension,
                                                 repoUrl, destPath, branchName);
                    else
                        logger.Error(string.Format("Repo url is not valid \"{0}\"", repoUrl));
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }

            else if (VerifyCommand(activeKeys, "extensions", "install", "lib")) {
                string destPath = TryGetValue(arguments, "<dest_path>");
                string repoUrl = TryGetValue(arguments, "<repo_url>");
                string extName = TryGetValue(arguments, "<extension_name>");
                string branchName = TryGetValue(arguments, "--branch");

                try {
                    if (repoUrl.IsValidUrl())
                        pyRevit.InstallExtension(extName, pyRevitExtensionTypes.LibraryExtension,
                                                 repoUrl, destPath, branchName);
                    else
                        logger.Error(string.Format("Repo url is not valid \"{0}\"", repoUrl));
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit extensions uninstall <extension_name> <dest_path> [--branch=<branch_name>]
            // =======================================================================================================
            // TODO: Implement extensions uninstall
            else if (VerifyCommand(activeKeys, "extensions", "uninstall")) {
                logger.Error("Not Yet Implemented.");

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit extensions paths
            // $ pyrevit extensions paths (add | remove) <extensions_path>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "paths")) {
                Console.WriteLine("==> Extension Search Paths:");
                foreach(var searchPath in pyRevit.GetExtensionSearchPaths())
                    Console.WriteLine(searchPath);

                ProcessErrorCodes();
            }

            else if (VerifyCommand(activeKeys, "extensions", "paths", "add")) {
                var searchPath = TryGetValue(arguments, "<extensions_path>");
                if (searchPath != null) {
                    try {
                        pyRevit.AddExtensionSearchPath(searchPath);
                    }
                    catch (Exception ex) {
                        LogException(ex, logLevel);
                    }
                }

                ProcessErrorCodes();
            }

            else if (VerifyCommand(activeKeys, "extensions", "paths", "remove")) {
                var searchPath = TryGetValue(arguments, "<extensions_path>");
                if (searchPath != null) {
                    try {
                        pyRevit.RemoveExtensionSearchPath(searchPath);
                    }
                    catch (Exception ex) {
                        LogException(ex, logLevel);
                    }
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit extensions (enable | disable) <extension_name>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "enable")) {
                if (arguments["<extension_name>"] != null) {
                    string extensionName = TryGetValue(arguments, "<extension_name>");
                    if (extensionName != null)
                        pyRevit.EnableExtension(extensionName);
                }

                ProcessErrorCodes();
            }

            else if (VerifyCommand(activeKeys, "extensions", "disable")) {
                if (arguments["<extension_name>"] != null) {
                    string extensionName = TryGetValue(arguments, "<extension_name>");
                    if (extensionName != null)
                        pyRevit.DisableExtension(extensionName);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit open
            // =======================================================================================================
            else if (arguments["open"].IsTrue) {
                try {
                    string primaryRepo = pyRevit.GetPrimaryClone();
                    Process.Start("explorer.exe", primaryRepo);
                }
                catch (pyRevitConfigValueNotSet) {
                    logger.Error("Primary repo is not set. Run with \"--debug\" for details.");
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit info
            // =======================================================================================================
            else if (arguments["info"].IsTrue) {
                try {
                    // reprt primary repo
                    Console.WriteLine("==> Primary Repository:");
                    Console.WriteLine(pyRevit.IsInstalled() ? pyRevit.GetPrimaryClone() : "Not Set");

                    // report registered repos
                    Console.WriteLine("\n==> Registered Repositories:");
                    foreach (string clone in pyRevit.GetRegisteredClones()) {
                        Console.WriteLine(clone);
                    }

                    // report attached revits
                    Console.WriteLine("\n==> Attached to Revit versions:");
                    foreach (var revit in pyRevit.GetAttachedRevits()) {
                        var attachedClone = pyRevit.GetAttachedClone(revit.FullVersion.Major);
                        Console.WriteLine(string.Format("{0} <==> {1}", revit.Version, attachedClone));
                    }
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit revit list [--installed]
            // =======================================================================================================
            else if (arguments["revit"].IsTrue && arguments["list"].IsTrue) {
                if (arguments["--installed"].IsTrue)
                    foreach (var revit in RevitController.ListInstalledRevits())
                        Console.WriteLine(revit);
                else
                    foreach (var revit in RevitController.ListRunningRevits())
                        Console.WriteLine(revit);
            }


            // =======================================================================================================
            // $ pyrevit revit killall
            // =======================================================================================================
            else if (arguments["revit"].IsTrue && arguments["killall"].IsTrue) {
                RevitController.KillAllRunningRevits();
            }

            // =======================================================================================================
            // $ pyrevit revit fileinfo <model_path>
            // =======================================================================================================
            else if (arguments["revit"].IsTrue && arguments["fileinfo"].IsTrue) {
                try {
                    var modelPath = TryGetValue(arguments, "<file_path>");
                    if (modelPath != null) {
                        var model = new RevitModelFile(modelPath);
                        Console.WriteLine(
                            string.Format("Created in: {0} ({1}({2}))",
                                          model.RevitProduct.ProductName,
                                          model.RevitProduct.BuildNumber,
                                          model.RevitProduct.BuildTarget));
                        Console.WriteLine(string.Format("Workshared: {0}", model.IsWorkshared ? "Yes" : "No"));
                        if (model.IsWorkshared)
                            Console.WriteLine(string.Format("Central Model Path: {0}", model.CentralModelPath));
                        Console.WriteLine(string.Format("Last Saved Path: {0}", model.LastSavedPath));
                        Console.WriteLine(string.Format("Document Id: {0}", model.UniqueId));
                    }
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }

                ProcessErrorCodes();
            }


            // =======================================================================================================
            // $ pyrevit clearcache (--all | <revit_version>)
            // =======================================================================================================
            else if (arguments["clearcache"].IsTrue) {
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
            else if (arguments["allowremotedll"].IsTrue) {
                logger.Error("Not Yet Implemented.");
            }


            // =======================================================================================================
            // $ pyrevit checkupdate [(enable | disable)]
            // =======================================================================================================
            else if (arguments["checkupdates"].IsTrue) {
                if (arguments["enable"].IsFalse && arguments["disable"].IsFalse)
                    try {
                        Console.WriteLine(
                            String.Format("Check Updates is {0}.",
                            pyRevit.GetCheckUpdates() ? "Enabled" : "Disabled")
                            );
                    }
                    catch (Exception ex) {
                        LogException(ex, logLevel);
                    }
                else
                    pyRevit.SetCheckUpdates(arguments["enable"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit autoupdate [(enable | disable)]
            // =======================================================================================================
            else if (arguments["autoupdate"].IsTrue) {
                if (arguments["enable"].IsFalse && arguments["disable"].IsFalse)
                    Console.WriteLine(
                        String.Format("Auto Update is {0}.",
                        pyRevit.GetAutoUpdate() ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetAutoUpdate(arguments["enable"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit rocketmode [(enable | disable)]
            // =======================================================================================================
            else if (arguments["rocketmode"].IsTrue) {
                if (arguments["enable"].IsFalse && arguments["disable"].IsFalse)
                    Console.WriteLine(
                        String.Format("Rocket Mode is {0}.",
                        pyRevit.GetRocketMode() ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetRocketMode(arguments["enable"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit logs [(none | verbose | debug)]
            // =======================================================================================================
            else if (arguments["logs"].IsTrue) {
                if (arguments["none"].IsFalse && arguments["verbose"].IsFalse && arguments["debug"].IsFalse)
                    Console.WriteLine(String.Format("Logging Level is {0}.", pyRevit.GetLoggingLevel().ToString()));
                else {
                    if (arguments["none"].IsTrue)
                        pyRevit.SetLoggingLevel(PyRevitLogLevels.None);
                    else if (arguments["verbose"].IsTrue)
                        pyRevit.SetLoggingLevel(PyRevitLogLevels.Verbose);
                    else if (arguments["debug"].IsTrue)
                        pyRevit.SetLoggingLevel(PyRevitLogLevels.Debug);
                }
            }


            // =======================================================================================================
            // $ pyrevit filelogging [(enable | disable)]
            // =======================================================================================================
            else if (arguments["filelogging"].IsTrue) {
                if (arguments["enable"].IsFalse && arguments["disable"].IsFalse)
                    Console.WriteLine(
                        String.Format("File Logging is {0}.",
                        pyRevit.GetFileLogging() ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetFileLogging(arguments["enable"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit loadbeta [(enable | disable)]
            // =======================================================================================================
            else if (arguments["loadbeta"].IsTrue) {
                if (arguments["enable"].IsFalse && arguments["disable"].IsFalse)
                    Console.WriteLine(
                        String.Format("Load Beta is {0}.",
                        pyRevit.GetLoadBetaTools() ? "Enabled" : "Disabled")
                        );
                else
                    pyRevit.SetLoadBetaTools(arguments["enable"].IsTrue);
            }


            // =======================================================================================================
            // $ pyrevit usagelogging
            // =======================================================================================================
            else if (arguments["usagelogging"].IsTrue
                    && arguments["enable"].IsFalse
                    && arguments["disable"].IsFalse) {
                try {
                    Console.WriteLine(
                        String.Format("Usage logging is {0}.",
                        pyRevit.GetUsageReporting() ? "Enabled" : "Disabled")
                        );
                    Console.WriteLine(String.Format("Log File Path: {0}", pyRevit.GetUsageLogFilePath()));
                    Console.WriteLine(String.Format("Log Server Url: {0}", pyRevit.GetUsageLogServerUrl()));
                }
                catch (Exception ex) {
                    LogException(ex, logLevel);
                }
            }


            // =======================================================================================================
            // $ pyrevit usagelogging enable (file | server) <dest_path>
            // =======================================================================================================
            else if (arguments["usagelogging"].IsTrue && arguments["enable"].IsTrue) {
                if (arguments["file"].IsTrue)
                    pyRevit.EnableUsageReporting(logFilePath: TryGetValue(arguments, "<dest_path>"));
                else
                    pyRevit.EnableUsageReporting(logServerUrl: TryGetValue(arguments, "<dest_path>"));
            }


            // =======================================================================================================
            // $ pyrevit usagelogging disable
            // =======================================================================================================
            else if (arguments["usagelogging"].IsTrue && arguments["disable"].IsTrue) {
                pyRevit.DisableUsageReporting();
            }


            // =======================================================================================================
            // $ pyrevit outputcss [<css_path>]
            // =======================================================================================================
            else if (arguments["outputcss"].IsTrue) {
                if (arguments["<css_path>"] == null)
                    Console.WriteLine(
                        String.Format("Output Style Sheet is set to: {0}",
                        pyRevit.GetOutputStyleSheet()
                        ));
                else
                    pyRevit.SetOutputStyleSheet(TryGetValue(arguments, "<css_path>"));
            }

            // =======================================================================================================
            // $ pyrevit config seed
            // =======================================================================================================
            else if (arguments["config"].IsTrue && arguments["seed"].IsTrue) {
                pyRevit.SeedConfig();
            }

            // =======================================================================================================
            // $ pyrevit config <option_path> (enable | disable)
            // $ pyrevit config <option_path> [<option_value>]
            // =======================================================================================================
            else if (arguments["config"].IsTrue && arguments["<option_path>"] != null) {
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
                            pyRevit.GetConfig(configSection, configOption)
                            ));
                    // if enable | disable
                    else if (arguments["enable"].IsTrue)
                        pyRevit.SetConfig(configSection, configOption, true);
                    else if (arguments["disable"].IsTrue)
                        pyRevit.SetConfig(configSection, configOption, false);
                    // if custom value 
                    else if (arguments["<option_value>"] != null)
                        pyRevit.SetConfig(
                            configSection,
                            configOption,
                            TryGetValue(arguments, "<option_value>")
                            );
                }
            }

            // now process any error codes
            ProcessErrorCodes();
        }

        // get enabled keywords
        private static List<string> ExtractEnabledKeywords(IDictionary<string, ValueObject> arguments) {
            // grab active keywords
            var enabledKeywords = new List<string>();
            foreach (var argument in arguments.OrderBy(x => x.Key)) {
                if (argument.Value != null
                        && !argument.Key.Contains("<")
                        && !argument.Key.Contains(">")
                        && argument.Value.IsTrue) {
                    logger.Debug(string.Format("Active Keyword: {0}", argument.Key));
                    enabledKeywords.Add(argument.Key);
                }
            }
            return enabledKeywords;
        }

        // verify cli command based on keywords that must be true and the rest of keywords must be false
        private static bool VerifyCommand(
                IEnumerable<string> enabledKeywords, params string[] keywords) {
            // check all given keywords are active
            if (keywords.Length != enabledKeywords.Count())
                return false;

            foreach (var keyword in keywords)
                if (!enabledKeywords.Contains(keyword))
                    return false;

            return true;
        }

        // safely try to get a value from arguments dictionary, return null on errors
        private static string TryGetValue(
                IDictionary<string, ValueObject> arguments, string key, string defaultValue = null) {
            return arguments[key] != null ? arguments[key].Value as string : defaultValue;
        }

        // process generated error codes and show prompts if necessary
        private static void ProcessErrorCodes() {
        }

        // process generated error codes and show prompts if necessary
        private static void LogException(Exception ex, pyRevitManagerLogLevel logLevel) {
            if (logLevel == pyRevitManagerLogLevel.Debug)
                logger.Error(string.Format("{0} ({1})\n{2}", ex.Message, ex.GetType().ToString(), ex.StackTrace));
            else
                logger.Error(ex.Message);
        }
    }

}
