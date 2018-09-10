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

        private const string helpUrl = "https://github.com/eirannejad/pyRevitLabs/blob/master/README_CLI.md";
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
        pyrevit attach (--all | <revit_year>) [<repo_path>] [--allusers]
        pyrevit detach (--all | <revit_year>)
        pyrevit setengine latest (--all | --attached | <revit_year>) [<repo_path>]
        pyrevit setengine dynamosafe (--all | --attached | <revit_year>) [<repo_path>]
        pyrevit setengine <engine_version> (--all | --attached | <revit_year>) [<repo_path>]
        pyrevit extensions list
        pyrevit extensions search [<search_pattern>]
        pyrevit extensions info <extension_name>
        pyrevit extensions help <extension_name>
        pyrevit extensions open <extension_name>
        pyrevit extensions install <extension_name> <dest_path> [--branch=<branch_name>]
        pyrevit extensions install (ui | lib) <extension_name> <repo_url> <dest_path> [--branch=<branch_name>]
        pyrevit extensions uninstall <extension_name>
        pyrevit extensions update (--all | <extension_name>)
        pyrevit extensions paths
        pyrevit extensions paths (add | remove) <extensions_path>
        pyrevit extensions (enable | disable) <extension_name>
        pyrevit open
        pyrevit info
        pyrevit revit list [--installed]
        pyrevit revit killall [<revit_year>]
        pyrevit revit fileinfo <file_path>
        pyrevit revit listfiles <src_path> [--csv=<output_file>]
        pyrevit clearcache (--all | <revit_year>)
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
        --verbose                   Print info messages.
        --debug                     Print docopt options and logger debug messages.
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
            foreach (var rule in LogManager.Configuration.LoggingRules) {
                rule.DisableLoggingForLevel(LogLevel.Info);
                rule.DisableLoggingForLevel(LogLevel.Debug);
            }

            // process arguments for logging level
            var argsList = new List<string>(args);

            if (argsList.Contains("--verbose")) {
                argsList.Remove("--verbose");
                logLevel = pyRevitManagerLogLevel.InfoMessages;
                foreach (var rule in LogManager.Configuration.LoggingRules)
                    rule.EnableLoggingForLevel(LogLevel.Info);
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
                version: string.Format(StringLib.ConsoleVersionFormat,
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

            // get logger
            logger = LogManager.GetCurrentClassLogger();
            // get active keys for safe command extraction
            var activeKeys = ExtractEnabledKeywords(arguments);

            // now call methods based on inputs
            try {
                ExecuteCommand(arguments, activeKeys);
            }
            catch (Exception ex) {
                LogException(ex, logLevel);
            }

            ProcessErrorCodes();
        }


        private static void ExecuteCommand(IDictionary<string, ValueObject> arguments,
                                           IEnumerable<string> activeKeys) {
            // =======================================================================================================
            // $ pyrevit blog
            // $ pyrevit docs
            // $ pyrevit source
            // $ pyrevit youtubes
            // $ pyrevit support
            // =======================================================================================================
            if (VerifyCommand(activeKeys, "blog"))
                CommonUtils.OpenUrl(pyRevitConsts.pyRevitBlogsUrl);

            else if (VerifyCommand(activeKeys, "docs"))
                CommonUtils.OpenUrl(pyRevitConsts.pyRevitDocsUrl);

            else if (VerifyCommand(activeKeys, "source"))
                CommonUtils.OpenUrl(pyRevitConsts.pyRevitSourceRepoUrl);

            else if (VerifyCommand(activeKeys, "youtube"))
                CommonUtils.OpenUrl(pyRevitConsts.pyRevitYoutubeUrl);

            else if (VerifyCommand(activeKeys, "support"))
                CommonUtils.OpenUrl(pyRevitConsts.pyRevitSupportRepoUrl);

            // =======================================================================================================
            // $ pyrevit help
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "help"))
                CommonUtils.OpenUrl(
                    helpUrl,
                    errMsg: "Can not open online help page. No internet connection detected. " +
                            "Try `pyrevit --help` instead."
                    );

            // =======================================================================================================
            // $ pyrevit install [--core] [--branch=<branch_name>] [<dest_path>]
            // $ pyrevit install <repo_url> <dest_path> [--core] [--branch=<branch_name>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "install"))
                pyRevit.Install(
                    coreOnly: arguments["--core"].IsTrue,
                    branchName: TryGetValue(arguments, "--branch"),
                    repoPath: TryGetValue(arguments, "<repo_url>"),
                    destPath: TryGetValue(arguments, "<dest_path>")
                    );

            // =======================================================================================================
            // $ pyrevit register <repo_path>
            // $ pyrevit unregister <repo_path>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "register")) {
                string repoPath = TryGetValue(arguments, "<repo_path>");
                if (repoPath != null)
                    pyRevit.RegisterClone(repoPath);
            }

            else if (VerifyCommand(activeKeys, "unregister")) {
                string repoPath = TryGetValue(arguments, "<repo_path>");
                if (repoPath != null)
                    pyRevit.UnregisterClone(repoPath);
            }

            // =======================================================================================================
            // $ pyrevit uninstall [(--all | <repo_path>)] [--clearconfigs]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "uninstall")) {
                if (arguments["--all"].IsTrue)
                    pyRevit.UninstallAllClones(clearConfigs: arguments["--clearconfigs"].IsTrue);
                else
                    pyRevit.Uninstall(
                        repoPath: TryGetValue(arguments, "<repo_path>"),
                        clearConfigs: arguments["--clearconfigs"].IsTrue
                        );
            }

            // =======================================================================================================
            // $ pyrevit setprimary <repo_path>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "setprimary"))
                pyRevit.SetPrimaryClone(TryGetValue(arguments, "<repo_path>"));

            // =======================================================================================================
            // $ pyrevit checkout <branch_name> [<repo_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "checkout"))
                pyRevit.Checkout(
                    TryGetValue(arguments, "<branch_name>"),
                    TryGetValue(arguments, "<repo_path>")
                    );

            // =======================================================================================================
            // $ pyrevit setcommit <commit_hash> [<repo_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "setcommit"))
                pyRevit.SetCommit(
                    TryGetValue(arguments, "<commit_hash>"),
                    TryGetValue(arguments, "<repo_path>")
                    );

            // =======================================================================================================
            // $ pyrevit setversion <tag_name> [<repo_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "setversion"))
                pyRevit.SetVersion(
                    TryGetValue(arguments, "<tag_name>"),
                    TryGetValue(arguments, "<repo_path>")
                    );

            // =======================================================================================================
            // $ pyrevit update [--all] [<repo_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "update")) {
                if (arguments["--all"].IsTrue)
                    pyRevit.UpdateAllClones();
                else
                    pyRevit.Update(repoPath: TryGetValue(arguments, "<repo_url>"));
            }

            // =======================================================================================================
            // $ pyrevit attach (--all | <revit_year>) [<repo_path>] [--allusers]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "attach")) {
                string revitYear = TryGetValue(arguments, "<revit_year>");
                string repoPath = TryGetValue(arguments, "<repo_path>");

                if (revitYear != null)
                    pyRevit.Attach(int.Parse(revitYear), repoPath: repoPath, allUsers: arguments["--allusers"].IsTrue);
                else if (arguments["--all"].IsTrue)
                    pyRevit.AttachAll(repoPath: repoPath, allUsers: arguments["--allusers"].IsTrue);
            }

            // =======================================================================================================
            // $ pyrevit detach (--all | <revit_year>)
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "detach")) {
                string revitYear = TryGetValue(arguments, "<revit_year>");

                if (revitYear != null)
                    pyRevit.Detach(int.Parse(revitYear));
                else if (arguments["--all"].IsTrue)
                    pyRevit.DetachAll();
            }

            // =======================================================================================================
            // $ pyrevit setengine latest (--all | --attached | <revit_year>) [<repo_path>]
            // $ pyrevit setengine dynamosafe (--all | --attached | <revit_year>) [<repo_path>]
            // $ pyrevit setengine <engine_version> (--all | --attached | <revit_year>) [<repo_path>]
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
                    string revitYear = TryGetValue(arguments, "<revit_year>");
                    string repoPath = TryGetValue(arguments, "<repo_path>");

                    if (revitYear != null)
                        pyRevit.Attach(
                            int.Parse(revitYear),
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
            }

            // =======================================================================================================
            // $ pyrevit extensions list
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "list"))
                foreach (pyRevitExtension ext in pyRevit.GetInstalledExtensions())
                    Console.WriteLine(string.Format("{0}{1}", ext.Name.PadRight(24), ext.InstallPath));

            // =======================================================================================================
            // $ pyrevit extensions search [<search_pattern>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "search")) {
                string searchPattern = TryGetValue(arguments, "<search_pattern>");
                var extList = pyRevit.LookupRegisteredExtensions(searchPattern);
                Console.WriteLine("==> UI Extensions");
                foreach (pyRevitExtension ext in extList)
                    Console.WriteLine(String.Format("{0}{1}", ext.Name.PadRight(24), ext.Url));
            }

            // =======================================================================================================
            // $ pyrevit extensions info <extension_name>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "info")) {
                string extName = TryGetValue(arguments, "<extension_name>");
                if (extName != null) {
                    var ext = pyRevit.FindExtension(extName);
                    if (ext != null)
                        Console.WriteLine(ext.ToString());
                    else if (Errors.LatestError == ErrorCodes.MoreThanOneItemMatched)
                        logger.Warn(string.Format("More than one extension matches the search pattern \"{0}\"",
                                                    extName));
                }
            }

            // =======================================================================================================
            // $ pyrevit extensions help <extension_name>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "help")) {
                string extName = TryGetValue(arguments, "<extension_name>");
                if (extName != null) {
                    var ext = pyRevit.FindExtension(extName);
                    if (ext != null)
                        Process.Start(ext.Website);
                    else if (Errors.LatestError == ErrorCodes.MoreThanOneItemMatched)
                        logger.Warn(string.Format("More than one extension matches the search pattern \"{0}\"",
                                                    extName));
                }
            }

            // =======================================================================================================
            // $ pyrevit extensions open <extension_name>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "open")) {
                string extName = TryGetValue(arguments, "<extension_name>");
                if (extName != null) {
                    var ext = pyRevit.LookupInstalledExtension(extName);
                    if (ext != null)
                        Process.Start("explorer.exe", ext.InstallPath);
                    else if (Errors.LatestError == ErrorCodes.MoreThanOneItemMatched)
                        logger.Warn(string.Format("More than one extension matches the search pattern \"{0}\"",
                                                    extName));
                }
            }

            // =======================================================================================================
            // $ pyrevit extensions install <extension_name> <dest_path> [--branch=<branch_name>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "install")) {
                string destPath = TryGetValue(arguments, "<dest_path>");
                string extName = TryGetValue(arguments, "<extension_name>");
                string branchName = TryGetValue(arguments, "--branch");

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

            // =======================================================================================================
            // $ pyrevit extensions install (ui | lib) <extension_name> <repo_url> <dest_path> [--branch=<branch_name>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "install", "ui")) {
                string destPath = TryGetValue(arguments, "<dest_path>");
                string extName = TryGetValue(arguments, "<extension_name>");
                string repoUrl = TryGetValue(arguments, "<repo_url>");
                string branchName = TryGetValue(arguments, "--branch");

                if (repoUrl.IsValidUrl())
                    pyRevit.InstallExtension(extName, pyRevitExtensionTypes.UIExtension,
                                                repoUrl, destPath, branchName);
                else
                    logger.Error(string.Format("Repo url is not valid \"{0}\"", repoUrl));
            }

            else if (VerifyCommand(activeKeys, "extensions", "install", "lib")) {
                string destPath = TryGetValue(arguments, "<dest_path>");
                string repoUrl = TryGetValue(arguments, "<repo_url>");
                string extName = TryGetValue(arguments, "<extension_name>");
                string branchName = TryGetValue(arguments, "--branch");

                if (repoUrl.IsValidUrl())
                    pyRevit.InstallExtension(extName, pyRevitExtensionTypes.LibraryExtension,
                                                repoUrl, destPath, branchName);
                else
                    logger.Error(string.Format("Repo url is not valid \"{0}\"", repoUrl));
            }

            // =======================================================================================================
            // $ pyrevit extensions uninstall <extension_name>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "uninstall")) {
                string extName = TryGetValue(arguments, "<extension_name>");
                pyRevit.UninstallExtension(extName);
            }

            // =======================================================================================================
            // $ pyrevit extensions update (--all | <extension_name>)
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "update")) {
                string extName = TryGetValue(arguments, "<extension_name>");
                if (arguments["--all"].IsTrue)
                    pyRevit.UpdateAllInstalledExtensions();
                else if (extName != null)
                    pyRevit.UpdateInstalledExtension(extName);
            }

            // =======================================================================================================
            // $ pyrevit extensions paths
            // $ pyrevit extensions paths (add | remove) <extensions_path>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "extensions", "paths")) {
                Console.WriteLine("==> Extension Search Paths:");
                foreach (var searchPath in pyRevit.GetExtensionSearchPaths())
                    Console.WriteLine(searchPath);
            }

            else if (VerifyCommand(activeKeys, "extensions", "paths", "add")) {
                var searchPath = TryGetValue(arguments, "<extensions_path>");
                if (searchPath != null)
                    pyRevit.AddExtensionSearchPath(searchPath);
            }

            else if (VerifyCommand(activeKeys, "extensions", "paths", "remove")) {
                var searchPath = TryGetValue(arguments, "<extensions_path>");
                if (searchPath != null) {
                    pyRevit.RemoveExtensionSearchPath(searchPath);
                }
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
            }

            else if (VerifyCommand(activeKeys, "extensions", "disable")) {
                if (arguments["<extension_name>"] != null) {
                    string extensionName = TryGetValue(arguments, "<extension_name>");
                    if (extensionName != null)
                        pyRevit.DisableExtension(extensionName);
                }
            }

            // =======================================================================================================
            // $ pyrevit open
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "open")) {
                try {
                    string primaryRepo = pyRevit.GetPrimaryClone();
                    Process.Start("explorer.exe", primaryRepo);
                }
                catch (pyRevitConfigValueNotSet) {
                    logger.Error("Primary repo is not set. Run with \"--debug\" for details.");
                }
            }

            // =======================================================================================================
            // $ pyrevit info
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "info")) {
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

            // =======================================================================================================
            // $ pyrevit revit list [--installed]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "revit", "list")) {
                if (arguments["--installed"].IsTrue)
                    foreach (var revit in RevitController.ListInstalledRevits())
                        Console.WriteLine(revit);
                else
                    foreach (var revit in RevitController.ListRunningRevits())
                        Console.WriteLine(revit);
            }

            // =======================================================================================================
            // $ pyrevit revit killall [<revit_year>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "revit", "killall")) {
                var revitYear = TryGetValue(arguments, "<revit_year>");
                if (revitYear != null)
                    RevitController.KillRunningRevits(int.Parse(revitYear));
                else
                    RevitController.KillAllRunningRevits();
            }


            // =======================================================================================================
            // $ pyrevit revit fileinfo <file_path>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "revit", "fileinfo")) {
                var targetPath = TryGetValue(arguments, "<file_path>");
                if (targetPath != null)
                    PrintModelInfo(new RevitModelFile(targetPath));
            }

            // =======================================================================================================
            // $ pyrevit revit listfiles <src_path> [--csv=<output_file>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "revit", "listfiles")) {
                var targetPath = TryGetValue(arguments, "<src_path>");
                var outputCSV = TryGetValue(arguments, "--csv");

                // collect all revit models
                var models = new List<RevitModelFile>();
                var errorList = new List<(string, string)>();
                if (targetPath != null) {
                    logger.Info(string.Format("Searching for revit files under \"{0}\"", targetPath));
                    FileAttributes attr = File.GetAttributes(targetPath);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
                        var files = Directory.EnumerateFiles(targetPath, "*.rvt", SearchOption.AllDirectories);
                        logger.Info(string.Format(" {0} revit files found under \"{1}\"", files.Count(), targetPath));
                        foreach (var file in files) {
                            try {
                                logger.Info(string.Format("Revit file found \"{0}\"", file));
                                var model = new RevitModelFile(file);
                                models.Add(model);
                            }
                            catch (Exception ex) {
                                errorList.Add((file, ex.Message));
                            }
                        }
                    }

                    // now print or output the results
                    if (outputCSV != null) {
                        logger.Info(string.Format("Building CSV data to \"{0}\"", outputCSV));
                        var csv = new StringBuilder();
                        csv.Append("filepath,productname,buildnumber,isworkshared,centralmodelpath,lastsavedpath,uniqueid,error\n");
                        foreach (var model in models) {
                            var data = new List<string>() { string.Format("\"{0}\"", model.FilePath),
                                                            string.Format("\"{0}\"", model.RevitProduct != null ? model.RevitProduct.ProductName : ""),
                                                            string.Format("\"{0}\"", model.RevitProduct != null ? model.RevitProduct.BuildNumber : ""),
                                                            string.Format("\"{0}\"", model.IsWorkshared ? "True" : "False"),
                                                            string.Format("\"{0}\"", model.CentralModelPath),
                                                            string.Format("\"{0}\"", model.LastSavedPath),
                                                            string.Format("\"{0}\"", model.UniqueId.ToString()),
                                                            ""
                                                           };
                            csv.Append(string.Join(",", data) + "\n");
                        }

                        // write list of files with errors
                        logger.Debug(string.Format("Adding errors to \"{0}\"", outputCSV));
                        foreach (var errinfo in errorList)
                            csv.Append(string.Format("\"{0}\",,,,,,,\"{1}\"\n", errinfo.Item1, errinfo.Item2));

                        logger.Info(string.Format("Writing results to \"{0}\"", outputCSV));
                        File.WriteAllText(outputCSV, csv.ToString());
                    }
                    else {
                        // report info on all files
                        foreach (var model in models) {
                            Console.WriteLine(model.FilePath);
                            PrintModelInfo(new RevitModelFile(targetPath));
                            Console.WriteLine();
                        }

                        // write list of files with errors
                        Console.WriteLine("An error occured while processing these files:");
                        foreach (var errinfo in errorList)
                            Console.WriteLine(string.Format("\"{0}\": {1}\n", errinfo.Item1, errinfo.Item2));
                    }
                }
            }

            // =======================================================================================================
            // $ pyrevit clearcache (--all | <revit_year>)
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "clearcache")) {
                if (arguments["--all"].IsTrue)
                    pyRevit.ClearAllCaches();
                else if (arguments["<revit_year>"] != null) {
                    var revitYear = TryGetValue(arguments, "<revit_year>");
                    if (revitYear != null)
                        pyRevit.ClearCache(int.Parse(revitYear));
                }
            }

            // =======================================================================================================
            // $ pyrevit allowremotedll [(enable | disable)]
            // =======================================================================================================
            // TODO: Implement allowremotedll
            else if (VerifyCommand(activeKeys, "allowremotedll"))
                logger.Error("Not Yet Implemented.");

            // =======================================================================================================
            // $ pyrevit checkupdate [(enable | disable)]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "checkupdate"))
                Console.WriteLine(
                    String.Format("Check Updates is {0}.",
                    pyRevit.GetCheckUpdates() ? "Enabled" : "Disabled")
                    );

            else if (VerifyCommand(activeKeys, "checkupdate", "enable"))
                pyRevit.SetCheckUpdates(true);

            else if (VerifyCommand(activeKeys, "checkupdate", "disable"))
                pyRevit.SetCheckUpdates(false);

            // =======================================================================================================
            // $ pyrevit autoupdate [(enable | disable)]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "autoupdate"))
                Console.WriteLine(
                    String.Format("Auto Update is {0}.",
                    pyRevit.GetAutoUpdate() ? "Enabled" : "Disabled")
                    );

            else if (VerifyCommand(activeKeys, "autoupdate", "enable"))
                pyRevit.SetAutoUpdate(true);

            else if (VerifyCommand(activeKeys, "autoupdate", "disable"))
                pyRevit.SetAutoUpdate(false);

            // =======================================================================================================
            // $ pyrevit rocketmode [(enable | disable)]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "rocketmode"))
                Console.WriteLine(
                    String.Format("Rocket Mode is {0}.",
                    pyRevit.GetRocketMode() ? "Enabled" : "Disabled")
                    );

            else if (VerifyCommand(activeKeys, "rocketmode", "enable"))
                pyRevit.SetRocketMode(true);

            else if (VerifyCommand(activeKeys, "rocketmode", "disable"))
                pyRevit.SetRocketMode(false);

            // =======================================================================================================
            // $ pyrevit logs [(none | verbose | debug)]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "logs"))
                Console.WriteLine(String.Format("Logging Level is {0}.", pyRevit.GetLoggingLevel().ToString()));

            else if (VerifyCommand(activeKeys, "logs", "none"))
                pyRevit.SetLoggingLevel(PyRevitLogLevels.None);

            else if (VerifyCommand(activeKeys, "logs", "verbose"))
                pyRevit.SetLoggingLevel(PyRevitLogLevels.Verbose);

            else if (VerifyCommand(activeKeys, "logs", "debug"))
                pyRevit.SetLoggingLevel(PyRevitLogLevels.Debug);

            // =======================================================================================================
            // $ pyrevit filelogging [(enable | disable)]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "filelogging"))
                Console.WriteLine(
                    String.Format("File Logging is {0}.",
                    pyRevit.GetFileLogging() ? "Enabled" : "Disabled")
                    );

            else if (VerifyCommand(activeKeys, "filelogging", "enable"))
                pyRevit.SetFileLogging(true);

            else if (VerifyCommand(activeKeys, "filelogging", "disable"))
                pyRevit.SetFileLogging(false);

            // =======================================================================================================
            // $ pyrevit loadbeta [(enable | disable)]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "loadbeta"))
                Console.WriteLine(
                    String.Format("Load Beta is {0}.",
                    pyRevit.GetLoadBetaTools() ? "Enabled" : "Disabled")
                    );

            else if (VerifyCommand(activeKeys, "loadbeta", "enable"))
                pyRevit.SetLoadBetaTools(true);

            else if (VerifyCommand(activeKeys, "loadbeta", "disable"))
                pyRevit.SetLoadBetaTools(false);

            // =======================================================================================================
            // $ pyrevit usagelogging
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "usagelogging")) {
                Console.WriteLine(
                    String.Format("Usage logging is {0}.",
                    pyRevit.GetUsageReporting() ? "Enabled" : "Disabled")
                    );
                Console.WriteLine(String.Format("Log File Path: {0}", pyRevit.GetUsageLogFilePath()));
                Console.WriteLine(String.Format("Log Server Url: {0}", pyRevit.GetUsageLogServerUrl()));
            }

            // =======================================================================================================
            // $ pyrevit usagelogging enable (file | server) <dest_path>
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "usagelogging", "enable", "file"))
                pyRevit.EnableUsageReporting(logFilePath: TryGetValue(arguments, "<dest_path>"));

            else if (VerifyCommand(activeKeys, "usagelogging", "enable", "server"))
                pyRevit.EnableUsageReporting(logServerUrl: TryGetValue(arguments, "<dest_path>"));

            // =======================================================================================================
            // $ pyrevit usagelogging disable
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "usagelogging", "disable"))
                pyRevit.DisableUsageReporting();

            // =======================================================================================================
            // $ pyrevit outputcss [<css_path>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "outputcss")) {
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
            else if (VerifyCommand(activeKeys, "config", "seed"))
                pyRevit.SeedConfig();

            // =======================================================================================================
            // $ pyrevit config <option_path> (enable | disable)
            // $ pyrevit config <option_path> [<option_value>]
            // =======================================================================================================
            else if (VerifyCommand(activeKeys, "config")) {
                if (arguments["<option_path>"] != null) {
                    // extract section and option names
                    string orignalOptionValue = TryGetValue(arguments, "<option_path>");
                    if (orignalOptionValue.Split(':').Count() == 2) {
                        string configSection = orignalOptionValue.Split(':')[0];
                        string configOption = orignalOptionValue.Split(':')[1];

                        // if no value provided, read the value
                        if (arguments["<option_value>"] != null)
                            pyRevit.SetConfig(
                                configSection,
                                configOption,
                                TryGetValue(arguments, "<option_value>")
                                );
                        else if (arguments["<option_value>"] == null)
                            Console.WriteLine(
                                String.Format("{0} = {1}",
                                configOption,
                                pyRevit.GetConfig(configSection, configOption)
                                ));
                    }
                }
            }

            else if (VerifyCommand(activeKeys, "config", "enable")) {
                if (arguments["<option_path>"] != null) {
                    // extract section and option names
                    string orignalOptionValue = TryGetValue(arguments, "<option_path>");
                    if (orignalOptionValue.Split(':').Count() == 2) {
                        string configSection = orignalOptionValue.Split(':')[0];
                        string configOption = orignalOptionValue.Split(':')[1];

                        pyRevit.SetConfig(configSection, configOption, true);
                    }
                }
            }

            else if (VerifyCommand(activeKeys, "config", "disable")) {
                if (arguments["<option_path>"] != null) {
                    // extract section and option names
                    string orignalOptionValue = TryGetValue(arguments, "<option_path>");
                    if (orignalOptionValue.Split(':').Count() == 2) {
                        string configSection = orignalOptionValue.Split(':')[0];
                        string configOption = orignalOptionValue.Split(':')[1];

                        pyRevit.SetConfig(configSection, configOption, false);
                    }
                }
            }
        }

        // get enabled keywords
        private static List<string> ExtractEnabledKeywords(IDictionary<string, ValueObject> arguments) {
            // grab active keywords
            var enabledKeywords = new List<string>();
            foreach (var argument in arguments.OrderBy(x => x.Key)) {
                if (argument.Value != null
                        && !argument.Key.Contains("<")
                        && !argument.Key.Contains(">")
                        && !argument.Key.Contains("--")
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
                logger.Error(string.Format("{0}\nRun with \"--debug\" option to see debug messages.", ex.Message));
        }

        // print info on a revit model
        private static void PrintModelInfo(RevitModelFile model) {
            Console.WriteLine(string.Format("Created in: {0} ({1}({2}))",
                                model.RevitProduct.ProductName,
                                model.RevitProduct.BuildNumber,
                                model.RevitProduct.BuildTarget));
            Console.WriteLine(string.Format("Workshared: {0}", model.IsWorkshared ? "Yes" : "No"));
            if (model.IsWorkshared)
                Console.WriteLine(string.Format("Central Model Path: {0}", model.CentralModelPath));
            Console.WriteLine(string.Format("Last Saved Path: {0}", model.LastSavedPath));
            Console.WriteLine(string.Format("Document Id: {0}", model.UniqueId));
            Console.WriteLine(string.Format("Open Workset Settings: {0}", model.OpenWorksetConfig));
            Console.WriteLine(string.Format("Document Increment: {0}", model.DocumentIncrement));

            if (model.IsFamily) {
                Console.WriteLine("Model is a Revit Family!");
                Console.WriteLine(string.Format("Category Name: {0}", model.CategoryName));
                Console.WriteLine(string.Format("Host Category Name: {0}", model.HostCategoryName));
            }
        }
    }
}
