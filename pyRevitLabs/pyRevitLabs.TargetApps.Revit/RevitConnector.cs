using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pyRevitLabs.TargetApps.Revit {
    public class RevitApp {
        private Process _process;

        public RevitApp(Process runningRevitProcess) {
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


    public class RevitConnector {
        public static List<RevitApp> ListRunningRevits() {
            var runningRevits = new List<RevitApp>();
            foreach (Process ps in Process.GetProcesses()) {
                if (RevitApp.IsRevitProcess(ps))
                    runningRevits.Add(new RevitApp(ps));
            }
            return runningRevits;
        }

        public static List<RevitApp> ListInstalledRevits() {
            var installedRevits = new List<RevitApp>();
            return installedRevits;
        }

        public static void KillAllRunningRevits() {
            foreach (RevitApp revit in ListRunningRevits())
                revit.Kill();
        }

        public static void EnableRemoteDLLLoading(RevitApp targetRevit = null) {

        }

        public static void DisableRemoteDLLLoading(RevitApp targetRevit = null) {

        }
    }
}
