using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SnowballSmash.Diagnostics
{
    /// <summary>
    /// Diagnoses "UnityEditor.dll assembly is referenced by user code, but this is not allowed".
    ///
    /// Two passes:
    ///   1. Source scan  - editor-only API used outside an Editor folder and outside #if UNITY_EDITOR.
    ///   2. Asmdef audit - editor assemblies missing "includePlatforms": ["Editor"], and runtime
    ///                     assemblies that reference an editor-only assembly.
    ///
    /// Pass 2 matters most for UPM packages, where folder names carry no meaning and only assembly
    /// definitions decide what ships in a player build.
    ///
    /// Uses no UnityEditor API of its own, so it never becomes part of the problem it diagnoses.
    /// Usage: drop on any GameObject, right-click the component header, choose "Scan For Editor References".
    /// </summary>
    public class EditorReferenceScanner : MonoBehaviour
    {
        private const string SelfFileName = "EditorReferenceScanner.cs";
        private const int MaxReportedHits = 300;

        /// <summary>
        /// Tokens treated as editor-only API usage. Matched on identifier boundaries, so "Selection."
        /// does not fire on "onWordSelection.". Extend via <see cref="extraTokens"/> rather than editing this.
        /// </summary>
        private static readonly string[] DefaultTokens =
        {
            "using UnityEditor",
            "UnityEditor.",
            "[MenuItem",
            "[CustomEditor",
            "[CustomPropertyDrawer",
            "[InitializeOnLoad",
            "AssetDatabase",
            "EditorGUILayout",
            "EditorGUI",
            "EditorUtility",
            "EditorApplication",
            "EditorPrefs",
            "EditorWindow",
            "EditorSceneManager",
            "SerializedObject",
            "SerializedProperty",
            "PrefabUtility",
            "BuildPipeline",
            "MenuCommand",
            "SceneView",
            "Selection.",
            "Handles.",
            "Undo.",
        };

        [Header("Where To Look")]
        [Tooltip("Scan the Packages folder. Required for embedded or local UPM packages, which Assets-only scans miss entirely.")]
        [SerializeField] private bool scanPackages = true;

        [Tooltip("Scan Library/PackageCache. Enable when a package is pulled from a Git URL or the registry rather than embedded.")]
        [SerializeField] private bool scanPackageCache;

        [Header("Scan Options")]
        [Tooltip("Also report files inside a folder named 'Editor'. Folder names only exempt code under Assets. Inside a UPM package they mean nothing, so enable this when scanning packages.")]
        [SerializeField] private bool includeEditorFolders;

        [Tooltip("Also report usages already wrapped in #if UNITY_EDITOR. Useful for auditing, noisy for fixing a broken build.")]
        [SerializeField] private bool includeGuardedHits;

        [Tooltip("Additional substrings to treat as editor-only API usage.")]
        [SerializeField] private string[] extraTokens = Array.Empty<string>();

        [Header("Output")]
        [Tooltip("Copy the full report to the system clipboard when the scan finishes.")]
        [SerializeField] private bool copyReportToClipboard = true;

        /// <summary>
        /// Runs both passes and writes results to the console.
        /// </summary>
        [ContextMenu("Scan For Editor References")]
        public void Scan()
        {
#if UNITY_EDITOR
            List<ScanRoot> roots = BuildRoots();

            List<string> problems = new();
            List<string> guarded = new();
            List<AsmdefInfo> assemblies = new();
            int scannedCount = 0;

            foreach (ScanRoot root in roots)
            {
                scannedCount += ScanRootFolder(root, problems, guarded, assemblies);
            }

            ReportResults(roots, scannedCount, problems, guarded, assemblies);
#else
            Debug.LogWarning("EditorReferenceScanner only runs in the editor. Source files are not present in a player build.");
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// One folder tree to scan, with the label used when printing paths.
        /// </summary>
        private readonly struct ScanRoot
        {
            public readonly string FullPath;
            public readonly string Label;

            public ScanRoot(string fullPath, string label)
            {
                FullPath = fullPath;
                Label = label;
            }
        }

        /// <summary>
        /// Parsed contents of an .asmdef file.
        /// </summary>
        [Serializable]
        private class AsmdefJson
        {
            public string name;
            public string[] includePlatforms;
            public string[] references;
        }

        /// <summary>
        /// An assembly definition plus the identity needed to resolve references between them.
        /// </summary>
        private class AsmdefInfo
        {
            public string Name;
            public string DisplayPath;
            public string Guid;
            public string[] References;
            public bool IsEditorOnly;
            public bool LooksLikeEditorAssembly;
        }

        /// <summary>
        /// Resolves which folder trees to walk based on the configured options.
        /// </summary>
        private List<ScanRoot> BuildRoots()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            List<ScanRoot> roots = new() { new ScanRoot(Application.dataPath, "Assets") };

            if (scanPackages)
            {
                string packages = Path.Combine(projectRoot, "Packages");
                if (Directory.Exists(packages))
                {
                    roots.Add(new ScanRoot(packages, "Packages"));
                }
            }

            if (scanPackageCache)
            {
                string cache = Path.Combine(projectRoot, "Library", "PackageCache");
                if (Directory.Exists(cache))
                {
                    roots.Add(new ScanRoot(cache, "Library/PackageCache"));
                }
            }

            return roots;
        }

        /// <summary>
        /// Walks one root, scanning source files and collecting assembly definitions.
        /// Returns how many source files were scanned.
        /// </summary>
        private int ScanRootFolder(ScanRoot root, List<string> problems, List<string> guarded, List<AsmdefInfo> assemblies)
        {
            int scannedCount = 0;

            foreach (string file in Directory.GetFiles(root.FullPath, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file) == SelfFileName)
                {
                    continue;
                }

                string displayPath = ToDisplayPath(file, root);

                if (!includeEditorFolders && IsInEditorFolder(displayPath))
                {
                    continue;
                }

                scannedCount++;
                ScanFile(file, displayPath, problems, guarded);
            }

            foreach (string file in Directory.GetFiles(root.FullPath, "*.asmdef", SearchOption.AllDirectories))
            {
                AsmdefInfo info = ReadAsmdef(file, root);
                if (info != null)
                {
                    assemblies.Add(info);
                }
            }

            return scannedCount;
        }

        /// <summary>
        /// Parses one .asmdef and its .meta so references can be resolved by name or GUID.
        /// </summary>
        private static AsmdefInfo ReadAsmdef(string path, ScanRoot root)
        {
            AsmdefJson parsed;

            try
            {
                parsed = JsonUtility.FromJson<AsmdefJson>(File.ReadAllText(path));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not parse {ToDisplayPath(path, root)}: {exception.Message}");
                return null;
            }

            if (parsed == null)
            {
                return null;
            }

            string displayPath = ToDisplayPath(path, root);
            string name = string.IsNullOrEmpty(parsed.name) ? Path.GetFileNameWithoutExtension(path) : parsed.name;

            bool editorOnly = parsed.includePlatforms != null
                && parsed.includePlatforms.Length == 1
                && parsed.includePlatforms[0] == "Editor";

            bool looksEditor = IsInEditorFolder(displayPath)
                || name.EndsWith("Editor", StringComparison.OrdinalIgnoreCase)
                || name.Contains(".Editor", StringComparison.OrdinalIgnoreCase);

            return new AsmdefInfo
            {
                Name = name,
                DisplayPath = displayPath,
                Guid = ReadMetaGuid(path),
                References = parsed.references ?? Array.Empty<string>(),
                IsEditorOnly = editorOnly,
                LooksLikeEditorAssembly = looksEditor,
            };
        }

        /// <summary>
        /// Pulls the asset GUID from a .meta file so GUID-style asmdef references resolve.
        /// </summary>
        private static string ReadMetaGuid(string assetPath)
        {
            string metaPath = assetPath + ".meta";

            if (!File.Exists(metaPath))
            {
                return null;
            }

            foreach (string line in File.ReadAllLines(metaPath))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("guid:", StringComparison.Ordinal))
                {
                    return trimmed.Substring(5).Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Reads one file line by line, tracking comment and #if state, and records any token hits.
        /// </summary>
        private void ScanFile(string fullPath, string displayPath, List<string> problems, List<string> guarded)
        {
            string[] lines;

            try
            {
                lines = File.ReadAllLines(fullPath);
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"Could not read {displayPath}: {exception.Message}");
                return;
            }

            List<bool> guardStack = new();
            bool inBlockComment = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string code = StripComments(lines[i], ref inBlockComment);
                string trimmed = code.Trim();

                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (trimmed[0] == '#')
                {
                    UpdateGuardStack(trimmed, guardStack);
                    continue;
                }

                string token = FindToken(code);
                if (token == null)
                {
                    continue;
                }

                string entry = $"{displayPath}:{i + 1}  ->  {token}   |   {trimmed}";

                if (guardStack.Contains(true))
                {
                    guarded.Add(entry);
                }
                else
                {
                    problems.Add(entry);
                }
            }
        }

        /// <summary>
        /// Returns the first editor-only token found on a line, or null when the line is clean.
        /// </summary>
        private string FindToken(string code)
        {
            return FindIn(code, DefaultTokens) ?? FindIn(code, extraTokens);
        }

        /// <summary>
        /// Searches a token set, accepting only matches that start at an identifier boundary.
        /// </summary>
        private static string FindIn(string code, string[] tokens)
        {
            if (tokens == null)
            {
                return null;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                int index = code.IndexOf(token, StringComparison.Ordinal);
                while (index >= 0)
                {
                    if (IsStandaloneMatch(code, index))
                    {
                        return token;
                    }

                    index = code.IndexOf(token, index + 1, StringComparison.Ordinal);
                }
            }

            return null;
        }

        /// <summary>
        /// Rejects matches that begin partway through a longer identifier, so "onWordSelection."
        /// no longer trips the "Selection." token.
        /// </summary>
        private static bool IsStandaloneMatch(string code, int index)
        {
            if (index == 0)
            {
                return true;
            }

            char before = code[index - 1];
            return !char.IsLetterOrDigit(before) && before != '_';
        }

        /// <summary>
        /// Maintains a stack of preprocessor scopes, marking which ones are UNITY_EDITOR only.
        /// Heuristic: a condition containing UNITY_EDITOR counts as a guard unless it is negated or OR'd.
        /// </summary>
        private static void UpdateGuardStack(string directive, List<bool> guardStack)
        {
            if (directive.StartsWith("#if", StringComparison.Ordinal))
            {
                guardStack.Add(IsEditorOnlyCondition(directive));
                return;
            }

            if (directive.StartsWith("#elif", StringComparison.Ordinal) && guardStack.Count > 0)
            {
                guardStack[guardStack.Count - 1] = IsEditorOnlyCondition(directive);
                return;
            }

            if (directive.StartsWith("#else", StringComparison.Ordinal) && guardStack.Count > 0)
            {
                guardStack[guardStack.Count - 1] = false;
                return;
            }

            if (directive.StartsWith("#endif", StringComparison.Ordinal) && guardStack.Count > 0)
            {
                guardStack.RemoveAt(guardStack.Count - 1);
            }
        }

        private static bool IsEditorOnlyCondition(string directive)
        {
            return directive.Contains("UNITY_EDITOR", StringComparison.Ordinal)
                && !directive.Contains("!UNITY_EDITOR", StringComparison.Ordinal)
                && !directive.Contains("||", StringComparison.Ordinal);
        }

        /// <summary>
        /// Removes line and block comments so commented-out code is not reported.
        /// </summary>
        private static string StripComments(string line, ref bool inBlockComment)
        {
            StringBuilder builder = new(line.Length);
            int index = 0;

            while (index < line.Length)
            {
                if (inBlockComment)
                {
                    int end = line.IndexOf("*/", index, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        break;
                    }

                    inBlockComment = false;
                    index = end + 2;
                    continue;
                }

                if (index + 1 < line.Length && line[index] == '/' && line[index + 1] == '/')
                {
                    break;
                }

                if (index + 1 < line.Length && line[index] == '/' && line[index + 1] == '*')
                {
                    inBlockComment = true;
                    index += 2;
                    continue;
                }

                builder.Append(line[index]);
                index++;
            }

            return builder.ToString();
        }

        private static bool IsInEditorFolder(string path)
        {
            return path.Contains("/Editor/", StringComparison.Ordinal);
        }

        private static string ToDisplayPath(string fullPath, ScanRoot root)
        {
            return root.Label + fullPath.Substring(root.FullPath.Length).Replace('\\', '/');
        }

        /// <summary>
        /// Prints both passes and optionally pushes the full report to the clipboard.
        /// </summary>
        private void ReportResults(
            List<ScanRoot> roots,
            int scannedCount,
            List<string> problems,
            List<string> guarded,
            List<AsmdefInfo> assemblies)
        {
            StringBuilder report = new();

            StringBuilder rootLabels = new();
            for (int i = 0; i < roots.Count; i++)
            {
                if (i > 0)
                {
                    rootLabels.Append(", ");
                }

                rootLabels.Append(roots[i].Label);
            }

            report.AppendLine($"Editor reference scan: {scannedCount} files across [{rootLabels}], "
                + $"{problems.Count} unguarded hits, {assemblies.Count} assembly definitions.");

            if (problems.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("--- EDITOR API IN RUNTIME CODE ---");
                AppendEntries(report, problems);
            }

            if (includeGuardedHits && guarded.Count > 0)
            {
                report.AppendLine();
                report.AppendLine($"--- ALREADY GUARDED ({guarded.Count}) ---");
                AppendEntries(report, guarded);
            }

            List<string> asmdefProblems = AuditAssemblies(assemblies);

            if (asmdefProblems.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("--- ASSEMBLY DEFINITION PROBLEMS ---");
                AppendEntries(report, asmdefProblems);
            }

            if (problems.Count == 0 && asmdefProblems.Count == 0)
            {
                report.AppendLine("Nothing found. If the build still fails, check precompiled .dll import settings,");
                report.AppendLine("or enable Scan Package Cache if the offending package comes from a Git or registry source.");
            }

            string text = report.ToString();

            if (problems.Count > 0 || asmdefProblems.Count > 0)
            {
                Debug.LogError(text);
            }
            else
            {
                Debug.Log(text);
            }

            if (copyReportToClipboard)
            {
                GUIUtility.systemCopyBuffer = text;
            }
        }

        /// <summary>
        /// Flags editor assemblies that ship to players, and runtime assemblies that pull in editor ones.
        /// </summary>
        private static List<string> AuditAssemblies(List<AsmdefInfo> assemblies)
        {
            Dictionary<string, AsmdefInfo> byName = new();
            Dictionary<string, AsmdefInfo> byGuid = new();

            foreach (AsmdefInfo info in assemblies)
            {
                byName[info.Name] = info;

                if (!string.IsNullOrEmpty(info.Guid))
                {
                    byGuid[info.Guid] = info;
                }
            }

            List<string> findings = new();

            foreach (AsmdefInfo info in assemblies)
            {
                if (info.LooksLikeEditorAssembly && !info.IsEditorOnly)
                {
                    findings.Add($"{info.DisplayPath}  ->  '{info.Name}' looks like editor code but is missing "
                        + "\"includePlatforms\": [\"Editor\"], so it compiles into player builds.");
                }

                if (info.IsEditorOnly)
                {
                    continue;
                }

                foreach (string reference in info.References)
                {
                    AsmdefInfo target = Resolve(reference, byName, byGuid);

                    if (target != null && target.IsEditorOnly)
                    {
                        findings.Add($"{info.DisplayPath}  ->  runtime assembly '{info.Name}' references "
                            + $"editor-only assembly '{target.Name}'.");
                    }
                }
            }

            return findings;
        }

        private static AsmdefInfo Resolve(string reference, Dictionary<string, AsmdefInfo> byName, Dictionary<string, AsmdefInfo> byGuid)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return null;
            }

            if (reference.StartsWith("GUID:", StringComparison.Ordinal))
            {
                string guid = reference.Substring(5).Trim();
                return byGuid.TryGetValue(guid, out AsmdefInfo byGuidMatch) ? byGuidMatch : null;
            }

            return byName.TryGetValue(reference, out AsmdefInfo byNameMatch) ? byNameMatch : null;
        }

        private static void AppendEntries(StringBuilder report, List<string> entries)
        {
            int limit = Mathf.Min(entries.Count, MaxReportedHits);

            for (int i = 0; i < limit; i++)
            {
                report.AppendLine(entries[i]);
            }

            if (entries.Count > limit)
            {
                report.AppendLine($"... and {entries.Count - limit} more.");
            }
        }
#endif
    }
}
