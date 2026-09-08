using System.Collections;
using System.Collections.Generic;
using EFTM.Combat.Foundation;
using EFTM.Combat.Input;
using EFTM.Combat.Presentation;
using EFTM.Combat.Targeting;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace EFTM.Tests.PlayMode
{
    public sealed class CombatEncounterTests
    {
        private Scene scene;
        private CombatFoundationSceneRoot root;
        private UnityEngine.Camera camera;
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/CombatFoundationV1.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/_Project/Scenes/CombatFoundationV1.unity");
            foreach (var go in scene.GetRootGameObjects())
            { var candidate = go.GetComponent<CombatFoundationSceneRoot>(); if (candidate != null) root = candidate; }
            Assert.That(root.IsInitialized, Is.True);
            camera = root.CameraPresenter.GetComponent<UnityEngine.Camera>();
            camera.aspect = .5f;
            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Unload() { if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene); }

        [Test]
        public void FivePositionsAreHiddenBehindNearCoverAndOfferDifferentExposures()
        {
            var ratios = new HashSet<float>();
            for (var i=0;i<5;i++)
            {
                root.Targeting.SetPosition(i); Physics.SyncTransforms();
                root.CameraPresenter.ResetToHidden();
                Assert.That(root.Targeting.EvaluateVisibility(camera),Is.Zero,"Hidden slot " + i);
                root.CameraPresenter.Apply(ExposedSnapshot());
                var ratio = root.Targeting.EvaluateVisibility(camera);
                Debug.Log("[EFTM geometry] slot " + i + " visible=" + ratio);
                Assert.That(ratio,Is.GreaterThanOrEqualTo(.1f),"Exposed slot " + i);
                ratios.Add(ratio);
                var aim = root.CameraPresenter.AimAtWorldPoint(root.Targeting.AimAnchor.position);
                Assert.That(Mathf.Abs(aim.x), Is.LessThanOrEqualTo(6.88f));
                Assert.That(Mathf.Abs(aim.y), Is.LessThanOrEqualTo(8.02f));
            }
            Assert.That(ratios.Count,Is.GreaterThanOrEqualTo(3));
            Assert.That(ratios.Contains(1f),Is.True,"At least one full-body position.");
        }

        [Test]
        public void RealColliderSamplingDistinguishesOneAndTwoVisiblePoints()
        {
            var actor = new GameObject("ThresholdTarget");
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var view = new GameObject("ThresholdCamera").AddComponent<UnityEngine.Camera>();
            try
            {
                actor.transform.position = new Vector3(100,0,0);
                view.transform.position = new Vector3(100,0,-5);
                view.aspect=1f;
                blocker.layer = LayerMask.NameToLayer("CombatOccluder");
                blocker.transform.position = new Vector3(100.1f,0,-.02f);
                blocker.transform.localScale = new Vector3(1.8f,2f,.01f);
                var samples = new Vector3[20];
                for(var i=0;i<20;i++) samples[i]=new Vector3(-.95f+i*.1f,0,0);
                var rig=actor.AddComponent<CombatTargetingPresenter>();
                rig.Configure(actor.transform,actor.transform,null,samples,null,LayerMask.GetMask("CombatOccluder"));
                Physics.SyncTransforms();
                Assert.That(rig.EvaluateVisibility(view),Is.EqualTo(.1f));
                blocker.transform.localScale = new Vector3(2f,2f,.01f);
                Physics.SyncTransforms();
                Assert.That(rig.EvaluateVisibility(view),Is.EqualTo(.05f));
            }
            finally { Object.DestroyImmediate(actor); Object.DestroyImmediate(blocker); Object.DestroyImmediate(view.gameObject); }
        }

        [Test]
        public void OldPoseGhostAndPreAimNeverFollowRelocatedEnemy()
        {
            var model=new CombatFoundationModel(new CombatFoundationConfig(enemyRepositionChance:1f),new FixedRandom());
            root.Targeting.ResetPresentation(model.Snapshot);
            model.Execute(new CombatCommand(CombatCommandType.FakePeekPressed));
            model.Tick(.2f); model.Tick(.11f);
            root.CameraPresenter.Apply(model.Snapshot); Physics.SyncTransforms();
            root.Targeting.Observe(model,camera,root.CameraPresenter);
            Assert.That(model.Snapshot.Intel.HasPendingSnap,Is.True);
            var old=root.Targeting.Actor.position;
            var anchor=root.Targeting.AimAnchor.position;
            model.Execute(new CombatCommand(CombatCommandType.FakePeekReleased)); model.Tick(.25f);
            root.Targeting.Apply(model.Snapshot); Physics.SyncTransforms();
            Assert.That(root.Targeting.Actor.position,Is.Not.EqualTo(old));
            Assert.That(root.Targeting.Ghost.gameObject.activeSelf,Is.True);
            Assert.That(root.Targeting.Ghost.position,Is.EqualTo(old));
            Assert.That(root.Targeting.Ghost.GetComponentsInChildren<Collider>().Length,Is.Zero);
            var solution = root.CameraPresenter.AimAtWorldPoint(anchor, model.Snapshot.CoverSwitch.CurrentSide);
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim, preAim:
                new PreAimSolution(model.Snapshot.CoverSwitch.CurrentSide, model.Snapshot.Intel.Revision, solution.x, solution.y)));
            root.Targeting.Apply(model.Snapshot);
            Assert.That(root.Targeting.Ghost.gameObject.activeSelf,Is.False);
            Assert.That(model.Snapshot.Intel.HasPendingSnap,Is.False);
            var ray=root.CameraPresenter.ShotRay(model.Snapshot.AimYawDegrees,model.Snapshot.AimPitchDegrees);
            Assert.That(Vector3.Angle(ray.direction,anchor-ray.origin),Is.LessThan(.02f));
            model.Execute(new CombatCommand(CombatCommandType.AimDelta,valueA:1f));
            var manual=model.Snapshot.AimYawDegrees;
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim)); model.Tick(.25f);
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));
            Assert.That(model.Snapshot.AimYawDegrees,Is.EqualTo(manual));
        }

        [Test]
        public void ArmedFireWaitsForExposureThenHitsBeforeItsOwnRecoil()
        {
            root.ExecuteCommand(new CombatCommand(CombatCommandType.FakePeekPressed));
            root.Step(.2f); root.Step(.11f);
            Assert.That(root.Model.Snapshot.Intel.HasIntel,Is.True);
            root.ExecuteCommand(new CombatCommand(CombatCommandType.FakePeekReleased)); root.Step(.25f);
            // Keep target at the observed slot for the ray test, independent of random relocation.
            root.ExecuteCommand(new CombatCommand(CombatCommandType.ToggleTrueAim));
            var fire=root.GetComponent<UIDocument>().rootVisualElement.Q<Button>("fire");
            Assert.That(fire.style.display.value,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(fire.enabledSelf,Is.True);
            root.ExecuteCommand(new CombatCommand(CombatCommandType.FirePressed));
            root.Step(.15f); Assert.That(root.Shots.ShotCount,Is.Zero);
            root.Step(.16f); Assert.That(root.Shots.ShotCount,Is.Zero);
            root.Targeting.SetPosition(root.Model.Snapshot.Intel.PositionIndex); Physics.SyncTransforms();
            var before=root.CameraPresenter.ViewTransform.forward;
            root.Step(.001f);
            Assert.That(root.Shots.ShotCount,Is.EqualTo(1));
            Assert.That(root.Shots.LastHitTarget,Is.True);
            Assert.That(root.CameraPresenter.ViewTransform.forward.y,Is.GreaterThan(before.y));
            var controller=root.GetComponent<CombatInputView>().Controller;
            controller.TryBeginAim(42); controller.TryMoveAim(42,0,100,1080,2160);
            Assert.That(root.CameraPresenter.ViewTransform.forward.y,Is.LessThan(before.y));
            controller.ReleaseAll(); root.Step(.2f);
            Assert.That(root.Shots.ShotCount,Is.EqualTo(1));
        }

        [Test]
        public void ShotPoolAndVisibilityHotPathsDoNotAllocateManagedObjects()
        {
            root.CameraPresenter.Apply(ExposedSnapshot());
            var shot=new CombatEvent(CombatEventType.ShotRequested);
            for(var i=0;i<20;i++) { root.Targeting.EvaluateVisibility(camera); root.Shots.Fire(shot,root.CameraPresenter,true); root.Shots.Tick(.108f); }
            var before=System.GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<200;i++) { root.Targeting.EvaluateVisibility(camera); root.Shots.Fire(shot,root.CameraPresenter,true); root.Shots.Tick(.108f); }
            var allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero);
            Assert.That(root.Shots.PoolCapacity,Is.EqualTo(4));
            Assert.That(root.Shots.IsPrewarmed,Is.True);
        }

        [Test]
        public void SustainedSceneTickHasNoManagedAllocationsAfterWarmup()
        {
            root.ExecuteCommand(new CombatCommand(CombatCommandType.ToggleTrueAim));
            root.ExecuteCommand(new CombatCommand(CombatCommandType.FirePressed));
            for(var i=0;i<40;i++) root.Step(.108f);
            var before=System.GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<200;i++) root.Step(.108f);
            Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }

        [UnityTest]
        public IEnumerator CapturePortraitEvidenceWhenRequested()
        {
            var output=System.Environment.GetEnvironmentVariable("EFTM_VISUAL_OUTPUT");
            if(string.IsNullOrEmpty(output)) yield break;
            System.IO.Directory.CreateDirectory(output);
            var panel=root.GetComponent<UIDocument>().panelSettings;
            var texture=new RenderTexture(1080,2160,24);
            var uiTexture=new RenderTexture(1080,2160,24);
            texture.Create();
            uiTexture.Create();
            var previousTarget=panel.targetTexture;
            var previousClear=panel.clearColor;
            camera.targetTexture=texture; panel.targetTexture=uiTexture; panel.clearColor=true;
            try
            {
                for(var stage=0;stage<3;stage++)
                {
                    if(stage==1)
                    {
                        root.ExecuteCommand(new CombatCommand(CombatCommandType.FakePeekPressed));
                        root.Step(.2f);root.Step(.11f);
                    }
                    if(stage==2)
                    {
                        root.ExecuteCommand(new CombatCommand(CombatCommandType.FakePeekReleased)); root.Step(.25f);
                    }
                    yield return null; yield return null; yield return null;
                    var old=RenderTexture.active; RenderTexture.active=texture;
                    var pixels=new Texture2D(1080,2160,TextureFormat.RGB24,false);
                    pixels.ReadPixels(new Rect(0,0,1080,2160),0,0); pixels.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,"unity-v1-"+stage+".png"),pixels.EncodeToPNG());
                    Object.Destroy(pixels); RenderTexture.active=old;
                }
                var previous=RenderTexture.active; RenderTexture.active=uiTexture;
                var uiPixels=new Texture2D(1080,2160,TextureFormat.RGBA32,false);
                uiPixels.ReadPixels(new Rect(0,0,1080,2160),0,0);uiPixels.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,"unity-v1-ui.png"),uiPixels.EncodeToPNG());
                Object.Destroy(uiPixels);RenderTexture.active=previous;
            }
            finally
            {
                camera.targetTexture=null;panel.targetTexture=previousTarget;panel.clearColor=previousClear;
                texture.Release();Object.Destroy(texture);
                uiTexture.Release();Object.Destroy(uiTexture);
            }
        }

        [Test]
        public void TargetLossSkipsShotsWithoutCorruptingHistoricalIntel()
        {
            root.ExecuteCommand(new CombatCommand(CombatCommandType.FakePeekPressed)); root.Step(.2f);root.Step(.11f);
            Assert.That(root.Model.Snapshot.Intel.HasIntel,Is.True);
            Object.DestroyImmediate(root.Targeting.Actor.gameObject);
            root.ExecuteCommand(new CombatCommand(CombatCommandType.FakePeekReleased));root.Step(.25f);
            root.ExecuteCommand(new CombatCommand(CombatCommandType.ToggleTrueAim));
            root.ExecuteCommand(new CombatCommand(CombatCommandType.FirePressed));root.Step(.2f);root.Step(.11f);root.Step(.01f);
            Assert.That(root.Shots.ShotCount,Is.Zero);
            Assert.That(root.Model.Snapshot.Intel.HasIntel,Is.True);
        }

        [UnityTest]
        public IEnumerator ActualUiPointerUpTogglesAndCancellationReleasesCapture()
        {
            var ui = root.GetComponent<UIDocument>().rootVisualElement;
            var trueButton = ui.Q<Button>("true-aim");
            Assert.That(trueButton.worldBound.width,Is.GreaterThan(100f));
            Assert.That(ui.Q("combat-safe-area").worldBound.height,Is.GreaterThan(100f));
            SendDown(trueButton);
            yield return null;
            Assert.That(root.Model.Snapshot.Mode,Is.EqualTo(PeekMode.None));
            SendUp(trueButton);
            Assert.That(root.Model.Snapshot.Mode,Is.EqualTo(PeekMode.TrueAim));
            var fire = ui.Q<Button>("fire");
            SendDown(fire);
            Assert.That(root.Model.Snapshot.FireHeld,Is.True);
            root.GetComponent<CombatInputView>().ReleaseAllInput();
            Assert.That(root.Model.Snapshot.FireHeld,Is.False);
            yield return null; // UI Toolkit applies queued capture changes at the panel update.
            Assert.That(fire.HasPointerCapture(PointerId.mousePointerId),Is.False);
            root.Step(.25f);
            var fake = ui.Q<Button>("fake-peek");
            SendDown(fake);
            Assert.That(root.Model.Snapshot.Mode,Is.EqualTo(PeekMode.Fake));
            SendUp(fake);
            Assert.That(root.Model.Snapshot.Phase,Is.EqualTo(PeekPhase.Returning));
            yield return null;
            Assert.That(fake.HasPointerCapture(PointerId.mousePointerId),Is.False);
        }

        private static void SendDown(VisualElement element)
        {
            using(var evt=PointerDownEvent.GetPooled(new Event { type=EventType.MouseDown,button=0,mousePosition=element.worldBound.center }))
                element.SendEvent(evt);
        }
        private static void SendUp(VisualElement element)
        {
            using(var evt=PointerUpEvent.GetPooled(new Event { type=EventType.MouseUp,button=0,mousePosition=element.worldBound.center }))
                element.SendEvent(evt);
        }

        private static CombatSnapshot ExposedSnapshot() => new CombatSnapshot(PeekMode.Fake,PeekPhase.Holding,1f,
            false,false,0,RecoilPhase.Idle,0,0,0,0,0,default);
        private sealed class FixedRandom:IRandomSource { public float Next01()=>.5f; }
    }
}
