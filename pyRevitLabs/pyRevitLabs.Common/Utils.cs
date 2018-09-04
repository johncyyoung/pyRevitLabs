using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using NLog;

namespace pyRevitLabs.Common {
    public static class CommonUtils {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // helper for deleting directories recursively
        // @handled @logs
        public static void DeleteDirectory(string targetDir) {
            if (Directory.Exists(targetDir)) {
                logger.Debug(string.Format("Recursive deleting directory {0}", targetDir));
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
                    throw new pyRevitException(string.Format("Error recursive deleting directory {0}", targetDir));
                }
            }
        }

        public static void ConfirmPath(string path) {
            Directory.CreateDirectory(path);
        }

        public static void ConfirmFile(string filepath) {
            ConfirmPath(Path.GetDirectoryName(filepath));
            if (!File.Exists(filepath)) {
                var file = File.CreateText(filepath);
                file.Close();
            }
        }

        public static string DownloadFile(string url, string destPath) {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var client = new WebClient()) {
                client.DownloadFile(url, destPath);
            }

            return destPath;
        }
    }
}
