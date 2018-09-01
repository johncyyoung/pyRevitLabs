using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pyRevitLabs.Common.Extensions {
    public static class FileSizeExtension {
        public static string CleanupSize(this float bytes) {
            var sizes = new List<String> { "Bytes", "KB", "MB", "GB", "TB" };

            if (bytes == 0)
                return "0 Byte";

            var i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
            if (i >= 0 && i <= sizes.Count)
                return Math.Round(bytes / Math.Pow(1024, i), 2) + " " + sizes[i];
            else
                return bytes + " Bytes";
        }

        public static string CleanupSize(this long bytes) {
            return CleanupSize((float)bytes);
        }

        public static string CleanupSize(this int bytes) {
            return CleanupSize((float)bytes);
        }
    }

    public static class InputExtensions {
        public static int LimitToRange(this int value, int inclusiveMinimum, int inclusiveMaximum) {
            if (value < inclusiveMinimum) { return inclusiveMinimum; }
            if (value > inclusiveMaximum) { return inclusiveMaximum; }
            return value;
        }
    }

    public static class StringExtensions {
        public static string GetDisplayPath(this string sourceString) {
            var separator = Path.AltDirectorySeparatorChar.ToString();
            return sourceString.Replace("||", separator)
                               .Replace("|\\", separator)
                               .Replace("|", separator)
                               .Replace(Path.DirectorySeparatorChar.ToString(), separator);
        }

        public static string TripleDot(this string sourceString, uint maxLength) {
            if (sourceString.Length > maxLength && maxLength > 3)
                return sourceString.Substring(0, (int)maxLength - 3) + "...";
            else
                return sourceString;
        }

        public static string NullToNA(this string sourceString) {
            if (sourceString == "" || sourceString == null)
                return "N/A";
            else
                return sourceString;
        }

        public static string NormalizeAsPath(this string path) {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }

        public static Version ConvertToVersion(this string version) {
            if (!version.Contains("."))
                version = version + ".0";
            return new Version(version);
        }
    }

    public static class DateTimeExtensions {
        public static string NeatTime(this DateTime sourceDate) {
            return String.Format("{0:dd/MM/yyyy HH:mm:ss}", sourceDate);
        }
    }
}
