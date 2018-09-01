using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pyRevitLabs.Common {
    public enum ErrorCodes {
        NoErrors,
        PathIsNotValidGitRepo,
        PathDoesNotExist,
        FileDoesNotExist,
    }


    public sealed class Errors {
        private static Errors instance = null;
        private static readonly object padlock = new object();

        Errors() {
        }

        public static Errors Instance {
            get {
                lock (padlock) {
                    if (instance == null) {
                        instance = new Errors();
                    }
                    return instance;
                }
            }
        }

        public static ErrorCodes LatestError { get; set; } = ErrorCodes.NoErrors;
    }
}
