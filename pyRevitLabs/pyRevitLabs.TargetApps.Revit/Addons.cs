using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using pyRevitLabs.Common;
using NLog;

namespace pyRevitLabs.TargetApps.Revit {
    public class RevitAddonManifest {
        public RevitAddonManifest(string manifestFile) {
            FilePath = manifestFile;

            var doc = new XmlDocument();
            try {
                doc.Load(manifestFile);
                Name = doc.DocumentElement.SelectSingleNode("/RevitAddIns/AddIn/Name").InnerText;
                Assembly = doc.DocumentElement.SelectSingleNode("/RevitAddIns/AddIn/Assembly").InnerText;
                AddInId = doc.DocumentElement.SelectSingleNode("/RevitAddIns/AddIn/AddInId").InnerText;
                FullClassName = doc.DocumentElement.SelectSingleNode("/RevitAddIns/AddIn/FullClassName").InnerText;
                VendorId = doc.DocumentElement.SelectSingleNode("/RevitAddIns/AddIn/VendorId").InnerText;
            }
            catch { }
        }

        public string FilePath { get; set; }

        public string Name { get; set; }
        public string Assembly { get; set; }
        public string AddInId { get; set; }
        public string FullClassName { get; set; }
        public string VendorId { get; set; }
    }

    public static class Addons {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // TODO: generate this using xml module so other metadata could be added inside <AddIn> (tested)
        // <pyRevitClonePath>{5}</pyRevitClonePath>
        // <pyRevitEngineVersion>{6}</pyRevitEngineVersion>
        private const string ManifestTemplate = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""no""?>
<RevitAddIns>
    <AddIn Type = ""Application"">
        <Name>{0}</Name>
        <Assembly>{1}</Assembly>
        <AddInId>{2}</AddInId>
        <FullClassName>{3}</FullClassName>
        <VendorId>{4}</VendorId>
    </AddIn>
</RevitAddIns>
";
        public static string GetRevitAddonsFolder(Version revitVersion, bool allUsers = false) {
            var rootFolder = allUsers ? Environment.SpecialFolder.CommonApplicationData : Environment.SpecialFolder.ApplicationData;
            return Path.Combine(Environment.GetFolderPath(rootFolder), "Autodesk", "Revit", "Addins", revitVersion.Major.ToString());
        }

        public static string GetRevitAddonsFilePath(Version revitVersion, string addinFileName, bool allusers = false) {
            var rootFolder = allusers ? Environment.SpecialFolder.CommonApplicationData : Environment.SpecialFolder.ApplicationData;
            return Path.Combine(GetRevitAddonsFolder(revitVersion, allUsers: allusers), addinFileName + ".addin");
        }

        public static void CreateManifestFile(Version revitVersion, string addinFileName,
                                              string addinName, string assemblyPath, string addinId, string addinClassName, string vendorId,
                                              bool allusers = false) {
            string manifest = String.Format(ManifestTemplate, addinName, assemblyPath, addinId, addinClassName, vendorId);
            logger.Debug(string.Format("Creating addin manifest...\n{0}", manifest));
            var addinFile = GetRevitAddonsFilePath(revitVersion, addinFileName, allusers: allusers);
            logger.Debug(string.Format("Creating manifest file {0}", addinFile));
            CommonUtils.ConfirmFile(addinFile);
            var f = File.CreateText(addinFile);
            f.Write(manifest);
            f.Close();
        }

        public static void RemoveManifestFile(Version revitVersion, string addinName, bool currentAndAllUsers = true) {
            var revitManifest = GetManifest(revitVersion, addinName, allUsers: false);
            if (revitManifest != null)
                File.Delete(revitManifest.FilePath);
            if (currentAndAllUsers) {
                revitManifest = GetManifest(revitVersion, addinName, allUsers: true);
                if (revitManifest != null)
                    File.Delete(revitManifest.FilePath);
            }
        }

        public static RevitAddonManifest GetManifest(Version revitVersion, string addinName, bool allUsers) {
            string addinPath = GetRevitAddonsFolder(revitVersion, allUsers: allUsers);
            if (Directory.Exists(addinPath)) {
                foreach (string file in Directory.GetFiles(addinPath)) {
                    if (file.ToLower().EndsWith(".addin")) {
                        try {
                            var revitManifest = new RevitAddonManifest(file);
                            if (revitManifest.Name.ToLower() == addinName.ToLower())
                                return revitManifest;
                        }
                        catch { }
                    }
                }
            }

            return null;
        }
    }
}
