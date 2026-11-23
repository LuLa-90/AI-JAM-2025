using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIJam.Validation {
    public enum Severity { Error, Warning, Info }

    /// <summary>One validation result. Path is relative to the checked prefab root ("" = root).</summary>
    [System.Serializable]
    public class Finding {
        public Severity Severity;
        public string Rule;
        public string Message;
        public string Path;
        [Tooltip("What was checked: Team, Robot or Arena (the prefab Path is relative to).")]
        public string Source = "";

        public Finding(Severity severity, string rule, string message, string path = "") {
            Severity = severity;
            Rule = rule;
            Message = message;
            Path = path ?? "";
        }

        public override string ToString() {
            string where = string.IsNullOrEmpty(Path) ? "" : $"  [{Path}]";
            string src = string.IsNullOrEmpty(Source) ? "" : Source + " · ";
            return $"{Severity.ToString().ToUpper()} {src}{Rule}: {Message}{where}";
        }
    }

    public static class Findings {
        public static bool HasErrors(this IEnumerable<Finding> f) => f.Any(x => x.Severity == Severity.Error);

        public static string Report(string title, IEnumerable<Finding> findings) {
            var list = findings.ToList();
            int e = list.Count(x => x.Severity == Severity.Error), w = list.Count(x => x.Severity == Severity.Warning);
            var lines = new List<string> { $"{title}: {(e == 0 ? "PASS" : "FAIL")} ({e} errors, {w} warnings)" };
            lines.AddRange(list.OrderBy(x => x.Severity).Select(x => "  " + x));
            return string.Join("\n", lines);
        }

        public static string PathOf(Transform t, Transform root) {
            if (t == null || t == root) return "";
            var parts = new List<string>();
            for (var c = t; c != null && c != root; c = c.parent) parts.Add(c.name);
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
