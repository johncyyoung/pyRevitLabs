using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.Win32;

using pyRevitLabs.Common;
using pyRevitLabs.Common.Extensions;

using NLog;

namespace pyRevitLabs.TargetApps.Revit
{
    // EXCEPTIONS ====================================================================================================

    // DATA TYPES ====================================================================================================
    public class RevitModelFile
    {
        // keep this updated from:
        // https://knowledge.autodesk.com/support/revit-products/learn-explore/caas/sfdcarticles/sfdcarticles/How-to-tie-the-Build-number-with-the-Revit-update.html
        private static Dictionary<string, (string, string)> _revitBuildNumberLookupTable = new Dictionary<string, (string, string)>() {
            {"20110309_2315", ( "12.0.0", "2012 First Customer Ship" )},
            {"20110622_0930", ( "12.0.1", "2012 Update Release 1" )},
            {"20110916_2132", ( "12.0.2", "2012 Update Release 2" )},
            {"20120221_2030", ( "13.0.0", "2013 First Customer Ship" )},
            {"20120716_1115", ( "13.0.1", "2013 Update Release 1" )},
            {"20121003_2115", ( "13.0.2", "2013 Update Release 2" )},
            {"20130531_2115", ( "13.0.3", "2013 Update Release 3" )},
            {"20120821_1330", ( "13.0", "2013 LT First Customer Ship" )},
            {"20130531_0300", ( "13.1", "2013 LT Update Release 1" )},
            {"20130308_1515", ( "14.0.0", "2014 First Customer Ship" )},
            {"20130709_2115", ( "14.0.1", "2014 Update Release 1" )},
            {"20131024_2115", ( "14.0.2", "2014 Update Release 2" )},
            {"20140709_2115", ( "14.0.3", "2014 Update Release 3" )},
            {"20140223_1515", ( "15.0.0", "2015 First Customer Ship" )},
            {"20140322_1515", ( "15.0.1", "2015 Update Release 1" )},
            {"20140323_1530", ( "15.0.2", "2015 Update Release 2" )},
            {"20140606_1530", ( "15.0.3", "2015 Update Release 3" )},
            {"20140903_1530", ( "15.0.4", "2015 Update Release 4" )},
            {"20141119_1515", ( "15.0.5", "2015 Update Release 5" )},
            {"20140905_0730", ( "15.2.0", "2015 Release 2 (Subscription only release)" )},
            {"20141119_0715", ( "15.2.5", "2015 Release 2 Update Release 5 (Subscription only release)" )},
            {"20150127_1515", ( "15.0.6", "2015 Update Release 6" )},
            {"20150127_0715", ( "15.2.6", "2015 Release 2 Update Release 6 (Subscription only release)" )},
            {"20150303_1515", ( "15.0.7", "2015 Update Release 7" )},
            {"20150303_0715", ( "15.2.7", "2015 Release 2 Update Release 7 (Subscription only release)" )},
            {"20150512_1015", ( "15.0.8", "2015 Update Release 8" )},
            {"20150511_0715", ( "15.2.8", "2015 Release 2 Update Release 8 (Subscription only release)" )},
            {"20150702_1515", ( "15.0.9", "2015 Update Release 9" )},
            {"20150704_0715", ( "15.2.9", "2015 Release 2 Update Release 9 (Subscription only release)" )},
            {"20151007_1515", ( "15.0.10", "2015 Update Release 10" )},
            {"20151008_0715", ( "15.2.10", "2015 Release 2 Update Release 10 (Subscription only release)" )},
            {"20151207_1515", ( "15.0.11", "2015 Update Release 11 *Issue with Revit Server" )},
            {"20151208_0715", ( "15.2.11", "2015 Release 2 Update Release 11 (Subscription only release) *Issue with Revit Server" )},
            {"20160119_1515", ( "15.0.12", "2015 Update Release 12" )},
            {"20160120_0715", ( "15.2.12", "2015 Release 2 Update Release 12 (Subscription only release)" )},
            {"20160220_1515", ( "15.0.13", "2015 Update Release 13" )},
            {"20160220_0715", ( "15.2.13", "2015 Release 2 Update Release 13 (Subscription only release)" )},
            {"20160512_1515", ( "15.0.14", "2015 Update Release 14" )},
            {"20160512_0715", ( "15.2.14", "2015 Release 2 Update Release 14  (Subscription only release)" )},
            {"20150220_1215", ( "16.0.428.0", "2016 First Customer Ship" )},
            {"20150506_1715", ( "16.0.462.0", "2016 Service Pack 1" )},
            {"20150714_1515", ( "16.0.490.0", "2016 Service Pack 2" )},
            {"20151007_0715", ( "16.0.1063", "2016 Release 2 (R2)" )},
            {"20151209_0715", ( "16.0.1092.0", "2016 Update 1 for R2" )},
            {"20160126_1600", ( "16.0.1108.0", "2016 Update 2 for R2" )},
            {"20160217_1800", ( "16.0.1118.0", "2016 Update 3 for R2" )},
            {"20160314_0715", ( "16.0.1124.0", "2016 Update 4 for R2" )},
            {"20160525_1230", ( "16.0.1144.0", "2016 Update 5 for R2" )},
            {"20160720_0715", ( "16.0.1161.0", "2016 Update 6 for R2" )},
            {"20161004_0715", ( "16.0.1185.0", "2016 Update 7 for R2" )},
            {"20170117_1200", ( "16.0.1205.0", "2016 Update 8 for R2 (2016.1.8)" )},
            {"20160225_1515", ("17.0.416.0", "2017 First Customer Ship")},
            {"20160606_1515", ("17.0.476.0", "2017 Service Pack 1")},
            {"20160720_1515", ("17.0.501.0", "2017 Service Pack 2")},
            {"20161205_1400", ("17.0.503.0", "2017.0.3")},
            {"20161006_0315", ("17.0.1081.0", "2017.1")},
            {"20161117_1200", ("17.0.1099.0", "2017.1.1")},
            {"20170118_1100", ("17.0.1117.0", "2017.2")},
            {"20170419_0315", ("17.0.1128.0", "2017.2.1")},
            {"20170816_0615", ("17.0.1146.0", "2017.2.2")},
            {"20171027_0315", ("17.0.1150.0", "2017.2.3")},
            {"20170223_1515", ("18.0.0.420", "2018 First Customer Ship")},
            {"20170421_2315", ("18.0.1.2", "2018.0.1")},
            {"20170525_2315", ("18.0.2.11", "2018.0.2")},
            {"20170630_0700", ("18.1.0.92", "2018.1")},
            {"20170907_2315", ("18.1.1.18", "2018.1.1")},
            {"20170927_1515", ("18.2.0.51", "2018.2")},
            {"20180329_1100", ("18.3.0.81", "2018.3")},
            {"20180423_1000", ("18.3.1.2", "2018.3.1")},
            {"20180216_1515", ("19.0.0.405", "2019 First Customer Ship")},
            {"20180328_1800", ("19.0.1.1", "2019 Update for Trial Build")},
            {"20180518_1600", ("19.0.10.18", "2019.0.1")},
            {"20180806_1515", ("19.1.0.112", "2019.1")},
        };

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static Regex FileVersionFinder = new Regex(@".*(?<build>\d{8}_\d{4}).*");

        public RevitModelFile(string filePath)
        {
            FilePath = filePath;
            ProcessBasicFileInfo();
        }

        private void ProcessBasicFileInfo()
        {
            try {
                var rawData = CommonUtils.GetStructuredStorageStream(FilePath, "BasicFileInfo");
                var rawString = Encoding.Unicode.GetString(rawData);
                foreach (string line in rawString.Split(new string[] { "\0", "\r\n" }, StringSplitOptions.RemoveEmptyEntries)) {
                    logger.Debug(string.Format("Looking for build number in: \"{0}\"", line));
                    Match match = FileVersionFinder.Match(line);
                    if (match.Success) {
                        BuildNumber = match.Groups["build"].Value;
                        logger.Debug(BuildNumber);
                        if (_revitBuildNumberLookupTable.ContainsKey(BuildNumber)) {
                            FileVersion = new Version(_revitBuildNumberLookupTable[BuildNumber].Item1);
                            ProductName = string.Format(ProductName, _revitBuildNumberLookupTable[BuildNumber].Item2);
                            return;
                        }
                    }
                }
                ProductName = String.Format(ProductName, "???");
            }
            catch (Exception ex) {
                throw new pyRevitException("Target is not a valid Revit model.");
            }
        }

        public string FilePath { get; set; }

        public string BuildNumber { get; private set; }

        public string BuildTarget { get; private set; } = "x64";

        public string ProductName { get; private set; } = "Autodesk Revit {0}";

        public Version FileVersion { get; private set; } = new Version("0.0");
    }


    public class RevitProcess
    {
        private Process _process;

        public RevitProcess(Process runningRevitProcess)
        {
            _process = runningRevitProcess;
        }

        public static bool IsRevitProcess(Process runningProcess)
        {
            if (runningProcess.ProcessName.ToLower() == "revit")
                return true;
            return false;
        }

        public string RevitModule
        {
            get {
                return _process.MainModule.FileName;
            }
        }

        public Version RevitVesion
        {
            get {
                var fileInfo = FileVersionInfo.GetVersionInfo(RevitModule);
                return new Version("20" + fileInfo.FileVersion);
            }
        }

        public string RevitLocation
        {
            get {
                return Path.GetDirectoryName(_process.MainModule.FileName);
            }
        }

        public override string ToString()
        {
            return String.Format("PID: {0} Version: {1} Path: {2}",
                                 _process.Id, RevitVesion, RevitModule);
        }

        public void Kill()
        {
            _process.Kill();
        }
    }


    public class RevitInstall
    {
        public Version DisplayVersion;
        public string InstallLocation;
        public int LanguageCode;

        public RevitInstall(string version, string installLoc, int langCode)
        {
            DisplayVersion = version.ConvertToVersion();
            InstallLocation = installLoc;
            LanguageCode = langCode;
        }

        public override string ToString()
        {
            return String.Format("Version: {0}:{2} Path: {1}", Version, InstallLocation, LanguageCode);
        }

        public Version Version
        {
            get {
                return new Version(
                    2000 + DisplayVersion.Major,
                    DisplayVersion.Minor,
                    DisplayVersion.Build,
                    DisplayVersion.Revision
                    );
            }
        }
    }


    // MODEL =========================================================================================================
    public class RevitController
    {
        public static List<RevitProcess> ListRunningRevits()
        {
            var runningRevits = new List<RevitProcess>();
            foreach (Process ps in Process.GetProcesses()) {
                if (RevitProcess.IsRevitProcess(ps))
                    runningRevits.Add(new RevitProcess(ps));
            }
            return runningRevits;
        }

        public static List<RevitInstall> ListInstalledRevits()
        {
            var revitFinder = new Regex(@"^Revit \d\d\d\d");
            var installedRevits = new List<RevitInstall>();
            var uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (var key in uninstallKey.GetSubKeyNames()) {
                var subkey = uninstallKey.OpenSubKey(key);
                var appName = subkey.GetValue("DisplayName") as string;
                if (appName != null && revitFinder.IsMatch(appName))
                    installedRevits.Add(new RevitInstall(
                        subkey.GetValue("DisplayVersion") as string,
                        subkey.GetValue("InstallLocation") as string,
                        (int)subkey.GetValue("Language")
                        ));
            }
            return installedRevits;
        }

        public static void KillAllRunningRevits()
        {
            foreach (RevitProcess revit in ListRunningRevits())
                revit.Kill();
        }

        public static void EnableRemoteDLLLoading(RevitProcess targetRevit = null)
        {

        }

        public static void DisableRemoteDLLLoading(RevitProcess targetRevit = null)
        {

        }
    }
}