using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pyRevitLabs.Common;

namespace pyRevitLabs.TargetApps.Revit {
    public static class PyRevitConsts {
        // consts for the official pyRevit repo
        public static string pyRevitOriginalRepoPath = GlobalConfigs.UnderTest ?
            @"https://github.com/eirannejad/rsparam.git" :
            @"https://github.com/eirannejad/pyRevit.git";

        public const string pyRevitExtensionsDefinitionFileUri =
            @"https://github.com/eirannejad/pyRevit/raw/master/extensions/extensions.json";

        // urls
        public const string pyRevitBlogsUrl = @"https://eirannejad.github.io/pyRevit/";
        public const string pyRevitDocsUrl = @"https://pyrevit.readthedocs.io/en/latest/";
        public const string pyRevitSourceRepoUrl = @"https://github.com/eirannejad/pyRevit";
        public const string pyRevitYoutubeUrl = @"https://www.youtube.com/pyrevit";
        public const string pyRevitSupportRepoUrl = @"https://www.patreon.com/pyrevit";

        // repo info
        public const string pyRevitInstallName = "pyRevit";
        public const string pyRevitOriginalRepoMainBranch = "master";
        public const string pyRevitExtensionRepoMainBranch = "master";

        // consts for creating pyRevit addon manifest file
        public const string pyRevitAddinFileName = "pyRevit";
        public const string pyRevitAddinName = "PyRevitLoader";
        public const string pyRevitAddinId = "B39107C3-A1D7-47F4-A5A1-532DDF6EDB5D";
        public const string pyRevitAddinClassName = "PyRevitLoader.PyRevitLoaderApplication";
        public const string pyRevitVendorId = "eirannejad";
        public const string pyRevitDllName = "pyRevitLoader.dll";

        // consts for recording pyrevit.exe config in the pyRevit configuration file
        public const string pyRevitAppdataDirName = "pyRevit";
        public const string pyRevitAppdataLogsDirName = "Logs";
        public const string pyRevitConfigFileName = "pyRevit_config.ini";
        // core configs
        public const string pyRevitCoreConfigSection = "core";
        public const string pyRevitCheckUpdatesKey = "checkupdates";
        public const string pyRevitAutoUpdateKey = "autoupdate";
        public const string pyRevitVerboseKey = "verbose";
        public const string pyRevitDebugKey = "debug";
        public const string pyRevitFileLoggingKey = "filelogging";
        public const string pyRevitStartupLogTimeoutKey = "startuplogtimeout";
        public const string pyRevitUserExtensionsKey = "userextensions";
        public const string pyRevitCompileCSharpKey = "compilecsharp";
        public const string pyRevitCompileVBKey = "compilevb";
        public const string pyRevitLoadBetaKey = "loadbeta";
        public const string pyRevitRocketModeKey = "rocketmode";
        public const string pyRevitBinaryCacheKey = "bincache";
        public const string pyRevitMinDriveSpaceKey = "minhostdrivefreespace";
        public const string pyRevitRequiredHostBuildKey = "requiredhostbuild";
        public const string pyRevitOutputStyleSheet = "outputstylesheet";
        public const int pyRevitDynamoCompatibleEnginerVer = 273;
        // usage logging configs
        public const string pyRevitUsageLoggingSection = "usagelogging";
        public const string pyRevitUsageLoggingStatusKey = "active";
        public const string pyRevitUsageLogFilePathKey = "logfilepath";
        public const string pyRevitUsageLogServerUrlKey = "logserverurl";
        // pyrevit.exe specific configs
        public const string pyRevitManagerConfigSectionName = "environment";
        public const string pyRevitManagerInstalledClonesKey = "clones";
        // extensions
        public const string pyRevitExtensionDisabledKey = "disabled";
        public const string UIExtensionDirPostfix = ".extension";
        public const string LibraryExtensionDirPostfix = ".lib";

    }
}
