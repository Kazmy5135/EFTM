using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EFTM.Editor
{
    /// <summary>Imports Blender assets and replaces only the U2 environment, preserving gameplay bindings.</summary>
    public static class WarehouseEnvironmentBuilder
    {
        public const string ArtPath = "Assets/_Project/Art/Warehouse";
        public const string PrefabPath = ArtPath + "/WarehouseEnvironment.prefab";
        private const string ScenePath = "Assets/_Project/Scenes/CombatFoundationV1.unity";
        private const string RootName = "WarehouseEnvironment";

        [Serializable] private sealed class Manifest
        {
            public MaterialRecord[] materials;
            public ColliderRecord[] colliders;
            public int meshCount;
            public int triangles;
        }
        [Serializable] private sealed class MaterialRecord
        {
            public string name;
            public float[] color;
            public float roughness;
            public float metallic;
            public string albedo;
            public string normal;
        }
        [Serializable] private sealed class ColliderRecord
        {
            public string name;
            public float[] position;
            public float[] size;
        }

        [MenuItem("Tools/EFTM/Install Blender Warehouse Environment")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            BuildPrefab();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string[] legacyNames = { "Floor", "Ceiling", "LeftWall", "RightWall", "FarWall", "NearCover", "DoorFrame", "DirectionalLight" };
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (legacyNames.Contains(obj.name) || obj.name.StartsWith("CeilingBeam-", StringComparison.Ordinal) || obj.name == RootName)
                    UnityEngine.Object.DestroyImmediate(obj);
            }
            CreateEnvironment();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[EFTM] Blender warehouse installed; existing camera, input and settings retained.");
        }

        public static void InstallAndValidateForCommandLine()
        {
            Install();
            ValidateSceneAndCapture();
            ProjectFrameworkValidator.Validate();
        }

        public static void CreateEnvironment()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Build the warehouse prefab using Tools/EFTM/Install Blender Warehouse Environment first.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
            instance.name = RootName;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .009f;
            RenderSettings.fogColor = new Color(.16f, .18f, .18f);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.43f, .48f, .50f);
            RenderSettings.ambientEquatorColor = new Color(.31f, .34f, .33f);
            RenderSettings.ambientGroundColor = new Color(.20f, .21f, .19f);
            RenderSettings.ambientIntensity = 1f;
        }

        private static void BuildPrefab()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtPath + "/warehouse-manifest.json"));
            if (manifest == null || manifest.materials == null || manifest.colliders == null)
                throw new InvalidDataException("Warehouse manifest is incomplete.");
            var materialFolder = ArtPath + "/Materials";
            if (!AssetDatabase.IsValidFolder(materialFolder)) AssetDatabase.CreateFolder(ArtPath, "Materials");
            foreach (var record in manifest.materials)
            {
                var path = materialFolder + "/" + record.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(Shader.Find("Standard"));
                    AssetDatabase.CreateAsset(material, path);
                }
                material.SetFloat("_Metallic", record.metallic);
                material.SetFloat("_Glossiness", 1f - record.roughness);
                material.color = new Color(record.color[0], record.color[1], record.color[2]);
                if (!string.IsNullOrEmpty(record.albedo))
                {
                    var texture = ImportTexture(record.albedo, false);
                    material.mainTexture = texture;
                    material.color = Color.white;
                    material.SetTexture("_BumpMap", ImportTexture(record.normal, true));
                    material.SetFloat("_BumpScale", .45f);
                    material.EnableKeyword("_NORMALMAP");
                }
                if (record.name == "WH_Lamp")
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", new Color(.74f,.82f,.78f) * 2f);
                }
                EditorUtility.SetDirty(material);
            }
            var modelPath = ArtPath + "/Models/Warehouse.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.isReadable = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            foreach (var record in manifest.materials)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialFolder + "/" + record.name + ".mat");
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), record.name), material);
            }
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var root = new GameObject(RootName);
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.transform.SetParent(root.transform, false);
                instance.name = "WarehouseModel";
                // Source export accounts for handedness; reject a mirrored or rotated replacement.
                var far = FindRenderer(instance, "FarWall");
                var near = FindRenderer(instance, "NearCover");
                Debug.Log($"[EFTM] Imported far center {far.bounds.center}; near center {near.bounds.center}.");
                if (Mathf.Abs(far.bounds.center.z - 22f) > .05f || Mathf.Abs(near.bounds.center.x - 1.14f) > .05f)
                    throw new InvalidDataException($"FBX coordinate conversion mismatch: far={far.bounds.center}, near={near.bounds.center}.");
                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.gameObject.isStatic = true;
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    if (renderer.sharedMaterials.Any(m => m == null || m.shader == null || m.shader.name != "Standard"))
                        throw new InvalidDataException("A Blender material did not map to Unity Standard: " + renderer.name);
                }
                var collisions = new GameObject("Colliders");
                collisions.transform.SetParent(root.transform, false);
                foreach (var record in manifest.colliders)
                {
                    var obj = new GameObject(record.name);
                    obj.transform.SetParent(collisions.transform, false);
                    obj.transform.localPosition = V3(record.position);
                    obj.AddComponent<BoxCollider>().size = V3(record.size);
                    obj.isStatic = true;
                }
                var lights = new GameObject("Lighting");
                lights.transform.SetParent(root.transform, false);
                // A small bounded set of lights; expensive shadow maps only on the key light.
                for (var i = 0; i < 5; i++)
                {
                    var obj = new GameObject("CeilingFill-" + (i+1));
                    obj.transform.SetParent(lights.transform, false);
                    obj.transform.localPosition = new Vector3(0f, 2.6f, 2.5f + i * 4.4f);
                    var light = obj.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 6f;
                    light.intensity = 1.35f;
                    light.color = new Color(.86f,.92f,1f);
                    light.shadows = LightShadows.None;
                    light.renderMode = LightRenderMode.ForceVertex;
                }
                var keyObject = new GameObject("CorridorKey");
                keyObject.transform.SetParent(lights.transform, false);
                keyObject.transform.localPosition = new Vector3(-.8f, 2.6f, .7f);
                keyObject.transform.localRotation = Quaternion.Euler(7f, 2f, 0f);
                var key = keyObject.AddComponent<Light>();
                key.type = LightType.Spot;
                key.range = 30f;
                key.spotAngle = 100f;
                key.intensity = 1.7f;
                key.color = new Color(.95f,.94f,.86f);
                key.shadows = LightShadows.Soft;
                key.shadowStrength = .65f;
                key.shadowResolution = LightShadowResolution.Medium;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"[EFTM] Warehouse prefab: {manifest.meshCount} meshes, {manifest.triangles} triangles, {manifest.colliders.Length} box colliders.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static Texture2D ImportTexture(string filename, bool normal)
        {
            var path = ArtPath + "/Textures/" + filename;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.anisoLevel = 4;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static MeshRenderer FindRenderer(GameObject root, string name)
        {
            return root.GetComponentsInChildren<MeshRenderer>().Single(r => r.name == name);
        }
        private static Vector3 V3(float[] value) => new Vector3(value[0],value[1],value[2]);

        public static void ValidateSceneAndCapture()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var environment = scene.GetRootGameObjects().Single(o => o.name == RootName);
            var renderers = environment.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length != 21) throw new InvalidDataException("Unexpected warehouse mesh count.");
            var floor = FindRenderer(environment,"Floor").bounds;
            if (Mathf.Abs(floor.size.x - 4f) > .05f || Mathf.Abs(floor.size.z - 24f) > .05f)
                throw new InvalidDataException("FBX metre scale is invalid: " + floor.size);
            if (environment.GetComponentsInChildren<Collider>().Length < 10)
                throw new InvalidDataException("Missing environment colliders.");
            var camera = GameObject.Find("CombatCamera").GetComponent<UnityEngine.Camera>();
            var hidden = GameObject.Find("HiddenPose").transform;
            var exposed = GameObject.Find("ExposedPose").transform;
            Physics.SyncTransforms();
            // Test the gameplay-critical sightline at the original camera positions.
            var target = new Vector3(0f,1.5f,18f);
            if (!Physics.Linecast(hidden.position,target,out var hiddenHit) ||
                (hiddenHit.collider.name != "NearCover" && hiddenHit.collider.name != "DoorFrame"))
                throw new InvalidDataException("Hidden camera no longer has real near-cover occlusion.");
            if (Physics.Linecast(exposed.position,target,out var exposedHit))
                throw new InvalidDataException("Exposed central sightline is blocked by " + exposedHit.collider.name);
            var output = Path.GetFullPath(Path.Combine(Application.dataPath,"../../codex-chat-images"));
            Directory.CreateDirectory(output);
            Capture(camera, hidden.position, hidden.rotation, Path.Combine(output,"warehouse-unity-hidden.png"));
            Capture(camera, exposed.position, exposed.rotation, Path.Combine(output,"warehouse-unity-peek.png"));
            File.WriteAllText(Path.Combine(output,"warehouse-unity-validation.txt"),
                $"Unity {Application.unityVersion}\nMeshes: {renderers.Length}\nFloor: {floor.size}\nHidden ray: {hiddenHit.collider.name}\nExposed ray: clear\nCamera poses unchanged\n");
            Debug.Log("[EFTM] Warehouse scale, material mapping, hidden occlusion and exposed sightline validation passed.");
        }

        private static void Capture(UnityEngine.Camera camera, Vector3 position, Quaternion rotation, string path)
        {
            var oldPosition = camera.transform.position;
            var oldRotation = camera.transform.rotation;
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(720,1440,24,RenderTextureFormat.ARGB32);
            var image = new Texture2D(720,1440,TextureFormat.RGB24,false);
            try
            {
                camera.transform.SetPositionAndRotation(position,rotation);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,720,1440),0,0);
                image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                camera.transform.SetPositionAndRotation(oldPosition,oldRotation);
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
