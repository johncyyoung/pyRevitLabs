using System;
using System.IO;
using System.Reflection;

// from this link (method 3)
// https://support.microsoft.com/en-us/help/837908/how-to-load-an-assembly-at-runtime-that-is-located-in-a-folder-that-is

namespace pyRevitLabs.TargetApps.Revit {
    public static class PyRevitBindings {
        // activates a resolver that looks into the current binary folder to find missing libraries
        public static void ActivateResolver() {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
            // this handler is called only when the common language runtime tries to bind to the assembly and fails
            // retrieve the list of referenced assemblies in an array of AssemblyName
            Assembly MyAssembly, executingAssm;
            string assmDllName = "";

            executingAssm = Assembly.GetExecutingAssembly();
            AssemblyName[] refrencedAssemblies = executingAssm.GetReferencedAssemblies();

            // loop through the array of referenced assembly names
            foreach (AssemblyName assmName in refrencedAssemblies) {
                // check for the assembly names that have raised the "AssemblyResolve" event
                if (assmName.FullName.Split(',')[0] == args.Name.Split(',')[0]) {
                    // build the path of the assembly from where it has to be loaded.
                    assmDllName = args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                    break;
                }
            }

            var fullAssmDllPath = Path.Combine(Path.GetDirectoryName(typeof(PyRevit).Assembly.Location), assmDllName);
            if (File.Exists(fullAssmDllPath)) {
                // load the assembly from the specified path. 
                MyAssembly = Assembly.LoadFrom(fullAssmDllPath);

                // return the loaded assembly.
                return MyAssembly;
            }

            return null;
        }
    }
}
