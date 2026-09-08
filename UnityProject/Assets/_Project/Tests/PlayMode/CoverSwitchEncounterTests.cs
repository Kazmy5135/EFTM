using System.Collections;
using System.Linq;
using EFTM.Combat.Camera;
using EFTM.Combat.Foundation;
using EFTM.Combat.Input;
using EFTM.Combat.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace EFTM.Tests.PlayMode
{
    public sealed class CoverSwitchEncounterTests
    {
        private Scene scene;
        private CombatFoundationSceneRoot root;
        private CoverTransitionPresenter transition;
        private UnityEngine.Camera view;
        [UnitySetUp] public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/CombatFoundationV1.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/_Project/Scenes/CombatFoundationV1.unity");
            root = scene.GetRootGameObjects().Select(o => o.GetComponent<CombatFoundationSceneRoot>()).Single(c => c != null);
            Assert.That(root.IsInitialized, Is.True);
            transition = root.CameraPresenter.Transition;
            view = root.CameraPresenter.GetComponent<UnityEngine.Camera>(); view.aspect = .5f;
            root.SendMessage("OnApplicationFocus", true);
            Physics.SyncTransforms(); yield return null;
        }
        [UnityTearDown] public IEnumerator Unload()
        { if(scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene); }

        private void Switch() => root.ExecuteCommand(new CombatCommand(CombatCommandType.SwitchCoverRequested));
        private void Finish(int hz = 60) { for(var i=0;i<hz;i++) root.Step(1f/hz); }

        [TestCase(30)] [TestCase(60)]
        public void BothDirectionsMoveImmediatelyFaceCorridorAndLandHidden(int hz)
        {
            for(var direction=0;direction<2;direction++)
            {
                var source = root.Model.Snapshot.CoverSwitch.CurrentSide;
                var sourcePosition = transition.PlayerRoot.position;
                var rotation = view.transform.rotation;
                Switch();
                Assert.That(root.Model.Snapshot.CoverSwitch.Phase, Is.EqualTo(CoverSwitchPhase.Traversing));
                var lastPosition = sourcePosition;
                var lastCamera = view.transform.position;
                var travel = source == CoverSide.Right ? Vector3.left : Vector3.right;
                var maximumRoll = 0f;
                var maximumHeadOffset = 0f;
                var saw = false;
                for(var i=0;i<hz;i++)
                {
                    root.Step(1f/hz);
                    var current = transition.PlayerRoot.position;
                    if (i == 0) Assert.That(Vector3.Distance(current, sourcePosition), Is.GreaterThan(.0001f), "No stationary pre-turn delay.");
                    Assert.That(Vector3.Distance(current,lastPosition), Is.LessThan(.15f));
                    Assert.That(Vector3.Angle(view.transform.forward, rotation * Vector3.forward), Is.LessThan(.01f));
                    var roll = Mathf.DeltaAngle(0f, view.transform.eulerAngles.z);
                    Assert.That(roll * (source == CoverSide.Right ? 1 : -1), Is.InRange(-.001f, 4.001f));
                    maximumRoll = Mathf.Max(maximumRoll, Mathf.Abs(roll));
                    var head = view.transform.position - current - Vector3.up * 1.55f;
                    maximumHeadOffset = Mathf.Max(maximumHeadOffset, Vector3.Dot(head, travel));
                    Assert.That(Vector3.Dot(view.transform.position - lastCamera, travel), Is.GreaterThanOrEqualTo(-.00001f));
                    Assert.That(head.y, Is.InRange(-.0201f, .0001f));
                    lastCamera = view.transform.position;
                    Assert.That(Quaternion.Angle(transition.PlayerRoot.rotation, transition.Rig.ForwardReference), Is.LessThan(.01f));
                    lastPosition=current;
                    saw |= root.Targeting.LastVisibility >= .1f;
                    Assert.That(root.Model.Snapshot.FireHeld, Is.False);
                }
                Assert.That(saw, Is.True, "Real moving camera must see enough silhouette at both frame rates.");
                Assert.That(maximumRoll, Is.EqualTo(4f).Within(.01f));
                Assert.That(maximumHeadOffset, Is.EqualTo(.08f).Within(.001f));
                Assert.That(Quaternion.Angle(view.transform.rotation, rotation), Is.LessThan(.01f));
                Assert.That(root.Model.Snapshot.CoverSwitch.CurrentSide, Is.Not.EqualTo(source));
                Assert.That(root.Model.Snapshot.CoverSwitch.IsSwitching, Is.False);
                Assert.That(root.Model.Snapshot.Mode, Is.EqualTo(PeekMode.None));
                Assert.That(root.Targeting.EvaluateVisibility(view), Is.Zero);
                Assert.That(root.Model.Snapshot.Intel.GhostVisible, Is.True);
                Assert.That(Vector3.Distance(view.transform.position,
                    transition.Rig.Get(root.Model.Snapshot.CoverSwitch.CurrentSide).hiddenPose.position), Is.LessThan(.001f));
                Finish(); Assert.That(root.Model.Snapshot.Mode, Is.EqualTo(PeekMode.None));
            }
        }

        [Test]
        public void CameraHeadingDoesNotFollowAnyEnemyPositionDuringSwitch()
        {
            Switch(); root.Step(.2f);
            var rotation = view.transform.rotation;
            for (var slot = 0; slot < 5; slot++)
            {
                root.Targeting.SetPosition(slot); Physics.SyncTransforms();
                root.Step(0f); // Compare identical motion progress with only the enemy position changed.
                Assert.That(Quaternion.Angle(view.transform.rotation, rotation), Is.LessThan(.01f));
            }
        }

        [Test]
        public void CrossSidePreAimUsesOldWorldAnchorAndRetainsManualWorldDirection()
        {
            for(var direction=0;direction<2;direction++)
            {
                Switch(); Finish();
                var old = root.Model.Snapshot.Intel.WorldPose;
                Assert.That(root.Model.Snapshot.Intel.HasPendingSnap, Is.True);
                root.ExecuteCommand(new CombatCommand(CombatCommandType.ToggleTrueAim));
                root.Step(.2f); root.Step(.11f);
                var state = root.Model.Snapshot;
                var ray = root.CameraPresenter.ShotRay(state.AimYawDegrees,state.AimPitchDegrees);
                Assert.That(Vector3.Angle(ray.direction,new Vector3(old.AnchorX,old.AnchorY,old.AnchorZ)-ray.origin),Is.LessThan(.01f));
                Assert.That(Vector3.Angle(ray.direction,view.transform.forward), Is.LessThan(.01f));
                var signedRoll = Mathf.DeltaAngle(0,view.transform.eulerAngles.z);
                Assert.That(signedRoll * (state.CoverSwitch.CurrentSide == CoverSide.Right ? 1 : -1), Is.GreaterThan(0));
                root.ExecuteCommand(new CombatCommand(CombatCommandType.AimDelta,valueA:1));
                var manual=root.Model.Snapshot.AimYawDegrees;
                root.ExecuteCommand(new CombatCommand(CombatCommandType.ToggleTrueAim));root.Step(.25f);
                root.ExecuteCommand(new CombatCommand(CombatCommandType.ToggleTrueAim));root.Step(.2f);root.Step(.11f);
                Assert.That(root.Model.Snapshot.AimYawDegrees, Is.EqualTo(manual));
                root.ExecuteCommand(new CombatCommand(CombatCommandType.ToggleTrueAim));root.Step(.25f);
            }
        }

        [Test]
        public void FocusAndPauseLatchIndependentlyAndDoNotTeleportOrCatchUp()
        {
            Switch(); root.Step(.25f);
            var elapsed=root.Model.Snapshot.CoverSwitch.ElapsedSeconds;
            var position=transition.PlayerRoot.position;
            var cameraPosition = view.transform.position;
            var cameraRotation = view.transform.rotation;
            root.SendMessage("OnApplicationPause",true);root.SendMessage("OnApplicationFocus",false);
            root.Step(30f);
            Assert.That(transition.PlayerRoot.position, Is.EqualTo(position));
            Assert.That(view.transform.position, Is.EqualTo(cameraPosition));
            Assert.That(Quaternion.Angle(view.transform.rotation, cameraRotation), Is.LessThan(.001f));
            root.SendMessage("OnApplicationPause",false);root.Step(.25f);
            Assert.That(root.Model.Snapshot.CoverSwitch.ElapsedSeconds, Is.EqualTo(elapsed));
            root.SendMessage("OnApplicationFocus",true);root.Step(.01f);
            Assert.That(root.Model.Snapshot.CoverSwitch.ElapsedSeconds, Is.EqualTo(elapsed+.01f).Within(.00001f));
            Finish(); Assert.That(root.Model.Snapshot.CoverSwitch.CurrentSide, Is.EqualTo(CoverSide.Left));
        }

        [Test]
        public void MovingLeanCurvesLeadRollAndMatchAcrossFrameRates()
        {
            Switch();
            root.Step(.12f);
            var head = view.transform.position - transition.PlayerRoot.position;
            Assert.That(Mathf.Abs(head.x), Is.EqualTo(.08f).Within(.0001f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, view.transform.eulerAngles.z)), Is.InRange(2f, 3.99f));
            var previousCamera = Vector3.zero;
            var previousRoll = Quaternion.identity;
            for (var pass = 0; pass < 2; pass++)
            {
                var model = new CombatFoundationModel(new CombatFoundationConfig(), new FixedRandom());
                model.Execute(new CombatCommand(CombatCommandType.SwitchCoverRequested));
                var hz = pass == 0 ? 30 : 60;
                for (var i = 0; i < hz / 2; i++) model.Tick(1f / hz);
                transition.ResetMotion();
                Assert.That(transition.TryApply(model.Snapshot, .06f), Is.True);
                if (pass == 1)
                {
                    Assert.That(Vector3.Distance(previousCamera, transition.CameraPose.position), Is.LessThan(.0001f));
                    Assert.That(Quaternion.Angle(previousRoll, transition.CameraPose.rotation), Is.LessThan(.01f));
                }
                previousCamera = transition.CameraPose.position; previousRoll = transition.CameraPose.rotation;
            }
        }

        [TestCase(30)] [TestCase(60)]
        public void BothMovingLeanPathsExposeAllFiveSlotsWithoutNearPlaneClipping(int hz)
        {
            var savedPosition = view.transform.position;
            var savedRotation = view.transform.rotation;
            var corners = new Vector3[4];
            view.CalculateFrustumCorners(new Rect(0f, 0f, 1f, 1f), view.nearClipPlane, UnityEngine.Camera.MonoOrStereoscopicEye.Mono, corners);
            try
            {
                for (var slot = 0; slot < 5; slot++)
                {
                    var seen = false;
                    root.Targeting.SetPosition(slot); Physics.SyncTransforms();
                    foreach (var source in new[] { CoverSide.Right, CoverSide.Left })
                    {
                        for (var frame = 0; frame <= Mathf.CeilToInt(.8f * hz); frame++)
                        {
                            var progress = Mathf.Clamp01(frame / (.8f * hz));
                            transition.EvaluateMotion(source, progress, out var player, out var camera);
                            view.transform.SetPositionAndRotation(camera.position, camera.rotation);
                            Assert.That(transition.IsClear(player, camera.position, .06f), Is.True);
                            foreach (var corner in corners)
                                Assert.That(Physics.CheckSphere(view.transform.TransformPoint(corner), .001f,
                                    LayerMask.GetMask("CombatOccluder"), QueryTriggerInteraction.Ignore), Is.False);
                            var visible = root.Targeting.EvaluateVisibility(view);
                            if (progress == 0f || progress == 1f) Assert.That(visible, Is.Zero);
                            seen |= visible >= .1f;
                        }
                    }
                    Assert.That(seen, Is.True, "No moving observation window for slot " + slot);
                }
            }
            finally { view.transform.SetPositionAndRotation(savedPosition, savedRotation); }
        }

        [Test]
        public void RuntimeBlockerStopsAtLastValidPoseWithoutEnemySettlement()
        {
            Switch(); root.Step(.15f);
            var before=transition.PlayerRoot.position;
            var blocker=new GameObject("TestDynamicBlocker");
            try
            {
                blocker.layer=LayerMask.NameToLayer("CombatOccluder");
                blocker.transform.position=new Vector3(.2f,.9f,-.85f);
                blocker.AddComponent<BoxCollider>().size=new Vector3(.2f,1.8f,1f);Physics.SyncTransforms();
                LogAssert.Expect(LogType.Error,"[EFTM] Cover path blocked; encounter stopped at last valid pose.");
                root.Step(.25f);
                Assert.That(transition.PlayerRoot.position,Is.EqualTo(before));
                Assert.That(root.Model.Snapshot.CoverSwitch.Suspended,Is.True);
                Assert.That(root.Model.Snapshot.CoverSwitch.CurrentSide,Is.EqualTo(CoverSide.Right));
                Assert.That(root.Model.Snapshot.CurrentEnemyPosition,Is.Zero);
            }
            finally { Object.DestroyImmediate(blocker); }
        }

        [Test]
        public void TargetLossStillCompletesSafeSwitchWithoutFreshIntel()
        {
            Object.DestroyImmediate(root.Targeting.Actor.gameObject);
            Switch();Finish();
            Assert.That(root.Model.Snapshot.CoverSwitch.CurrentSide,Is.EqualTo(CoverSide.Left));
            Assert.That(root.Model.Snapshot.Intel.GhostVisible,Is.False);
        }

        [Test]
        public void MovingPresenterAndSamplingHaveNoSteadyFrameAllocations()
        {
            Switch();root.Step(.3f);
            var model=new CombatFoundationModel(new CombatFoundationConfig(),new FixedRandom());
            model.Execute(new CombatCommand(CombatCommandType.SwitchCoverRequested));model.Tick(.25f);
            var snapshot=model.Snapshot;
            for(var i=0;i<20;i++) { transition.TryApply(snapshot,.06f);root.CameraPresenter.Apply(snapshot);root.Targeting.Observe(model,view,root.CameraPresenter); }
            var before=System.GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<200;i++) { transition.TryApply(snapshot,.06f);root.CameraPresenter.Apply(snapshot);root.Targeting.Observe(model,view,root.CameraPresenter); }
            Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }

        [UnityTest]
        public IEnumerator SwitchButtonTriggersOnReleaseAndCancelDoesNotCommit()
        {
            var ui=root.GetComponent<UIDocument>().rootVisualElement;
            var button=ui.Q<Button>("switch-cover");
            var fake=ui.Q<Button>("fake-peek");
            Assert.That(button.worldBound.yMax,Is.LessThan(fake.worldBound.yMin));
            Assert.That(button.worldBound.Overlaps(ui.Q<Button>("true-aim").worldBound),Is.False);
            Down(button);yield return null;
            Assert.That(root.Model.Snapshot.CoverSwitch.IsSwitching,Is.False);
            root.GetComponent<CombatInputView>().ReleaseAllInput();yield return null;
            Up(button);Assert.That(root.Model.Snapshot.CoverSwitch.IsSwitching,Is.False);
            Down(button);yield return null;Up(button);
            Assert.That(root.Model.Snapshot.CoverSwitch.IsSwitching,Is.True);
            Assert.That(fake.enabledSelf,Is.False);
            Assert.That(ui.Q<Button>("true-aim").enabledSelf,Is.False);
            Assert.That(root.GetComponent<CombatInputView>().Controller.TryBeginFire(99),Is.False);
            Finish();Assert.That(button.text,Is.EqualTo("换到右侧"));
        }
        private static void Down(VisualElement e)
        { using(var evt=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=0,mousePosition=e.worldBound.center})) e.SendEvent(evt); }
        private static void Up(VisualElement e)
        { using(var evt=PointerUpEvent.GetPooled(new Event{type=EventType.MouseUp,button=0,mousePosition=e.worldBound.center})) e.SendEvent(evt); }
        private sealed class FixedRandom : IRandomSource { public float Next01()=>.9f; }
    }
}
