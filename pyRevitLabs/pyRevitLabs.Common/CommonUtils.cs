using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using NLog;
using OpenMcdf;

namespace pyRevitLabs.Common
{
    public static class CommonUtils
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [DllImport("ole32.dll")] private static extern int StgIsStorageFile([MarshalAs(UnmanagedType.LPWStr)] string pwcsName);

        // helper for deleting directories recursively
        // @handled @logs
        public static void DeleteDirectory(string targetDir)
        {
            if (Directory.Exists(targetDir)) {
                logger.Debug(string.Format("Recursive deleting directory \"{0}\"", targetDir));
                string[] files = Directory.GetFiles(targetDir);
                string[] dirs = Directory.GetDirectories(targetDir);

                try {
                    foreach (string file in files) {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }

                    foreach (string dir in dirs) {
                        DeleteDirectory(dir);
                    }

                    Directory.Delete(targetDir, false);
                }
                catch (Exception ex) {
                    throw new pyRevitException(string.Format("Error recursive deleting directory \"{0}\" | {1}",
                                                             targetDir, ex.Message));
                }
            }
        }

        public static void ConfirmPath(string path)
        {
            Directory.CreateDirectory(path);
        }

        public static void ConfirmFile(string filepath)
        {
            ConfirmPath(Path.GetDirectoryName(filepath));
            if (!File.Exists(filepath)) {
                var file = File.CreateText(filepath);
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
                if (errMsg != null)
                    logger.Error(errMsg);
                else
                    logger.Error(string.Format("Error opening url \"{0}\". No internet connection detected.", url));
            }
        }
    }
}