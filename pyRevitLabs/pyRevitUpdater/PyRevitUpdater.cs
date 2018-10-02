using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using pyRevitLabs.TargetApps.Revit;

namespace pyRevitUpdater {
    public class PyRevitUpdaterCLI {
        public static void ProcessArguments(string[] args) {
            if (args.Length == 1) {
                var clonePath = args[0];

                try {
                    var clone = PyRevit.GetRegisteredClone(clonePath);
                    PyRevit.Update(clone);
                    MessageBox.Show("pyRevitUpdater", "Update Completed", MessageBoxButton.OK);
                }
                catch {

                }
            }
        }
    }
}
