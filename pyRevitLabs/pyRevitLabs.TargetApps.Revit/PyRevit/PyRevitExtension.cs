using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using pyRevitLabs.Common;

using Newtonsoft.Json.Linq;
using NLog;

namespace pyRevitLabs.TargetApps.Revit {
    public class PyRevitExtension {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private dynamic _jsonObj;

        public PyRevitExtension(JObject jsonObj) {
            _jsonObj = jsonObj;
        }

        public PyRevitExtension(string extensionPath) {
            InstallPath = extensionPath;
        }

        public override string ToString() { return _jsonObj.ToString(); }

        public static string MakeConfigName(string extName, PyRevitExtensionTypes extType) {
            return extType ==
                PyRevitExtensionTypes.UIExtension ?
                    extName + PyRevitConsts.UIExtensionDirPostfix : extName + PyRevitConsts.LibraryExtensionDirPostfix;
        }

        public static bool IsExtensionDirectory(string path) {
            return path.EndsWith(PyRevitConsts.UIExtensionDirPostfix)
                    || path.EndsWith(PyRevitConsts.LibraryExtensionDirPostfix);
        }

        private string GetNameFromInstall() {
            return Path.GetFileName(InstallPath)
                       .Replace(PyRevitConsts.UIExtensionDirPostfix, "")
                       .Replace(PyRevitConsts.LibraryExtensionDirPostfix, "");
        }

        public bool BuiltIn { get { return bool.Parse(_jsonObj.builtin); } }
        public bool RocketModeCompatible { get { return bool.Parse(_jsonObj.rocket_mode_compatible); } }

        public string Name {
            get {
                if (_jsonObj != null)
                    return _jsonObj.name;
                else
                    return GetNameFromInstall();
            }
        }

        public string Description { get { return _jsonObj != null ? _jsonObj.description : ""; } }

        public string Author { get { return _jsonObj != null ? _jsonObj.author : ""; } }

        public string AuthorProfile { get { return _jsonObj != null ? _jsonObj.author_url : ""; } }

        public string Url { get { return _jsonObj != null ? _jsonObj.url : ""; } }

        public string Website { get { return _jsonObj != null ? _jsonObj.website : ""; } }

        public string InstallPath { get; private set; }

        public PyRevitExtensionTypes Type {
            get {
                return _jsonObj.type == "extension" ?
                    PyRevitExtensionTypes.UIExtension : PyRevitExtensionTypes.LibraryExtension;
            }
        }

        public string ConfigName {
            get {
                return MakeConfigName(Name, Type);
            }
        }

        // force update extension
        // @handled @logs
        public void Update() {
            logger.Debug(string.Format("Updating extension \"{0}\"", Name));
            logger.Debug(string.Format("Updating extension repo at \"{0}\"", InstallPath));
            var res = GitInstaller.ForcedUpdate(InstallPath);
            if (res <= UpdateStatus.Conflicts)
                throw new pyRevitException(string.Format("Error updating extension \"{0}\" installed at \"{1}\"",
                                                         Name, InstallPath));
        }

    }
}
