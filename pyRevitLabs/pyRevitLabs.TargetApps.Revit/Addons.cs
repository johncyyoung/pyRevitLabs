using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static string GetRevitAddonsFolder(string revitVersion, bool allusers = false) {
            var rootFolder = allusers ? Environment.SpecialFolder.CommonApplicationData : Environment.SpecialFolder.ApplicationData;
            return Path.Combine(Environment.GetFolderPath(rootFolder), "Autodesk", "Revit", "Addins", revitVersion);
        }

        public static string GetRevitAddonsFilePath(string revitVersion, string addinFileName, bool allusers = false) {
            var rootFolder = allusers ? Environment.SpecialFolder.CommonApplicationData : Environment.SpecialFolder.ApplicationData;
            return Path.Combine(GetRevitAddonsFolder(revitVersion, allusers: allusers), addinFileName + ".addin");
        }

        public static void CreateManifestFile(int revitVersion, string addinFileName,
                                              string addinName, string assemblyPath, string addinId, string addinClassName, string vendorId,
                                              bool allusers=false) {
            string manifest = String.Format(ManifestTemplate, addinName, assemblyPath, addinId, addinClassName, vendorId);
            var addinFile = GetRevitAddonsFilePath(revitVersion.ToString(), addinFileName, allusers: allusers);
            pyRevitUtils.ConfirmFile(addinFile);
            var f = File.CreateText(addinFile);
            f.Write(manifest);
            f.Close();
        }

        public static void RemoveManifestFile(string revitVersion, string addinNameOrFileName) {
            // TODO: implement remove
        }
    }
}
