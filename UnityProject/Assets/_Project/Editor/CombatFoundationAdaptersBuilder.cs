using System;
using System.Linq;
using EFTM.Combat.Presentation;
using EFTM.Combat.Targeting;
using EFTM.Combat.Weapons;
using EFTM.Combat.Input;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;

namespace EFTM.Editor
{
    public static class CombatFoundationAdaptersBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/CombatFoundationV1.unity";
        private const string MaterialFolder = "Assets/_Project/Settings";

        public static void UpgradeForCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = UnityEngine.Object.FindObjectOfType<CombatFoundationSceneRoot>();
            Install(root);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[EFTM] U3/U4 scene adapters generated; warehouse retained.");
        }

        public static void Install(CombatFoundationSceneRoot root)
        {
            EnsureLayers();
            InstallFont(root);
            if (GameObject.Find("CombatEncounter") != null)
                throw new InvalidOperationException("CombatEncounter already exists; refusing destructive regeneration.");
            var environment = GameObject.Find("WarehouseEnvironment");
            foreach (var child in environment.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = LayerMask.NameToLayer("CombatOccluder");

            var encounter = new GameObject("CombatEncounter").transform;
            var targetMaterial = Material("CombatDummy", "Standard", new Color(.57f,.26f,.12f));
            var headMaterial = Material("CombatDummyHead", "Standard", new Color(.77f,.56f,.36f));
            var coverMaterial = Material("CombatCover", "Standard", new Color(.27f,.31f,.28f));
            var ghostMaterial = Material("CombatIntelGhost", "EFTM/IntelGhost", new Color(1f,.78f,.04f,.38f));
            var tracerMaterial = Material("CombatTracer", "Sprites/Default", new Color(1f,.83f,.3f));
            var actor = CreateDummy("Enemy", encounter, targetMaterial, headMaterial, true);
            var ghost = CreateDummy("OldIntelGhost", encounter, ghostMaterial, ghostMaterial, false);
            ghost.gameObject.SetActive(false);
            var anchor = new GameObject("AimAnchor").transform;
            anchor.SetParent(actor, false); anchor.localPosition = new Vector3(0,1.72f,-.06f);
            var points = new[] {
                new Vector3(-.42f,0,19f), new Vector3(.1f,0,20f), new Vector3(.42f,0,18.6f),
                new Vector3(.85f,0,19.6f), new Vector3(-.15f,0,14f)
            };
            var slots = new Transform[5];
            for (var i = 0; i < 5; i++)
            {
                slots[i] = new GameObject("Position" + i).transform;
                slots[i].SetParent(encounter, false); slots[i].position = points[i];
            }
            var heights = new[] { 1.56f, 1.28f, .82f };
            for (var i = 0; i < 3; i++)
                Primitive("PositionCover" + i, encounter, PrimitiveType.Cube,
                    points[i] + new Vector3(0,heights[i]/2f,-.42f), new Vector3(.46f,heights[i],.22f),
                    coverMaterial, "CombatOccluder", true);
            var samples = new[] {
                new Vector3(-.055f,1.74f,-.07f), new Vector3(.055f,1.68f,-.07f),
                new Vector3(-.20f,1.48f,0), new Vector3(.20f,1.48f,0),
                new Vector3(-.12f,1.38f,-.1f), new Vector3(.12f,1.38f,-.1f),
                new Vector3(-.12f,1.2f,-.1f), new Vector3(.12f,1.2f,-.1f),
                new Vector3(-.28f,1.32f,0), new Vector3(.28f,1.32f,0),
                new Vector3(-.28f,1.08f,0), new Vector3(.28f,1.08f,0),
                new Vector3(-.1f,.93f,0), new Vector3(.1f,.93f,0),
                new Vector3(-.12f,.72f,0), new Vector3(.12f,.72f,0),
                new Vector3(-.12f,.45f,0), new Vector3(.12f,.45f,0),
                new Vector3(-.12f,.15f,0), new Vector3(.12f,.15f,0)
            };
            var targeting = encounter.gameObject.AddComponent<CombatTargetingPresenter>();
            targeting.Configure(actor, anchor, slots, samples, ghost, LayerMask.GetMask("CombatOccluder"));
            targeting.SetPosition(0);
            var lines = new LineRenderer[4]; var impacts = new Transform[4]; var sounds = new AudioSource[4];
            for (var i = 0; i < 4; i++)
            {
                var slot = new GameObject("ShotSlot" + i);
                slot.transform.SetParent(encounter,false);
                slot.layer = LayerMask.NameToLayer("CombatPresentation");
                lines[i] = slot.AddComponent<LineRenderer>();
                lines[i].sharedMaterial = tracerMaterial;
                lines[i].useWorldSpace = true;
                lines[i].startWidth = .014f; lines[i].endWidth = .004f;
                lines[i].positionCount = 2; lines[i].enabled = false;
                sounds[i] = slot.AddComponent<AudioSource>(); sounds[i].playOnAwake = false;
                impacts[i] = Primitive("Impact" + i, encounter, PrimitiveType.Sphere, Vector3.zero,
                    Vector3.one * .055f, tracerMaterial, "CombatPresentation", false);
                impacts[i].gameObject.SetActive(false);
            }
            var shots = encounter.gameObject.AddComponent<CombatShotPresenter>();
            shots.Configure(lines, impacts, sounds, LayerMask.GetMask("CombatOccluder", "CombatTarget"));
            root.ConfigureCombatAdapters(targeting, shots);
            EditorUtility.SetDirty(root);
        }

        private static Transform CreateDummy(string name, Transform parent, Material body, Material head, bool target)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent,false);
            var layer = target ? "CombatTarget" : "CombatPresentation";
            root.gameObject.layer = LayerMask.NameToLayer(layer);
            Primitive("Head",root,PrimitiveType.Sphere,new Vector3(0,1.7f,0),new Vector3(.26f,.30f,.25f),head,layer,target);
            Primitive("Torso",root,PrimitiveType.Capsule,new Vector3(0,1.24f,0),new Vector3(.40f,.27f,.25f),body,layer,target);
            Primitive("Pelvis",root,PrimitiveType.Cube,new Vector3(0,.91f,0),new Vector3(.36f,.22f,.24f),body,layer,target);
            for (var sign = -1; sign <= 1; sign += 2)
            {
                Primitive("Arm"+sign,root,PrimitiveType.Capsule,new Vector3(sign*.27f,1.25f,0),new Vector3(.15f,.23f,.17f),body,layer,target);
                Primitive("Leg"+sign,root,PrimitiveType.Capsule,new Vector3(sign*.12f,.46f,0),new Vector3(.17f,.43f,.2f),body,layer,target);
            }
            return root;
        }

        private static Transform Primitive(string name,Transform parent,PrimitiveType type,Vector3 position,
            Vector3 scale,Material material,string layer,bool collider)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name;
            obj.transform.SetParent(parent,false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.layer = LayerMask.NameToLayer(layer); obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj.transform;
        }

        private static Material Material(string name,string shaderName,Color color)
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Missing shader " + shaderName);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material,path); }
            material.color = color; EditorUtility.SetDirty(material); return material;
        }

        private static void EnsureLayers()
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            foreach (var name in new[] { "CombatOccluder", "CombatTarget", "CombatPresentation" })
            {
                if (LayerMask.NameToLayer(name) >= 0) continue;
                var found = false;
                for (var i = 8; i < 32; i++)
                {
                    if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;
                    layers.GetArrayElementAtIndex(i).stringValue = name; found = true; break;
                }
                if (!found) throw new InvalidOperationException("No free user layer for " + name);
            }
            tags.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void UpgradeFontForCommandLine()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            InstallFont(UnityEngine.Object.FindObjectOfType<CombatFoundationSceneRoot>());
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }

        public static void InstallFont(CombatFoundationSceneRoot root)
        {
            const string path = MaterialFolder + "/CombatUIFont.asset";
            var font = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/UI/Fonts/UnitySkillsCN-Regular.ttf");
                if (source == null) throw new InvalidOperationException("Bundled OFL CJK font is missing.");
                font = FontAsset.CreateFontAsset(source);
                font.name = "CombatUIFont";
                font.isMultiAtlasTexturesEnabled = true;
                const string glyphs = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz +×·（）.。:：%/掩体后通道边缘可见真架枪返回假动作观察中探出已建立线预备开火连续按住没有敌人信息瞄最后位置可能过期";
                if (!font.TryAddCharacters(glyphs, out var missing))
                    throw new InvalidOperationException("Combat UI glyphs missing: " + missing);
                font.atlasPopulationMode = AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(font,path);
                AssetDatabase.AddObjectToAsset(font.material,font);
                foreach (var atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas,font);
            }
            const string switchGlyphs = "换到左右侧先回掩体中转向点位看通道已暂停面";
            // Static assets may have serialized characters but no lookup dictionary after reload.
            // TryAddCharacters can also return false when there is nothing new to add.
            font.ReadFontAssetDefinition();
            if (!font.HasCharacters(switchGlyphs))
            {
                try
                {
                    font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    font.TryAddCharacters(switchGlyphs, out var missing);
                    font.ReadFontAssetDefinition();
                    if (!font.HasCharacters(switchGlyphs))
                        throw new InvalidOperationException("Cover-switch UI glyphs missing: " + missing);
                    foreach (var atlas in font.atlasTextures)
                    {
                        if (!AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, font);
                        EditorUtility.SetDirty(atlas);
                    }
                }
                finally { font.atlasPopulationMode = AtlasPopulationMode.Static; EditorUtility.SetDirty(font); }
            }
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(font);
            root.GetComponent<CombatInputView>().ConfigureFont(font);
            var fontData = new SerializedObject(font);
            fontData.FindProperty("m_SourceFontFileGUID").stringValue =
                AssetDatabase.AssetPathToGUID("Assets/_Project/UI/Fonts/UnitySkillsCN-Regular.ttf");
            fontData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(root.GetComponent<CombatInputView>());
        }

        public static void BuildWindowsDevelopment()
        {
            var output = Environment.GetEnvironmentVariable("EFTM_BUILD_OUTPUT");
            if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("Set EFTM_BUILD_OUTPUT to the build exe path.");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                target = BuildTarget.StandaloneWindows64, locationPathName = output,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows development build failed.");
            var notices = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(output), "FontLicenses");
            System.IO.Directory.CreateDirectory(notices);
            System.IO.File.Copy("Assets/_Project/UI/Fonts/OFL.txt",System.IO.Path.Combine(notices,"OFL.txt"),true);
            System.IO.File.Copy("Assets/_Project/UI/Fonts/THIRD-PARTY-NOTICES.md",System.IO.Path.Combine(notices,"THIRD-PARTY-NOTICES.md"),true);
            Debug.Log("[EFTM] Windows development build succeeded: " + report.summary.totalSize + " bytes.");
        }

        public static void PreparePlayerForCommandLine()
        {
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input == null) throw new InvalidOperationException("Active input handler setting not found.");
            Debug.Log("[EFTM] Active Input Handling before repair: " + input.intValue);
            input.intValue = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();
        }
    }
}
