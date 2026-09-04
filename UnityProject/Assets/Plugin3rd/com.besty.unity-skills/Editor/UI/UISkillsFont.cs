using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

namespace UnitySkills
{
    /// <summary>
    /// Forces the UnitySkills editor windows to render text with a bundled CJK font
    /// instead of the editor's shared default font.
    ///
    /// Why: on macOS, the Unity editor's default UI Toolkit text path rasterizes CJK
    /// glyphs on demand into a single *shared* dynamic font atlas. When that atlas has
    /// to grow/repack, individual glyphs can come back with a stale/blank UV rect and
    /// render as empty advances — so a handful of common characters (e.g. 局/更/卸/定)
    /// silently disappear while everything else looks fine. It is glyph-specific and
    /// stable-per-session, not a style/bold/truncation issue.
    ///
    /// Fix: build a dedicated <see cref="FontAsset"/> from our bundled, subsetted
    /// Maple Mono CN (OFL 1.1) TTF. A dedicated FontAsset gets its OWN multi-atlas
    /// sized for just this panel's glyphs, so it never hits the shared-atlas
    /// contention that triggers the drop. Assigned to the window root via
    /// <c>unityFontDefinition</c>, which is an inherited property, so every label in
    /// the window picks it up. Glyphs the font lacks (emoji, rare Han) still fall back
    /// to the editor defaults.
    /// </summary>
    [InitializeOnLoad]
    internal static class UISkillsFont
    {
        private const string TtfPath =
            "Assets/Plugin3rd/com.besty.unity-skills/Editor/UI/Fonts/UnitySkillsCN-Regular.ttf";

        private static FontAsset _cjkFont;
        private static bool _attempted;
        private static readonly List<WeakReference<VisualElement>> Roots =
            new List<WeakReference<VisualElement>>();

        static UISkillsFont()
        {
            EditorApplication.delayCall += RegisterOpenWindowsAfterReload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnEditorQuitting;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode &&
                state != PlayModeStateChange.ExitingPlayMode)
                return;

            // Dynamic multi-atlas fonts may create additional textures/materials while
            // the window is rendering. Refresh the protection immediately before Unity
            // unloads transient objects for the next mode.
            ProtectFontAssetGraph(_cjkFont);
        }

        private static FontAsset GetFontAsset()
        {
            if (_attempted) return _cjkFont;
            _attempted = true;

            try
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
                if (font == null)
                {
                    // Missing font is non-fatal: fall back to the editor default so the
                    // window still works (just with the original macOS glyph-drop quirk).
                    Debug.LogWarning($"[UnitySkills] CJK font not found, using editor default: {TtfPath}");
                    return null;
                }

                // Dynamic, multi-atlas FontAsset → own atlas, grows safely, no shared-atlas drop.
                _cjkFont = FontAsset.CreateFontAsset(font);
                if (_cjkFont != null)
                {
                    // UI Toolkit keeps generated text data across Play Mode and assembly
                    // reload boundaries. Keep this runtime graph alive and let the next
                    // managed domain adopt it from the surviving window root.
                    ProtectFontAssetGraph(_cjkFont);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UnitySkills] Failed to build CJK FontAsset: {ex.Message}");
                _cjkFont = null;
            }

            return _cjkFont;
        }

        /// <summary>
        /// Apply the bundled CJK font to a window's root element. Safe to call on every
        /// window; the FontAsset is built once and shared. No-op if the font is missing.
        /// </summary>
        public static void Apply(VisualElement root)
        {
            if (root == null) return;

            RegisterRoot(root);

            var fa = GetFontAsset();
            if (fa == null) return;
            root.style.unityFontDefinition = new StyleFontDefinition(fa);
        }

        private static void RegisterRoot(VisualElement root)
        {
            for (int i = Roots.Count - 1; i >= 0; i--)
            {
                if (!Roots[i].TryGetTarget(out var existing) || existing == null)
                {
                    Roots.RemoveAt(i);
                    continue;
                }

                if (ReferenceEquals(existing, root)) return;
            }

            Roots.Add(new WeakReference<VisualElement>(root));
        }

        private static void RegisterOpenWindowsAfterReload()
        {
            EditorApplication.delayCall -= RegisterOpenWindowsAfterReload;

            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            var openRoots = new List<VisualElement>();
            for (int i = 0; i < windows.Length; i++)
            {
                var window = windows[i];
                if (window is UnitySkillsWindow ||
                    window is UnitySkillsAuditWindow ||
                    window is AllowlistPickerWindow)
                {
                    var root = window.rootVisualElement;
                    openRoots.Add(root);
                    RegisterRoot(root);
                    TryAdoptFontFromRoot(root);
                }
            }

            var fa = GetFontAsset();
            if (fa == null) return;

            for (int i = 0; i < openRoots.Count; i++)
            {
                var root = openRoots[i];
                root.style.unityFontDefinition = new StyleFontDefinition(fa);
                root.MarkDirtyRepaint();
            }
        }

        private static void TryAdoptFontFromRoot(VisualElement root)
        {
            if (_cjkFont != null || root == null) return;

            var existing = root.style.unityFontDefinition.value.fontAsset;
            if (existing == null || existing.material == null || AssetDatabase.Contains(existing))
                return;

            _cjkFont = existing;
            _attempted = true;
            ProtectFontAssetGraph(_cjkFont);
        }

        private static void ClearFontFromRegisteredRoots()
        {
            for (int i = Roots.Count - 1; i >= 0; i--)
            {
                if (!Roots[i].TryGetTarget(out var root) || root == null)
                {
                    Roots.RemoveAt(i);
                    continue;
                }

                root.style.unityFontDefinition =
                    new StyleFontDefinition(StyleKeyword.Null);
            }
        }

        private static void OnEditorQuitting()
        {
            EditorApplication.delayCall -= RegisterOpenWindowsAfterReload;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            ClearFontFromRegisteredRoots();
            ReleaseFontAsset();
            Roots.Clear();
            EditorApplication.quitting -= OnEditorQuitting;
        }

        private static void ReleaseFontAsset()
        {
            if (_cjkFont == null)
            {
                _cjkFont = null;
                _attempted = false;
                return;
            }

            // These are runtime-only children created from the persistent source TTF.
            var material = _cjkFont.material;
            var atlasTextures = _cjkFont.atlasTextures;

            UnityEngine.Object.DestroyImmediate(_cjkFont);
            _cjkFont = null;
            _attempted = false;

            DestroyRuntimeObject(material);
            if (atlasTextures == null) return;

            for (int i = 0; i < atlasTextures.Length; i++)
                DestroyRuntimeObject(atlasTextures[i]);
        }

        private static void ProtectFontAssetGraph(FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            ProtectRuntimeObject(fontAsset);
            ProtectRuntimeObject(fontAsset.material);

            var atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures == null) return;

            for (int i = 0; i < atlasTextures.Length; i++)
                ProtectRuntimeObject(atlasTextures[i]);

            var dependencies = EditorUtility.CollectDependencies(
                new UnityEngine.Object[] { fontAsset });
            for (int i = 0; i < dependencies.Length; i++)
            {
                var dependency = dependencies[i];
                if (dependency is Material || dependency is Texture)
                    ProtectRuntimeObject(dependency);
            }
        }

        private static void ProtectRuntimeObject(UnityEngine.Object obj)
        {
            if (obj == null || AssetDatabase.Contains(obj)) return;
            obj.hideFlags = HideFlags.HideAndDontSave;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object obj)
        {
            if (obj == null || AssetDatabase.Contains(obj)) return;
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
