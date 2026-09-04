using System;
using EFTM.Combat.Foundation;
using EFTM.Combat.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace EFTM.Combat.Input
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class CombatInputView : MonoBehaviour
    {
        private const int NoPointer = int.MinValue;

        private UIDocument document;
        private PanelSettings runtimePanelSettings;
        private CombatInputController controller;
        private Func<CombatSnapshot> getSnapshot;
        private VisualElement safeRoot;
        private VisualElement aimSurface;
        private Button trueAimButton;
        private Button fakePeekButton;
        private Button fireButton;
        private Label statusLabel;
        private Rect lastSafeArea;
        private int trueAimPointerId = NoPointer;

        public CombatInputController Controller => controller;

        private void Awake()
        {
            document = GetComponent<UIDocument>();
            EnsurePanelSettings();
        }

        private void OnEnable()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }

            EnsurePanelSettings();
            if (TryBuildUi() && getSnapshot != null)
            {
                ApplySnapshot(getSnapshot());
            }
        }

        public void Bind(
            Action<CombatCommand> dispatch,
            Func<CombatSnapshot> snapshotProvider,
            CombatFoundationSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            getSnapshot = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
            controller = new CombatInputController(
                dispatch,
                snapshotProvider,
                settings.YawDegreesPerReferenceWidth,
                settings.PitchDegreesPerReferenceHeight);

            if (document == null)
            {
                document = GetComponent<UIDocument>();
                EnsurePanelSettings();
            }

            if (TryBuildUi())
            {
                ApplySnapshot(getSnapshot());
            }
        }

        public void Unbind()
        {
            ReleaseAllInput();
            controller = null;
            getSnapshot = null;
        }

        public void ApplySnapshot(CombatSnapshot snapshot)
        {
            if (safeRoot == null && !TryBuildUi())
            {
                return;
            }

            UpdateSafeArea();

            var isTrueAim = snapshot.Mode == PeekMode.TrueAim && snapshot.Phase != PeekPhase.Hidden;
            var isReturning = snapshot.Phase == PeekPhase.Returning;
            fireButton.style.display = isTrueAim && !isReturning ? DisplayStyle.Flex : DisplayStyle.None;
            fireButton.text = snapshot.FireHeld
                ? "开火中"
                : snapshot.FireArmed
                    ? "已预备"
                    : "按住开火";
            fireButton.EnableInClassList("is-held", snapshot.FireHeld);

            trueAimButton.text = isTrueAim && !isReturning ? "返回掩体" : "真架枪";
            trueAimButton.EnableInClassList("is-active", isTrueAim && !isReturning);
            fakePeekButton.SetEnabled(!isTrueAim && !isReturning);

            statusLabel.text = BuildStatus(snapshot);
        }

        public void ReleaseAllInput()
        {
            trueAimPointerId = NoPointer;
            controller?.ReleaseAll();
        }

        private void OnDisable()
        {
            ReleaseAllInput();

            if (runtimePanelSettings != null)
            {
                if (document != null && document.panelSettings == runtimePanelSettings)
                {
                    document.panelSettings = null;
                }

                Destroy(runtimePanelSettings);
                runtimePanelSettings = null;
            }

            safeRoot = null;
            aimSurface = null;
            trueAimButton = null;
            fakePeekButton = null;
            fireButton = null;
            statusLabel = null;
        }

        private void EnsurePanelSettings()
        {
            if (document.panelSettings != null)
            {
                return;
            }

            runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            runtimePanelSettings.name = "CombatFoundationRuntimePanelSettings";
            runtimePanelSettings.hideFlags = HideFlags.DontSave;
            runtimePanelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            runtimePanelSettings.referenceResolution = new Vector2Int(1080, 2160);
            runtimePanelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            runtimePanelSettings.match = 0.5f;
            runtimePanelSettings.sortingOrder = 100f;
            document.panelSettings = runtimePanelSettings;
        }

        private bool TryBuildUi()
        {
            if (safeRoot != null)
            {
                return true;
            }

            var root = document.rootVisualElement;
            if (root == null)
            {
                return false;
            }

            root.Clear();
            root.style.position = Position.Absolute;
            root.style.left = 0f;
            root.style.right = 0f;
            root.style.top = 0f;
            root.style.bottom = 0f;

            safeRoot = new VisualElement { name = "combat-safe-area" };
            safeRoot.style.position = Position.Absolute;
            root.Add(safeRoot);

            aimSurface = new VisualElement { name = "aim-surface" };
            aimSurface.style.position = Position.Absolute;
            aimSurface.style.left = 0f;
            aimSurface.style.right = 0f;
            aimSurface.style.top = 0f;
            aimSurface.style.bottom = 0f;
            safeRoot.Add(aimSurface);

            statusLabel = new Label("掩体后") { name = "combat-status" };
            statusLabel.style.position = Position.Absolute;
            statusLabel.style.top = 44f;
            statusLabel.style.left = 44f;
            statusLabel.style.right = 44f;
            statusLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            statusLabel.style.fontSize = 34f;
            statusLabel.style.color = new Color(0.90f, 0.96f, 0.94f, 0.92f);
            safeRoot.Add(statusLabel);

            trueAimButton = CreateActionButton("true-aim", "真架枪", new Color(0.18f, 0.55f, 0.43f, 0.94f));
            trueAimButton.style.left = 54f;
            trueAimButton.style.bottom = 74f;
            trueAimButton.style.width = 300f;
            trueAimButton.style.height = 132f;
            safeRoot.Add(trueAimButton);

            fakePeekButton = CreateActionButton("fake-peek", "假动作", new Color(0.22f, 0.29f, 0.34f, 0.94f));
            fakePeekButton.style.right = 54f;
            fakePeekButton.style.bottom = 74f;
            fakePeekButton.style.width = 300f;
            fakePeekButton.style.height = 132f;
            safeRoot.Add(fakePeekButton);

            fireButton = CreateActionButton("fire", "按住开火", new Color(0.72f, 0.20f, 0.14f, 0.96f));
            fireButton.style.left = Length.Percent(50f);
            fireButton.style.marginLeft = -130f;
            fireButton.style.bottom = 270f;
            fireButton.style.width = 260f;
            fireButton.style.height = 260f;
            fireButton.style.borderTopLeftRadius = 130f;
            fireButton.style.borderTopRightRadius = 130f;
            fireButton.style.borderBottomLeftRadius = 130f;
            fireButton.style.borderBottomRightRadius = 130f;
            fireButton.style.display = DisplayStyle.None;
            safeRoot.Add(fireButton);

            RegisterInputCallbacks();
            UpdateSafeArea();
            return true;
        }

        private static Button CreateActionButton(string name, string text, Color background)
        {
            var button = new Button { name = name, text = text, focusable = false };
            button.style.position = Position.Absolute;
            button.style.backgroundColor = background;
            button.style.color = Color.white;
            button.style.fontSize = 31f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.borderLeftWidth = 2f;
            button.style.borderRightWidth = 2f;
            button.style.borderTopWidth = 2f;
            button.style.borderBottomWidth = 2f;
            button.style.borderLeftColor = new Color(1f, 1f, 1f, 0.34f);
            button.style.borderRightColor = new Color(1f, 1f, 1f, 0.34f);
            button.style.borderTopColor = new Color(1f, 1f, 1f, 0.34f);
            button.style.borderBottomColor = new Color(1f, 1f, 1f, 0.34f);
            button.style.borderTopLeftRadius = 24f;
            button.style.borderTopRightRadius = 24f;
            button.style.borderBottomLeftRadius = 24f;
            button.style.borderBottomRightRadius = 24f;
            return button;
        }

        private void RegisterInputCallbacks()
        {
            trueAimButton.RegisterCallback<PointerDownEvent>(OnTrueAimDown);
            trueAimButton.RegisterCallback<PointerUpEvent>(OnTrueAimUp);
            trueAimButton.RegisterCallback<PointerCancelEvent>(OnTrueAimCancel);
            trueAimButton.RegisterCallback<PointerCaptureOutEvent>(OnTrueAimCaptureOut);

            fakePeekButton.RegisterCallback<PointerDownEvent>(OnFakePeekDown);
            fakePeekButton.RegisterCallback<PointerUpEvent>(OnFakePeekUp);
            fakePeekButton.RegisterCallback<PointerCancelEvent>(OnPointerCancelled);
            fakePeekButton.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            fireButton.RegisterCallback<PointerDownEvent>(OnFireDown);
            fireButton.RegisterCallback<PointerMoveEvent>(OnAimMoved);
            fireButton.RegisterCallback<PointerUpEvent>(OnFireUp);
            fireButton.RegisterCallback<PointerCancelEvent>(OnPointerCancelled);
            fireButton.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            aimSurface.RegisterCallback<PointerDownEvent>(OnAimDown);
            aimSurface.RegisterCallback<PointerMoveEvent>(OnAimMoved);
            aimSurface.RegisterCallback<PointerUpEvent>(OnAimUp);
            aimSurface.RegisterCallback<PointerCancelEvent>(OnPointerCancelled);
            aimSurface.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnTrueAimDown(PointerDownEvent evt)
        {
            if (trueAimPointerId != NoPointer)
            {
                return;
            }

            trueAimPointerId = evt.pointerId;
            trueAimButton.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnTrueAimUp(PointerUpEvent evt)
        {
            if (evt.pointerId != trueAimPointerId)
            {
                return;
            }

            trueAimPointerId = NoPointer;
            controller?.TryToggleTrueAim(evt.pointerId);
            ReleasePointerIfCaptured(trueAimButton, evt.pointerId);
            evt.StopPropagation();
        }

        private void OnTrueAimCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId == trueAimPointerId)
            {
                trueAimPointerId = NoPointer;
            }
        }

        private void OnTrueAimCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId == trueAimPointerId)
            {
                trueAimPointerId = NoPointer;
            }
        }

        private void OnFakePeekDown(PointerDownEvent evt)
        {
            if (controller != null && controller.TryBeginFakePeek(evt.pointerId))
            {
                fakePeekButton.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }
        }

        private void OnFakePeekUp(PointerUpEvent evt)
        {
            if (controller != null && controller.TryEndFakePeek(evt.pointerId))
            {
                ReleasePointerIfCaptured(fakePeekButton, evt.pointerId);
                evt.StopPropagation();
            }
        }

        private void OnFireDown(PointerDownEvent evt)
        {
            if (controller != null && controller.TryBeginFire(evt.pointerId))
            {
                fireButton.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }
        }

        private void OnFireUp(PointerUpEvent evt)
        {
            if (controller != null && controller.TryEndFire(evt.pointerId))
            {
                ReleasePointerIfCaptured(fireButton, evt.pointerId);
                evt.StopPropagation();
            }
        }

        private void OnAimDown(PointerDownEvent evt)
        {
            if (controller != null && controller.TryBeginAim(evt.pointerId))
            {
                aimSurface.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }
        }

        private void OnAimUp(PointerUpEvent evt)
        {
            if (controller != null && controller.TryEndAim(evt.pointerId))
            {
                ReleasePointerIfCaptured(aimSurface, evt.pointerId);
                evt.StopPropagation();
            }
        }

        private void OnAimMoved(PointerMoveEvent evt)
        {
            if (controller == null || safeRoot == null)
            {
                return;
            }

            var width = Mathf.Max(1f, safeRoot.resolvedStyle.width);
            var height = Mathf.Max(1f, safeRoot.resolvedStyle.height);
            if (controller.TryMoveAim(
                    evt.pointerId,
                    evt.deltaPosition.x,
                    evt.deltaPosition.y,
                    width,
                    height))
            {
                evt.StopPropagation();
            }
        }

        private void OnPointerCancelled(PointerCancelEvent evt)
        {
            controller?.CancelPointer(evt.pointerId);
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            controller?.CancelPointer(evt.pointerId);
        }

        private void UpdateSafeArea()
        {
            var safeArea = Screen.safeArea;
            if (safeArea == lastSafeArea || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            lastSafeArea = safeArea;
            safeRoot.style.left = Length.Percent(safeArea.xMin / Screen.width * 100f);
            safeRoot.style.right = Length.Percent((Screen.width - safeArea.xMax) / Screen.width * 100f);
            safeRoot.style.top = Length.Percent((Screen.height - safeArea.yMax) / Screen.height * 100f);
            safeRoot.style.bottom = Length.Percent(safeArea.yMin / Screen.height * 100f);
        }

        private static string BuildStatus(CombatSnapshot snapshot)
        {
            if (snapshot.Mode == PeekMode.TrueAim)
            {
                if (snapshot.Phase == PeekPhase.Returning)
                {
                    return "真架枪 · 返回掩体";
                }

                if (snapshot.PeekProgress < 1f)
                {
                    return snapshot.FireHeld ? "真架枪 · 已预备开火" : "真架枪 · 探出中";
                }

                return snapshot.FireHeld ? "真架枪 · 连续开火" : "真架枪 · 已建立枪线";
            }

            if (snapshot.Mode == PeekMode.Fake)
            {
                return snapshot.Phase == PeekPhase.Returning
                    ? "假动作 · 返回掩体"
                    : "假动作 · 观察中";
            }

            return "掩体后 · 通道边缘可见";
        }

        private static void ReleasePointerIfCaptured(VisualElement element, int pointerId)
        {
            if (element.HasPointerCapture(pointerId))
            {
                element.ReleasePointer(pointerId);
            }
        }
    }
}
