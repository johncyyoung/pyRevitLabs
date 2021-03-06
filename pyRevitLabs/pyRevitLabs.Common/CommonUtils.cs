﻿using OpenMcdf;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using IWshRuntimeLibrary;

using NLog;

namespace pyRevitLabs.Common {
    public static class CommonUtils
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [DllImport("ole32.dll")] private static extern int StgIsStorageFile([MarshalAs(UnmanagedType.LPWStr)] string pwcsName);

        public static bool VerifyPath(string path) {
            return Directory.Exists(path);
        }

        // helper for deleting directories recursively
        // @handled @logs
        public static void DeleteDirectory(string targetDir, bool verbose = true)
        {
            if (CommonUtils.VerifyPath(targetDir)) {
                if (verbose)
                    logger.Debug("Recursive deleting directory \"{0}\"", targetDir);
                string[] files = Directory.GetFiles(targetDir);
                string[] dirs = Directory.GetDirectories(targetDir);

                try {
                    foreach (string file in files) {
                        System.IO.File.SetAttributes(file, FileAttributes.Normal);
                        System.IO.File.Delete(file);
                    }

                    foreach (string dir in dirs) {
                        DeleteDirectory(dir, verbose: false);
                    }

                    Directory.Delete(targetDir, false);
                }
                catch (Exception ex) {
                    throw new pyRevitException(string.Format("Error recursive deleting directory \"{0}\" | {1}",
                                                             targetDir, ex.Message));
                }
            }
        }

        // helper for copying a directory recursively
        // @handled @logs
        public static void CopyDirectory(string sourceDir, string destDir) {
            logger.Debug("Copying \"{0}\" to \"{1}\"", sourceDir, destDir);
            try {
                // create all of the directories
                foreach (string dirPath in Directory.GetDirectories(sourceDir, "*",
                    SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));

                // copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(sourceDir, "*.*",
                    SearchOption.AllDirectories))
                    System.IO.File.Copy(newPath, newPath.Replace(sourceDir, destDir), true);
            }
            catch (Exception ex) {
                throw new pyRevitException(
                    string.Format("Error copying \"{0}\" to \"{1}\" | {2}", sourceDir, destDir, ex.Message)
                    );
            }
        }

        public static void ConfirmPath(string path)
        {
            Directory.CreateDirectory(path);
        }

        public static void ConfirmFile(string filepath)
        {
            ConfirmPath(Path.GetDirectoryName(filepath));
            if (!System.IO.File.Exists(filepath)) {
                var file = System.IO.File.CreateText(filepath);
                file.Close();
            }
        }

        public static string DownloadFile(string url, string destPath)
        {
            if (CheckInternetConnection()) {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (var client = new WebClient()) {
                    client.DownloadFile(url, destPath);
                }
            }
            else
                throw new pyRevitNoInternetConnectionException();

            return destPath;
        }

        public static bool CheckInternetConnection()
        {
            try {
                using (var client = new WebClient())
                using (client.OpenRead("http://clients3.google.com/generate_204")) {
                    return true;
                }
            }
            catch {
                return false;
            }
        }

        public static byte[] GetStructuredStorageStream(string filePath, string streamName)
        {
            logger.Debug(string.Format("Attempting to read \"{0}\" stream from structured storage file at \"{1}\"",
                                       streamName, filePath));
            int res = StgIsStorageFile(filePath);

            if (res == 0) {
                CompoundFile cf = new CompoundFile(filePath);
                CFStream foundStream = cf.RootStorage.TryGetStream(streamName);
                if (foundStream != null) {
                    byte[] streamData = foundStream.GetData();
                    cf.Close();
                    return streamData;
                }
                return null;
            }
            else {
                throw new NotSupportedException("File is not a structured storage file");
            }
        }

        // open url
        public static void OpenUrl(string url, string errMsg = null) {
            if (CheckInternetConnection())
                Process.Start(url);
            else {
                if (errMsg == null)
                    errMsg = string.Format("Error opening url \"{0}\"", url);

                logger.Error(string.Format("{0}. No internet connection detected.", errMsg));
            }
        }

        public static void SetFileSecurity(string filePath, string userNameWithDoman) {
            //get file info
            FileInfo fi = new FileInfo(filePath);

            //get security access
            FileSecurity fs = fi.GetAccessControl();

            //remove any inherited access
            fs.SetAccessRuleProtection(true, false);

            //get any special user access
            AuthorizationRuleCollection rules = 
                fs.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            //remove any special access
            foreach (FileSystemAccessRule rule in rules)
                fs.RemoveAccessRule(rule);

            //add current user with full control.
            fs.AddAccessRule(
                new FileSystemAccessRule(userNameWithDoman, FileSystemRights.FullControl, AccessControlType.Allow)
                );

            //add all other users delete only permissions.
            //fs.AddAccessRule(
            //    new FileSystemAccessRule("Authenticated Users", FileSystemRights.Delete, AccessControlType.Allow)
            //    );

            //flush security access.
            System.IO.File.SetAccessControl(filePath, fs);
        }

        public static void OpenInExplorer(string resourcePath) {
            Process.Start("explorer.exe", resourcePath);
        }

        public static void AddShortcut(string shortCutName,
                                       string appName,
                                       string pathToExe,
                                       string args,
                                       string workingDir,
                                       string iconPath,
                                       string description,
                                       bool allUsers = false) {
            string commonStartMenuPath = Environment.GetFolderPath(
                allUsers ? Environment.SpecialFolder.CommonStartMenu : Environment.SpecialFolder.StartMenu
                );
            string appStartMenuPath = Path.Combine(commonStartMenuPath, "Programs", appName);

            ConfirmPath(appStartMenuPath);

            string shortcutLocation = Path.Combine(appStartMenuPath, shortCutName + ".lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutLocation);

            shortcut.Description = "Test App Description";
            //shortcut.IconLocation = @"C:\Program Files (x86)\TestApp\TestApp.ico"; //uncomment to set the icon of the shortcut
            shortcut.TargetPath = pathToExe;
            shortcut.Arguments = args;
            shortcut.Description = description;
            shortcut.IconLocation = iconPath;
            shortcut.WorkingDirectory = workingDir;
            shortcut.Save();
        }
    }
}

