using System;
using System.Collections.Generic;
using System.IO;
using EFTM.Combat.Camera;
using EFTM.Combat.Input;
using EFTM.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EFTM.Editor
{
    public static class CombatFoundationV1SceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/CombatFoundationV1.unity";
        private const string SettingsFolder = "Assets/_Project/Settings";
        private const string SettingsPath = SettingsFolder + "/CombatFoundationSettings.asset";
        private const string PanelSettingsPath = SettingsFolder + "/CombatFoundationPanelSettings.asset";
        private const string ThemePath = "Assets/_Project/UI/CombatFoundationTheme.tss";
        private const string AutoBuildFlag = "EFTM.BuildCombatFoundationV1.flag";

        [InitializeOnLoadMethod]
        private static void ScheduleRequestedBuild()
        {
            var flagPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", AutoBuildFlag);
            if (!File.Exists(flagPath))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                try
                {
                    Build();
                }
                finally
                {
                    File.Delete(flagPath);
                }
            };
        }

        [MenuItem("Tools/EFTM/Rebuild Combat Foundation V1 Scene")]
        public static void BuildFromMenu()
        {
            Build();
        }

        public static void BuildForCommandLine()
        {
            Build();
        }

        private static void Build()
        {
            EnsureFolder(SettingsFolder);
            var settings = AssetDatabase.LoadAssetAtPath<CombatFoundationSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<CombatFoundationSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var panelSettings = CreateOrUpdatePanelSettings();

            var previousActiveScene = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previousActiveScene.path))
            {
                throw new InvalidOperationException(
                    "Save or open the current editor scene before rebuilding CombatFoundationV1.");
            }

            var creationMode = Application.isBatchMode
                ? NewSceneMode.Single
                : NewSceneMode.Additive;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, creationMode);
            scene.name = "CombatFoundationV1";
            SceneManager.SetActiveScene(scene);

            ConfigureRenderSettings();
            CreateEnvironment();
            var cameraPresenter = CreateCameraRig();

            var rootObject = new GameObject("[CombatFoundationSceneRoot]");
            var document = rootObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.sortingOrder = 100f;
            var inputView = rootObject.AddComponent<CombatInputView>();
            var sceneRoot = rootObject.AddComponent<CombatFoundationSceneRoot>();
            sceneRoot.Configure(settings, inputView, cameraPresenter);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);

            if (creationMode == NewSceneMode.Additive &&
                previousActiveScene.IsValid() &&
                previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }

            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EFTM] CombatFoundationV1 scene and settings generated.");
        }

        private static PanelSettings CreateOrUpdatePanelSettings()
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                throw new InvalidOperationException($"Runtime UI theme is missing at {ThemePath}.");
            }

            panelSettings.themeStyleSheet = theme;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1080, 2160);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = 100f;
            EditorUtility.SetDirty(panelSettings);
            return panelSettings;
        }

        private static PeekCameraPresenter CreateCameraRig()
        {
            var poseRoot = new GameObject("CameraPoses").transform;

            var hiddenPose = new GameObject("HiddenPose").transform;
            hiddenPose.SetParent(poseRoot, false);
            hiddenPose.SetPositionAndRotation(
                new Vector3(0.40f, 1.55f, -2.10f),
                Quaternion.identity);

            var exposedPose = new GameObject("ExposedPose").transform;
            exposedPose.SetParent(poseRoot, false);
            exposedPose.SetPositionAndRotation(
                new Vector3(0.02f, 1.495f, -1.10f),
                Quaternion.Euler(0f, 1.03f, 7.16f));

            var cameraObject = new GameObject("CombatCamera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.fieldOfView = 54f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 60f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.08f, 1f);
            cameraObject.AddComponent<AudioListener>();

            var presenter = cameraObject.AddComponent<PeekCameraPresenter>();
            presenter.Configure(cameraObject.transform, hiddenPose, exposedPose);
            return presenter;
        }

        private static void CreateEnvironment()
        {
            CreateCube("Floor", new Vector3(0f, -0.12f, 10f), new Vector3(4f, 0.24f, 24f));
            CreateCube("Ceiling", new Vector3(0f, 3.12f, 10f), new Vector3(4f, 0.24f, 24f));
            CreateCube("LeftWall", new Vector3(-2f, 1.50f, 10f), new Vector3(0.24f, 3f, 24f));
            CreateCube("RightWall", new Vector3(2f, 1.50f, 10f), new Vector3(0.24f, 3f, 24f));
            CreateCube("FarWall", new Vector3(0f, 1.50f, 22f), new Vector3(4f, 3f, 0.24f));
            CreateCube("NearCover", new Vector3(1.64f, 1.50f, -0.02f), new Vector3(2.70f, 3.40f, 0.32f));
            CreateCube("DoorFrame", new Vector3(0.24f, 1.50f, -0.02f), new Vector3(0.18f, 3.40f, 0.46f));

            for (var index = 0; index < 6; index++)
            {
                var z = 2.6f + index * 3.25f;
                CreateCube($"CeilingBeam-{index + 1}", new Vector3(0f, 2.92f, z), new Vector3(3.72f, 0.12f, 0.18f));
            }

            var lightObject = new GameObject("DirectionalLight");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.86f, 0.94f, 1f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetPositionAndRotation(position, Quaternion.identity);
            cube.transform.localScale = scale;
            return cube;
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.11f, 0.15f, 0.16f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.31f, 0.38f, 0.40f);
            RenderSettings.ambientEquatorColor = new Color(0.20f, 0.25f, 0.26f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.12f, 0.13f);
            RenderSettings.ambientIntensity = 1.25f;
        }

        private static void EnsureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var found = false;
            for (var index = 0; index < scenes.Count; index++)
            {
                if (scenes[index].path != ScenePath)
                {
                    continue;
                }

                scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
                found = true;
                break;
            }

            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folderPath)
        {
            var segments = folderPath.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
