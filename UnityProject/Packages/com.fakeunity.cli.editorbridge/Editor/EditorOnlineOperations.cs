#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    /// <summary>
    /// Fixed, structured online Editor operations. These commands deliberately avoid arbitrary
    /// Roslyn source so routine refresh/import automation has a stable machine contract.
    /// </summary>
    internal static class EditorOnlineOperations
    {
        internal const string RefreshOperation = "asset-database-refresh";
        internal const string ImportOperation = "asset-import";
        internal const string CompileRequestOperation = "compile-request";
        internal const string PrefabExtractOperation = "prefab-extract-child";
        internal const string PrefabExtractUndoOperation = "prefab-extract-child-undo";

        internal static RoslynExecutionResult Execute(string operation, string[] requestedPaths,
            string payloadJson, string requestId)
        {
            var result = new RoslynExecutionResult { State = "runtime_error" };
            bool? structuredSuccess = null;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (String.Equals(operation, RefreshOperation, StringComparison.Ordinal))
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    result.ResultText = "AssetDatabase refresh completed";
                    result.ResultType = typeof(string).FullName;
                    result.ResultJson = ResultJson.Serialize(new Dictionary<string, object>
                    {
                        { "operation", RefreshOperation },
                        { "refreshed", true }
                    });
                }
                else if (String.Equals(operation, ImportOperation, StringComparison.Ordinal))
                {
                    var paths = (requestedPaths ?? new string[0]).Select(NormalizeAssetPath).Distinct(StringComparer.Ordinal).ToArray();
                    if (paths.Length == 0)
                        throw new ArgumentException("asset-import requires at least one Assets/ or Packages/ path.");
                    foreach (var path in paths)
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    result.Artifacts = paths;
                    result.ResultText = paths.Length + " asset path(s) imported";
                    result.ResultType = typeof(string[]).FullName;
                    result.ResultJson = ResultJson.Serialize(new Dictionary<string, object>
                    {
                        { "operation", ImportOperation },
                        { "imported", paths }
                    });
                }
                else if (String.Equals(operation, PrefabExtractOperation, StringComparison.Ordinal))
                {
                    var paths = (requestedPaths ?? new string[0]).Select(NormalizeAssetPath).ToArray();
                    if (paths.Length != 2 || paths[0] == paths[1])
                        throw new ArgumentException("prefab-extract-child requires distinct host and output Prefab paths.");
                    var payload = JsonUtility.FromJson<PrefabExtractPayload>(payloadJson ?? "{}");
                    var value = ExtractPrefabChild(paths[0], paths[1], payload, requestId);
                    result.Artifacts = payload.dry_run ? new string[0] : paths;
                    result.ResultText = payload.dry_run
                        ? "Prefab child extraction dry-run completed"
                        : "Prefab child extracted and connected";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                    result.ResultJson = ResultJson.Serialize(value);
                }
                else if (String.Equals(operation, PrefabExtractUndoOperation, StringComparison.Ordinal))
                {
                    var payload = JsonUtility.FromJson<PrefabExtractUndoPayload>(payloadJson ?? "{}");
                    var value = UndoPrefabExtract(payload.operation_id);
                    result.ResultText = "Prefab child extraction restored";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                    result.ResultJson = ResultJson.Serialize(value);
                }
                else if (String.Equals(operation, CompileRequestOperation, StringComparison.Ordinal))
                {
                    CompilationPipeline.RequestScriptCompilation();
                    result.ResultText = "Script compilation requested";
                    result.ResultType = typeof(string).FullName;
                    result.ResultJson = ResultJson.Serialize(new Dictionary<string, object>
                    {
                        { "operation", CompileRequestOperation }, { "requested", true }
                    });
                }
                else if (String.Equals(operation, OnlineProviderRegistry.ExecuteOperation, StringComparison.Ordinal))
                {
                    var execution = OnlineProviderRegistry.Execute(payloadJson, requestId);
                    result.ResultJson = execution.ResultJson;
                    result.ResultText = execution.Success
                        ? "Project online provider operation completed"
                        : "Project online provider operation " + execution.State;
                    result.ResultType = "provider-result-json";
                    result.State = execution.State;
                    result.ErrorType = execution.Success ? null : "FakeUnityCLI.OnlineProviderFailure";
                    result.ErrorMessage = execution.Success ? null : execution.Error ?? "Project provider returned failure.";
                    structuredSuccess = execution.Success;
                }
                else if (LiveBridgeUpdate.IsOperation(operation))
                {
                    var value = LiveBridgeUpdate.Execute(operation, payloadJson);
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "Bridge live update " + value["state"];
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else
                {
                    throw new NotSupportedException("Unsupported structured Editor operation: " + operation);
                }

                stopwatch.Stop();
                result.ExecuteMilliseconds = Math.Max(0, (int)stopwatch.ElapsedMilliseconds);
                result.Success = structuredSuccess ?? true;
                if (structuredSuccess == null) result.State = "success";
                return result;
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                result.ExecuteMilliseconds = Math.Max(0, (int)stopwatch.ElapsedMilliseconds);
                result.ErrorType = exception.GetType().FullName;
                result.ErrorMessage = exception.Message;
                result.ErrorStackTrace = exception.ToString();
                return result;
            }
        }

        private static string NormalizeAssetPath(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Asset path cannot be empty.");
            var normalized = path.Replace('\\', '/').Trim();
            if (normalized.StartsWith("/", StringComparison.Ordinal) || normalized.Contains(":"))
                throw new ArgumentException("Asset path must be project-relative: " + path);
            var segments = normalized.Split('/');
            if (segments.Any(segment => segment == ".." || segment == "." || segment.Length == 0))
                throw new ArgumentException("Asset path contains an unsafe segment: " + path);
            if (!(normalized.StartsWith("Assets/", StringComparison.Ordinal) || normalized == "Assets" ||
                  normalized.StartsWith("Packages/", StringComparison.Ordinal) || normalized == "Packages"))
                throw new ArgumentException("Asset path must be under Assets/ or Packages/: " + path);
            return normalized;
        }

        private static Dictionary<string, object> ExtractPrefabChild(string hostPath, string outputPath,
            PrefabExtractPayload payload, string operationId)
        {
            if (!hostPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                !outputPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Host and output paths must be Prefab assets.");
            if (String.IsNullOrWhiteSpace(payload.object_path))
                throw new ArgumentException("object_path is required.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(hostPath) == null)
                throw new FileNotFoundException("Host Prefab does not exist or is not importable: " + hostPath);
            if (AssetDatabase.LoadMainAssetAtPath(outputPath) != null || File.Exists(AbsoluteAssetPath(outputPath)))
                throw new IOException("Output Prefab already exists; overwrite is not supported: " + outputPath);
            var parentDirectory = outputPath.Substring(0, outputPath.LastIndexOf('/'));
            if (!AssetDatabase.IsValidFolder(parentDirectory))
                throw new DirectoryNotFoundException("Output asset folder does not exist: " + parentDirectory);

            GameObject hostRoot = null;
            var backupCreated = false;
            try
            {
                hostRoot = PrefabUtility.LoadPrefabContents(hostPath);
                var childTransform = FindTransform(hostRoot.transform, payload.object_path);
                if (childTransform == hostRoot.transform)
                    throw new InvalidOperationException("extract-child cannot extract the host Prefab root.");
                var originalParent = childTransform.parent;
                var hostComponent = String.IsNullOrWhiteSpace(payload.bind_component)
                    ? null : FindComponent(hostRoot.transform, payload.bind_component);
                var bindingPlan = ValidateBindings(hostComponent, payload, childTransform.gameObject, originalParent.gameObject);
                var plan = new Dictionary<string, object>
                {
                    { "host_prefab", hostPath },
                    { "object", HierarchyPath(childTransform) },
                    { "output_prefab", outputPath },
                    { "replace_source_instance", true },
                    { "connect", true },
                    { "binding_changes", bindingPlan },
                    { "affected_assets", new [] { hostPath, outputPath } }
                };
                if (payload.dry_run)
                {
                    plan["dry_run"] = true;
                    plan["changed"] = false;
                    plan["operation_id"] = null;
                    return plan;
                }

                CreateExtractBackup(operationId, hostPath, outputPath);
                backupCreated = true;
                var connected = PrefabUtility.SaveAsPrefabAssetAndConnect(
                    childTransform.gameObject, outputPath, InteractionMode.AutomatedAction);
                if (connected == null)
                    throw new InvalidOperationException("SaveAsPrefabAssetAndConnect returned null.");
                if (hostComponent != null)
                    ApplyBindings(hostComponent, payload, connected, connected.transform.parent.gameObject);
                bool hostSaved;
                PrefabUtility.SaveAsPrefabAsset(hostRoot, hostPath, out hostSaved);
                if (!hostSaved) throw new InvalidOperationException("Host Prefab could not be saved after extraction.");
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(hostPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                CompleteExtractBackup(operationId);
                plan["dry_run"] = false;
                plan["changed"] = true;
                plan["operation_id"] = operationId;
                plan["created_guid"] = AssetDatabase.AssetPathToGUID(outputPath);
                plan["connected_instance"] = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(connected) == outputPath;
                plan["semantic_readback"] = new Dictionary<string, object>
                {
                    { "host_exists", AssetDatabase.LoadAssetAtPath<GameObject>(hostPath) != null },
                    { "output_exists", AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null },
                    { "source_instance_path", outputPath }
                };
                plan["undo_command"] = "fuc prefab extract-child-undo " + operationId + " --project \"" +
                                       ProjectRoot() + "\" --yes --json";
                return plan;
            }
            catch
            {
                if (hostRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(hostRoot);
                    hostRoot = null;
                }
                if (backupCreated) RestoreExtractBackup(operationId, markUndone: false);
                throw;
            }
            finally
            {
                if (hostRoot != null) PrefabUtility.UnloadPrefabContents(hostRoot);
            }
        }

        private static Dictionary<string, object> UndoPrefabExtract(string operationId)
        {
            if (String.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("operation_id is required.");
            var manifest = ReadExtractManifest(operationId);
            if (manifest.undone)
                return new Dictionary<string, object>
                {
                    { "operation_id", operationId }, { "restored", false }, { "already_undone", true },
                    { "host_prefab", manifest.host_path }, { "removed_output", manifest.output_path }
                };
            RestoreExtractBackup(operationId, markUndone: true);
            return new Dictionary<string, object>
            {
                { "operation_id", operationId }, { "restored", true }, { "already_undone", false },
                { "host_prefab", manifest.host_path }, { "removed_output", manifest.output_path }
            };
        }

        private static List<Dictionary<string, object>> ValidateBindings(Component hostComponent,
            PrefabExtractPayload payload, GameObject extracted, GameObject container)
        {
            var result = new List<Dictionary<string, object>>();
            if (hostComponent == null) return result;
            result.Add(DescribeBinding(hostComponent, payload.bind_field, extracted, "extracted"));
            if (!String.IsNullOrWhiteSpace(payload.bind_container_field))
                result.Add(DescribeBinding(hostComponent, payload.bind_container_field, container, "container"));
            return result;
        }

        private static void ApplyBindings(Component hostComponent, PrefabExtractPayload payload,
            GameObject extracted, GameObject container)
        {
            var serialized = new SerializedObject(hostComponent);
            serialized.Update();
            SetBinding(serialized, hostComponent, payload.bind_field, extracted);
            if (!String.IsNullOrWhiteSpace(payload.bind_container_field))
                SetBinding(serialized, hostComponent, payload.bind_container_field, container);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hostComponent);
        }

        private static Dictionary<string, object> DescribeBinding(Component host, string fieldName,
            GameObject target, string targetRole)
        {
            var field = FindField(host.GetType(), fieldName);
            var value = ResolveBindingTarget(field.FieldType, target);
            var serialized = new SerializedObject(host);
            var property = serialized.FindProperty(fieldName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException(host.GetType().FullName + "." + fieldName +
                    " is not a serialized Unity Object reference field.");
            return new Dictionary<string, object>
            {
                { "component", host.GetType().FullName }, { "field", fieldName },
                { "expected_type", field.FieldType.FullName }, { "target_role", targetRole },
                { "resolved_target", value.GetType().FullName }
            };
        }

        private static void SetBinding(SerializedObject serialized, Component host, string fieldName, GameObject target)
        {
            var field = FindField(host.GetType(), fieldName);
            var property = serialized.FindProperty(fieldName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException(host.GetType().FullName + "." + fieldName +
                    " is not a serialized Unity Object reference field.");
            property.objectReferenceValue = ResolveBindingTarget(field.FieldType, target);
        }

        private static UnityEngine.Object ResolveBindingTarget(Type expected, GameObject target)
        {
            if (expected.IsAssignableFrom(target.GetType())) return target;
            var candidates = target.GetComponents<Component>().Where(component =>
                component != null && expected.IsAssignableFrom(component.GetType())).ToArray();
            if (candidates.Length != 1)
                throw new InvalidOperationException("Binding target " + HierarchyPath(target.transform) +
                    " has " + candidates.Length + " component(s) assignable to " + expected.FullName + ".");
            return candidates[0];
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            if (String.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("Binding field cannot be empty.");
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            throw new MissingFieldException(type.FullName, fieldName);
        }

        private static Component FindComponent(Transform root, string selector)
        {
            var separator = selector.LastIndexOf("::", StringComparison.Ordinal);
            if (separator <= 0 || separator == selector.Length - 2)
                throw new ArgumentException("bind_component must use /Root/Path::Type.");
            var transform = FindTransform(root, selector.Substring(0, separator));
            var typeName = selector.Substring(separator + 2);
            var matches = transform.GetComponents<Component>().Where(component => component != null &&
                (component.GetType().FullName == typeName || component.GetType().Name == typeName)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Component selector " + selector + " matched " + matches.Length + " components.");
            return matches[0];
        }

        private static Transform FindTransform(Transform root, string path)
        {
            var segments = path.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) throw new ArgumentException("Object path cannot be empty.");
            var index = segments[0] == root.name ? 1 : 0;
            var current = root;
            for (; index < segments.Length; index++)
            {
                var matches = Enumerable.Range(0, current.childCount).Select(current.GetChild)
                    .Where(child => child.name == segments[index]).ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException("Object path segment '" + segments[index] +
                        "' matched " + matches.Length + " children under " + HierarchyPath(current) + ".");
                current = matches[0];
            }
            return current;
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new List<string>();
            for (var current = transform; current != null; current = current.parent) names.Add(current.name);
            names.Reverse();
            return "/" + String.Join("/", names.ToArray());
        }

        private static void CreateExtractBackup(string operationId, string hostPath, string outputPath)
        {
            var directory = ExtractBackupDirectory(operationId);
            if (Directory.Exists(directory)) throw new IOException("Extract operation backup already exists: " + operationId);
            Directory.CreateDirectory(directory);
            File.Copy(AbsoluteAssetPath(hostPath), Path.Combine(directory, "host.prefab"), false);
            var hostMeta = AbsoluteAssetPath(hostPath) + ".meta";
            if (File.Exists(hostMeta)) File.Copy(hostMeta, Path.Combine(directory, "host.prefab.meta"), false);
            WriteExtractManifest(directory, new PrefabExtractBackupManifest
            {
                operation_id = operationId, host_path = hostPath, output_path = outputPath,
                completed = false, undone = false
            });
        }

        private static void CompleteExtractBackup(string operationId)
        {
            var manifest = ReadExtractManifest(operationId);
            manifest.completed = true;
            WriteExtractManifest(ExtractBackupDirectory(operationId), manifest);
        }

        private static void RestoreExtractBackup(string operationId, bool markUndone)
        {
            var directory = ExtractBackupDirectory(operationId);
            var manifest = ReadExtractManifest(operationId);
            File.Copy(Path.Combine(directory, "host.prefab"), AbsoluteAssetPath(manifest.host_path), true);
            var backupMeta = Path.Combine(directory, "host.prefab.meta");
            if (File.Exists(backupMeta)) File.Copy(backupMeta, AbsoluteAssetPath(manifest.host_path) + ".meta", true);
            if (AssetDatabase.LoadMainAssetAtPath(manifest.output_path) != null)
                AssetDatabase.DeleteAsset(manifest.output_path);
            else
            {
                TryDelete(AbsoluteAssetPath(manifest.output_path));
                TryDelete(AbsoluteAssetPath(manifest.output_path) + ".meta");
            }
            AssetDatabase.ImportAsset(manifest.host_path,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            if (markUndone)
            {
                manifest.undone = true;
                WriteExtractManifest(directory, manifest);
            }
        }

        private static PrefabExtractBackupManifest ReadExtractManifest(string operationId)
        {
            var path = Path.Combine(ExtractBackupDirectory(operationId), "manifest.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Extract operation backup was not found: " + operationId);
            var value = JsonUtility.FromJson<PrefabExtractBackupManifest>(File.ReadAllText(path));
            if (value == null || value.operation_id != operationId)
                throw new InvalidDataException("Extract backup manifest is invalid: " + operationId);
            return value;
        }

        private static void WriteExtractManifest(string directory, PrefabExtractBackupManifest manifest) =>
            File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonUtility.ToJson(manifest, true));

        private static string ExtractBackupDirectory(string operationId) =>
            Path.Combine(ProjectRoot(), "Library", "FakeUnityCLI", "prefab-extract-undo", operationId);

        private static string AbsoluteAssetPath(string path) =>
            Path.Combine(ProjectRoot(), path.Replace('/', Path.DirectorySeparatorChar));

        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        [Serializable]
        private sealed class PrefabExtractPayload
        {
            public string object_path;
            public string bind_component;
            public string bind_field;
            public string bind_container_field;
            public bool dry_run;
        }

        [Serializable]
        private sealed class PrefabExtractUndoPayload { public string operation_id; }

        [Serializable]
        private sealed class PrefabExtractBackupManifest
        {
            public string operation_id;
            public string host_path;
            public string output_path;
            public bool completed;
            public bool undone;
        }
    }
}
#endif
