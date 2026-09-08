using System;
using System.Collections.Generic;
using EFTM.Combat.Camera;
using EFTM.Combat.Foundation;
using EFTM.Combat.Input;
using EFTM.Combat.Targeting;
using EFTM.Combat.Weapons;
using UnityEngine;

namespace EFTM.Combat.Presentation
{
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class CombatFoundationSceneRoot : MonoBehaviour
    {
        [SerializeField] private CombatFoundationSettings settings;
        [SerializeField] private CombatInputView inputView;
        [SerializeField] private PeekCameraPresenter cameraPresenter;
        [SerializeField] private CombatTargetingPresenter targeting;
        [SerializeField] private CombatShotPresenter shots;
        private UnityEngine.Camera viewCamera;

        private readonly List<CombatEvent> eventBuffer = new List<CombatEvent>(16);
        private CombatFoundationModel model;
        private bool initialized;

        public event Action<CombatEvent> EventRaised;

        public bool IsInitialized => initialized;

        public CombatFoundationModel Model => model;
        public CombatTargetingPresenter Targeting => targeting;
        public CombatShotPresenter Shots => shots;
        public PeekCameraPresenter CameraPresenter => cameraPresenter;

        public void ConfigureCombatAdapters(CombatTargetingPresenter targetPresenter, CombatShotPresenter shotPresenter)
        { targeting = targetPresenter; shots = shotPresenter; }

        public void Configure(
            CombatFoundationSettings foundationSettings,
            CombatInputView combatInputView,
            PeekCameraPresenter peekCameraPresenter)
        {
            settings = foundationSettings;
            inputView = combatInputView;
            cameraPresenter = peekCameraPresenter;
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying && !initialized)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            Step(Time.unscaledDeltaTime);
        }

        public void Step(float deltaSeconds)
        {
            if (!initialized) return;
            shots.Tick(deltaSeconds);
            model.Tick(deltaSeconds);
            targeting.Apply(model.Snapshot);
            cameraPresenter.Apply(model.Snapshot);
            FlushEvents();
            targeting.Observe(model, viewCamera, cameraPresenter);
            FlushEvents();
            inputView.ApplySnapshot(model.Snapshot);
            inputView.ApplyHitFeedback(shots.HitFeedbackRemaining > 0f, shots.LastHitTarget);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                EnterSafeReturn();
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                EnterSafeReturn();
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnterSafeReturn();
            shots?.ResetFeedback();
            inputView?.Unbind();
            initialized = false;
            model = null;
            eventBuffer.Clear();
        }

        private void Initialize()
        {
            var failures = new List<string>(3);
            if (settings == null)
            {
                failures.Add("CombatFoundationSettings is missing.");
            }

            if (inputView == null)
            {
                failures.Add("CombatInputView is missing.");
            }

            if (cameraPresenter == null || !cameraPresenter.IsConfigured)
            {
                failures.Add("PeekCameraPresenter or its explicit poses are missing.");
            }
            if (targeting == null) failures.Add("CombatTargetingPresenter missing.");
            else targeting.Validate(failures);
            if (shots == null) failures.Add("CombatShotPresenter missing.");
            else shots.Validate(failures);
            viewCamera = cameraPresenter == null ? null : cameraPresenter.GetComponent<UnityEngine.Camera>();
            if (viewCamera == null) failures.Add("Combat camera missing.");

            if (failures.Count > 0)
            {
                Debug.LogError("[EFTM] Combat foundation scene could not start:\n- " +
                               string.Join("\n- ", failures), this);
                enabled = false;
                return;
            }

            try
            {
                var config = settings.CreateConfig();
                if (config.EnemyPositionCount != targeting.PositionCount)
                    throw new InvalidOperationException("Configured enemy position count does not match the five scene slots.");
                model = new CombatFoundationModel(
                    config,
                    new SystemRandomSource(settings.RandomSeed));
            }
            catch (Exception exception)
            {
                Debug.LogError("[EFTM] Invalid combat foundation settings: " + exception.Message, this);
                enabled = false;
                return;
            }

            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Application.targetFrameRate = 60;

            inputView.Bind(ExecuteCommand, () => model.Snapshot, settings);
            targeting.ResetPresentation(model.Snapshot);
            shots.Prewarm();
            cameraPresenter.Apply(model.Snapshot);
            initialized = true;

            Debug.Log($"[EFTM] Combat foundation initialized with random seed {settings.RandomSeed}.", this);
        }

        public void ExecuteCommand(CombatCommand command)
        {
            model?.Execute(command);
            if (model != null)
            {
                FlushEvents();
                targeting.Apply(model.Snapshot);
                cameraPresenter.Apply(model.Snapshot);
                inputView.ApplySnapshot(model.Snapshot);
            }
        }

        private void FlushEvents()
        {
            eventBuffer.Clear();
            model.CopyPendingEventsTo(eventBuffer);
            for (var index = 0; index < eventBuffer.Count; index++)
            {
                if (eventBuffer[index].Type == CombatEventType.ShotRequested)
                    shots.Fire(eventBuffer[index], cameraPresenter, targeting.Actor != null);
                EventRaised?.Invoke(eventBuffer[index]);
            }
        }

        private void EnterSafeReturn()
        {
            if (!initialized || model == null)
            {
                return;
            }

            inputView?.ReleaseAllInput();

            var snapshot = model.Snapshot;
            if (snapshot.FireHeld)
            {
                model.Execute(new CombatCommand(CombatCommandType.FireReleased));
            }

            snapshot = model.Snapshot;
            if (snapshot.Mode == PeekMode.Fake && snapshot.Phase != PeekPhase.Returning)
            {
                model.Execute(new CombatCommand(CombatCommandType.FakePeekReleased));
            }
            else if (snapshot.Mode == PeekMode.TrueAim && snapshot.Phase != PeekPhase.Returning)
            {
                model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));
            }

            FlushEvents();
        }
    }
}
