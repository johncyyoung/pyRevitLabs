using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using pyRevitLabs.Common;

namespace pyRevitLabs.TargetApps.Revit {
    public static class Addons {
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
        public static string GetRevitAddonsFolder(Version revitVersion, bool allusers = false) {
            var rootFolder = allusers ? Environment.SpecialFolder.CommonApplicationData : Environment.SpecialFolder.ApplicationData;
            return Path.Combine(Environment.GetFolderPath(rootFolder), "Autodesk", "Revit", "Addins", revitVersion.Major.ToString());
        }

        public static string GetRevitAddonsFilePath(Version revitVersion, string addinFileName, bool allusers = false) {
            var rootFolder = allusers ? Environment.SpecialFolder.CommonApplicationData : Environment.SpecialFolder.ApplicationData;
            return Path.Combine(GetRevitAddonsFolder(revitVersion, allusers: allusers), addinFileName + ".addin");
        }

        public static void CreateManifestFile(Version revitVersion, string addinFileName,
                                              string addinName, string assemblyPath, string addinId, string addinClassName, string vendorId,
                                              bool allusers = false) {
            string manifest = String.Format(ManifestTemplate, addinName, assemblyPath, addinId, addinClassName, vendorId);
            var addinFile = GetRevitAddonsFilePath(revitVersion, addinFileName, allusers: allusers);
            pyRevitUtils.ConfirmFile(addinFile);
            var f = File.CreateText(addinFile);
            f.Write(manifest);
            f.Close();
        }

        public static void RemoveManifestFile(Version revitVersion, string addinName) {
            var manifestFile = GetManifestFile(revitVersion, addinName);
            if (manifestFile != null)
                File.Delete(manifestFile);
        }

        public static string GetManifestFile(Version revitVersion, string addinName) {
            string addinPath = GetRevitAddonsFolder(revitVersion);
            if (Directory.Exists(addinPath)) {
                foreach (string file in Directory.GetFiles(addinPath)) {
                    var doc = new XmlDocument();
                    if (file.ToLower().EndsWith(".addin")) {
                        try {
                            doc.Load(file);
                            var nameElement = doc.DocumentElement.SelectSingleNode("/RevitAddIns/AddIn/Name");
                            if (nameElement != null && nameElement.InnerText.ToLower() == addinName.ToLower())
                                return file;
                        }
                        catch { }
                    }
                }
            }

            return null;
        }
    }
}
