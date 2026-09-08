using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFTM.Combat.Camera;
using EFTM.Combat.Foundation;
using EFTM.Combat.Presentation;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EFTM.Editor
{
    /// <summary>Imports authored Blender layout only; never creates or moves environment geometry.</summary>
    public static class CoverSwitchSceneBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/CombatFoundationV1.unity";
        public static JObject ReadManifest()
        {
            var manifest = JObject.Parse(File.ReadAllText(WarehouseEnvironmentBuilder.ArtPath + "/warehouse-manifest.json"));
            if ((int?)manifest["schemaVersion"] != 2 || (string)manifest["units"] != "meters" ||
                (string)manifest["coordinateSystem"] != "Unity-X-right-Y-up-Z-forward" ||
                string.IsNullOrWhiteSpace((string)manifest["sourceRevision"]) ||
                !(manifest["coverSides"] is JArray sides) || sides.Count != 2 ||
                !(manifest["switchPaths"] is JArray paths) || paths.Count != 2)
                throw new InvalidDataException("Cover switch requires Blender schema v2 with two sides and two paths.");
            foreach (var name in new[] { "Right", "Left" })
            {
                var side = sides.Single(s => (string)s["side"] == name);
                foreach (var key in new[] { "playerAnchor", "hiddenPosition", "hiddenEuler", "exposedPosition", "exposedEuler" }) Vector(side[key]);
                var path = paths.Single(p => (string)p["source"] == name);
                if ((string)path["target"] != (name == "Right" ? "Left" : "Right") ||
                    !(path["controlPoints"] is JArray points) || points.Count != 4)
                    throw new InvalidDataException("Cover path must connect opposite sides with four control points.");
                foreach (var point in points) Vector(point);
                if (!manifest["colliders"].Any(c => (string)c["name"] == (string)side["coverName"]))
                    throw new InvalidDataException("Missing authored cover collider: " + name);
            }
            return manifest;
        }

        [MenuItem("Tools/EFTM/Upgrade Cover Switch 008")]
        public static void UpgradeCoverSwitch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play before migration.");
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save/reconcile modified scenes before migration.");
            ReadManifest();
            WarehouseEnvironmentBuilder.BuildPrefab();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Find<CombatFoundationSceneRoot>(scene);
            if (root == null || root.Targeting == null || root.Shots == null)
                throw new InvalidOperationException("Existing combat encounter must be preserved; adapters are missing.");
            Bind(root);
            CombatFoundationAdaptersBuilder.InstallFont(root);
            ValidateAndCapture(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[EFTM] 008 dual-cover migration validated and saved; encounter and scene GUID preserved.");
        }

        public static void Bind(CombatFoundationSceneRoot root)
        {
            var manifest = ReadManifest();
            var scene = root.gameObject.scene;
            var rig = Find<CoverSideRig>(scene);
            if (rig == null) rig = new GameObject("CoverSideRig").AddComponent<CoverSideRig>();
            var right = CreateSide(rig.transform, manifest, CoverSide.Right);
            var left = CreateSide(rig.transform, manifest, CoverSide.Left);
            rig.Configure((string)manifest["sourceRevision"], right, left);
            var player = scene.GetRootGameObjects().SingleOrDefault(o => o.name == "PlayerRoot");
            if (player == null) player = new GameObject("PlayerRoot");
            var transition = player.GetComponent<CoverTransitionPresenter>() ?? player.AddComponent<CoverTransitionPresenter>();
            transition.Configure(rig, player.transform, LayerMask.GetMask("CombatOccluder"));
            transition.ConfigureMotion(root.Settings);
            player.transform.SetPositionAndRotation(right.playerAnchor.position, right.playerAnchor.rotation);
            root.CameraPresenter.Configure(root.CameraPresenter.ViewTransform, right.hiddenPose, right.exposedPose);
            root.CameraPresenter.ConfigureCoverSwitch(transition);
            var legacy = scene.GetRootGameObjects().SingleOrDefault(o => o.name == "CameraPoses");
            if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy);
            var environment = scene.GetRootGameObjects().Single(o => o.name == "WarehouseEnvironment");
            foreach (var child in environment.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("CombatOccluder");
            EditorUtility.SetDirty(rig); EditorUtility.SetDirty(transition); EditorUtility.SetDirty(root.CameraPresenter);
        }

        private static CoverSideRig.Side CreateSide(Transform parent, JObject manifest, CoverSide side)
        {
            var record = manifest["coverSides"].Single(s => (string)s["side"] == side.ToString());
            var path = manifest["switchPaths"].Single(s => (string)s["source"] == side.ToString());
            var sideRoot = Child(parent, side.ToString());
            var player = Child(sideRoot, "PlayerAnchor"); player.position = Vector(record["playerAnchor"]);
            var hidden = Child(sideRoot, "HiddenPose");
            hidden.SetPositionAndRotation(Vector(record["hiddenPosition"]), Quaternion.Euler(Vector(record["hiddenEuler"])));
            var exposed = Child(sideRoot, "ExposedPose");
            exposed.SetPositionAndRotation(Vector(record["exposedPosition"]), Quaternion.Euler(Vector(record["exposedEuler"])));
            return new CoverSideRig.Side { side = side, playerAnchor = player, hiddenPose = hidden, exposedPose = exposed,
                path = path["controlPoints"].Select(Vector).ToArray() };
        }

        public static void ValidateAndCapture(Scene scene)
        {
            var root = Find<CombatFoundationSceneRoot>(scene);
            var transition = root.CameraPresenter.Transition;
            var view = root.CameraPresenter.GetComponent<UnityEngine.Camera>();
            var failures = new List<string>();
            transition.ConfigureMotion(root.Settings);
            transition.Validate(failures); root.Targeting.Validate(failures); root.Shots.Validate(failures);
            Physics.SyncTransforms();
            if (failures.Count == 0) root.Targeting.ValidateCoverGeometry(view, transition.Rig, failures);
            var tangent = Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
            var radius = view.nearClipPlane * Mathf.Sqrt(1f + tangent*tangent*(1f + view.aspect*view.aspect));
            transition.ValidateMotionPath(radius, failures);
            foreach (var side in new[] { CoverSide.Right, CoverSide.Left })
            {
                var data = transition.Rig.Get(side);
                for (var i = 0; i <= 128; i++)
                {
                    var peek = Vector3.Lerp(data.hiddenPose.position, data.exposedPose.position, i / 128f);
                    if (Physics.CheckSphere(peek, radius, LayerMask.GetMask("CombatOccluder"), QueryTriggerInteraction.Ignore))
                        failures.Add("Peek camera near plane blocked: " + side + " sample " + i);
                }
            }
            if (failures.Count != 0) throw new InvalidDataException(string.Join("\n", failures));
            Debug.Log("[EFTM] Both cover sides, ten hidden positions, exposure opportunities and path envelopes validated.");
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child;
            child = new GameObject(name).transform; child.SetParent(parent, false); return child;
        }
        private static T Find<T>(Scene scene) where T : Component
            => scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>(true)).SingleOrDefault();
        private static Vector3 Vector(JToken token)
        {
            if (!(token is JArray array) || array.Count != 3) throw new InvalidDataException("Expected three authored coordinates.");
            var value = new Vector3((float)array[0], (float)array[1], (float)array[2]);
            if (float.IsNaN(value.sqrMagnitude) || float.IsInfinity(value.sqrMagnitude)) throw new InvalidDataException("Nonfinite layout.");
            return value;
        }
    }
}
