using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EFTM.Editor
{
    public static class ProjectFrameworkValidator
    {
        private const string ExpectedEditorVersion = "2022.3.62f2";

        private static readonly string[] RequiredPaths =
        {
            "Assets/_Project/Runtime/EFTM.Runtime.asmdef",
            "Assets/_Project/Editor/EFTM.Editor.asmdef",
            "Assets/_Project/Tests/EditMode/EFTM.Tests.EditMode.asmdef",
            "Assets/_Project/Scenes/Bootstrap.unity",
            "Packages/manifest.json",
            "ProjectSettings/ProjectVersion.txt",
            "Docs/README.md"
        };

        [MenuItem("Tools/EFTM/Validate Project Framework")]
        public static void Validate()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("[EFTM] Could not resolve the project root.");
                return;
            }

            var failures = new List<string>();

            foreach (var relativePath in RequiredPaths)
            {
                var fullPath = Path.Combine(projectRoot, relativePath);
                if (!File.Exists(fullPath))
                {
                    failures.Add($"Missing required file: {relativePath}");
                }
            }

            if (Application.unityVersion != ExpectedEditorVersion)
            {
                failures.Add(
                    $"Editor version is {Application.unityVersion}; expected {ExpectedEditorVersion}.");
            }

            var bootstrapScene = EditorBuildSettings.scenes;
            if (bootstrapScene.Length == 0 ||
                bootstrapScene[0].path != "Assets/_Project/Scenes/Bootstrap.unity" ||
                !bootstrapScene[0].enabled)
            {
                failures.Add("Bootstrap scene must be the first enabled Build Settings scene.");
            }

            if (failures.Count == 0)
            {
                Debug.Log("[EFTM] Project framework validation passed.");
                return;
            }

            Debug.LogError("[EFTM] Project framework validation failed:\n- " +
                           string.Join("\n- ", failures));
        }
    }
}
