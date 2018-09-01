using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Text.RegularExpressions;

using pyRevitLabs.Common.Extensions;

namespace pyRevitLabs.TargetApps.Revit {
    public class RevitProcess {
        private Process _process;

        public RevitProcess(Process runningRevitProcess) {
            _process = runningRevitProcess;
        }

        public static bool IsRevitProcess(Process runningProcess) {
            if (runningProcess.ProcessName.ToLower() == "revit")
                return true;
            return false;
        }

        public string RevitModule {
            get {
                return _process.MainModule.FileName;
            }
        }

        public Version RevitVesion {
            get {
                var fileInfo = FileVersionInfo.GetVersionInfo(RevitModule);
                int revitVersion = 2000 + int.Parse(fileInfo.FileVersion.Substring(0, 2));
                return new Version(revitVersion, 0);
            }
        }

        public string RevitLocation {
            get {
                return Path.GetDirectoryName(_process.MainModule.FileName);
            }
        }

        public override string ToString() {
            return String.Format("Id: {0} Version: {1} Path: {2}",
                                 _process.Id, RevitVesion, RevitModule);
        }

        public void Kill() {
            _process.Kill();
        }
    }


    public class RevitInstall {
        public Version DisplayVersion;
        public string InstallLocation;
        public int LanguageCode;

        public RevitInstall(string version, string installLoc, int langCode) {
            DisplayVersion = version.ConvertToVersion();
            InstallLocation = installLoc;
            LanguageCode = langCode;
        }

        public override string ToString() {
            return String.Format("Version: {0}:{2} Path: {1}", Version, InstallLocation, LanguageCode);
        }

        public Version Version {
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


    public class RevitConnector {
        public static List<RevitProcess> ListRunningRevits() {
            var runningRevits = new List<RevitProcess>();
            foreach (Process ps in Process.GetProcesses()) {
                if (RevitProcess.IsRevitProcess(ps))
                    runningRevits.Add(new RevitProcess(ps));
            }
            return runningRevits;
        }

        public static List<RevitInstall> ListInstalledRevits() {
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
                        (int) subkey.GetValue("Language")
                        ));
            }
            return installedRevits;
        }

        public static void KillAllRunningRevits() {
            foreach (RevitProcess revit in ListRunningRevits())
                revit.Kill();
        }

        public static void EnableRemoteDLLLoading(RevitProcess targetRevit = null) {

        }

        public static void DisableRemoteDLLLoading(RevitProcess targetRevit = null) {

        }
    }
}
