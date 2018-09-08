using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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
        private static Regex DriveLetterFinder = new Regex(@"^(?<drive>[A-Za-z]):");
        private static Regex GuidFinder = new Regex(@".*(?<guid>[0-9A-Fa-f]{8}[-]" +
                                                        "[0-9A-Fa-f]{4}[-]" +
                                                        "[0-9A-Fa-f]{4}[-]" +
                                                        "[0-9A-Fa-f]{4}[-]" + 
                                                        "[0-9A-Fa-f]{12}).*");

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
            var normedPath = 
                Path.GetFullPath(
                    new Uri(path).LocalPath).TrimEnd(Path.DirectorySeparatorChar,
                                                     Path.AltDirectorySeparatorChar);
            var match = DriveLetterFinder.Match(normedPath);
            if (match.Success) {
                var driveLetter = match.Groups["drive"].Value + ":";
                normedPath = normedPath.Replace(driveLetter, driveLetter.ToUpper());
            }

            return normedPath;
        }

        public static Version ConvertToVersion(this string version) {
            if (!version.Contains("."))
                version = version + ".0";
            return new Version(version);
        }

        public static List<string> ConvertFromCommaSeparated(this string commaSeparatedValue) {
            return new List<string>(commaSeparatedValue.Split(','));
        }

        public static List<string> ConvertFromTomlList(this string commaSeparatedValue) {
            var cleanedValue = commaSeparatedValue.Replace("[", "").Replace("]", "");
            var quotedValues = new List<string>(cleanedValue.Split(','));
            var results = new List<string>();
            var valueFinder = new Regex(@"'(?<value>.+)'");
            foreach(var value in quotedValues) {
                var m = valueFinder.Match(value);
                if (m.Success)
                    results.Add(m.Groups["value"].Value);
            }
            return results;
        }

        public static Guid ExtractGuid(this string inputString) {
            var zeroGuid = new Guid();
            var m = GuidFinder.Match(inputString);
            if (m.Success) {
                try {
                    var guid = new Guid(m.Groups["guid"].Value);
                    if (guid != zeroGuid)
                        return guid;
                } catch {
                    return zeroGuid;
                }
            }
            return zeroGuid;
        }

        public static bool IsValidUrl(this string sourceString) {
            Uri uriResult;
            return Uri.TryCreate(sourceString, UriKind.Absolute, out uriResult);
        }
    }

    public static class DateTimeExtensions {
        public static string NeatTime(this DateTime sourceDate) {
            return String.Format("{0:dd/MM/yyyy HH:mm:ss}", sourceDate);
        }
    }

    public static class StringEnumerableExtensions {
        public static string ConvertToCommaSeparatedString(this IEnumerable<string> sourceValues) {
            return string.Join(",", sourceValues);
        }

        public static string ConvertToTomlListString(this IEnumerable<string> sourceValues) {
            var quotedValues = new List<string>();
            foreach (var value in sourceValues)
                quotedValues.Add(string.Format("'{0}'", value));
            return "[" + string.Join(",", quotedValues) + "]";
        }
    }
}
