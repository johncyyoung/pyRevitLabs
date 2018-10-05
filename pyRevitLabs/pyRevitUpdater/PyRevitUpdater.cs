﻿using System;
using System.Windows;

using pyRevitLabs.TargetApps.Revit;

namespace pyRevitUpdater {
    public class PyRevitUpdaterCLI {
        public static void ProcessArguments(string[] args) {
            if (args.Length >= 1) {
                var clonePath = args[0];
                if (args.Length == 2 && args[1] == "--gui") {
                    // show gui
                    var updaterWindow = new UpdaterWindow();
                    updaterWindow.ClonePath = clonePath;
                    updaterWindow.ShowDialog();
                }
                else {
                    RunUpdate(clonePath);
                }
            }
        }

        public static bool RevitsAreRunning() {
            return RevitController.ListRunningRevits().Count > 0;
        }

        public static void RunUpdate(string clonePath) {
            try {
                var clone = PyRevit.GetRegisteredClone(clonePath);
                PyRevit.Update(clone);
            }
            catch (Exception ex){
                MessageBox.Show(ex.Message, PyRevitConsts.AddinFileName);
            }
        }

    }
}
