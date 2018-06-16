using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pyRevitLabs.TargetApps.Revit {
    public static class Addons {
        private const string ManifestTemplate = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""no""?>
<RevitAddIns>
    <AddIn Type = ""Application"">
        <Name>{0}</ Name>
        <Assembly>{1}</Assembly>
        <AddInId>{2}</AddInId>
        <FullClassName>{3}</FullClassName>
        <VendorId>{4}</VendorId>
    </AddIn>
</RevitAddIns>
";
        public static bool CreateManifestFile(string addinName, string assemblyPath, string addinId, string addinClassName, string vendorId, bool allusers=false) {
            string manifest = String.Format(ManifestTemplate, addinName, assemblyPath, addinId, addinClassName, vendorId);
            return true;
        }
    }
}
