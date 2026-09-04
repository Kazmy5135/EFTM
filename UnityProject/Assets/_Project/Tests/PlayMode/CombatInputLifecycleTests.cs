using System.Collections;
using EFTM.Combat.Camera;
using EFTM.Combat.Foundation;
using EFTM.Combat.Input;
using EFTM.Combat.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EFTM.Tests.PlayMode
{
    public sealed class CombatInputLifecycleTests
    {
        [Test]
        public void FakePeekOnlyMatchingPointerCanRelease()
        {
            var model = CreateModel();
            var controller = CreateController(model);

            Assert.That(controller.TryBeginFakePeek(11), Is.True);
            Assert.That(controller.TryEndFakePeek(12), Is.False);
            Assert.That(model.Snapshot.Mode, Is.EqualTo(PeekMode.Fake));
            Assert.That(model.Snapshot.Phase, Is.EqualTo(PeekPhase.Peeking));

            Assert.That(controller.TryEndFakePeek(11), Is.True);
            Assert.That(model.Snapshot.Phase, Is.EqualTo(PeekPhase.Returning));
        }

        [Test]
        public void SafeReleaseStopsPrearmedFireAndReturnsTrueAim()
        {
            var model = CreateModel();
            var controller = CreateController(model);

            Assert.That(controller.TryToggleTrueAim(3), Is.True);
            Assert.That(controller.TryBeginFire(4), Is.True);
            Assert.That(model.Snapshot.FireHeld, Is.True);

            controller.ReleaseAll();

            Assert.That(model.Snapshot.FireHeld, Is.False);
            Assert.That(model.Snapshot.Mode, Is.EqualTo(PeekMode.TrueAim));
            Assert.That(model.Snapshot.Phase, Is.EqualTo(PeekPhase.Returning));
            Assert.That(controller.HasFirePointer, Is.False);
        }

        [Test]
        public void FirePointerCanDragAimWhileTrueAimIsPeeking()
        {
            var model = CreateModel();
            var controller = CreateController(model);
            controller.TryToggleTrueAim(1);
            controller.TryBeginFire(2);

            Assert.That(controller.TryMoveAim(2, 100f, -100f, 1000f, 2000f), Is.True);
            Assert.That(model.Snapshot.AimYawDegrees, Is.EqualTo(-2.75f).Within(0.001f));
            Assert.That(model.Snapshot.AimPitchDegrees, Is.EqualTo(2.5785f).Within(0.001f));
        }

        [Test]
        public void PeekCameraUsesExplicitPosesAndSmoothProgress()
        {
            var presenterObject = new GameObject("Presenter");
            var cameraObject = new GameObject("Camera");
            var hiddenObject = new GameObject("Hidden");
            var exposedObject = new GameObject("Exposed");

            try
            {
                hiddenObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                exposedObject.transform.SetPositionAndRotation(
                    new Vector3(2f, 4f, 6f),
                    Quaternion.Euler(0f, 0f, 8f));

                var presenter = presenterObject.AddComponent<PeekCameraPresenter>();
                presenter.Configure(cameraObject.transform, hiddenObject.transform, exposedObject.transform);
                presenter.Apply(CreateSnapshot(0.5f));

                Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(1f, 2f, 3f)));
                Assert.That(
                    Quaternion.Angle(cameraObject.transform.rotation, Quaternion.Euler(0f, 0f, 4f)),
                    Is.LessThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(presenterObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(hiddenObject);
                Object.DestroyImmediate(exposedObject);
            }
        }

        [UnityTest]
        public IEnumerator CombatFoundationSceneStartsWithRequiredAdapters()
        {
            var operation = SceneManager.LoadSceneAsync(
                "Assets/_Project/Scenes/CombatFoundationV1.unity",
                LoadSceneMode.Additive);
            yield return operation;
            yield return null;

            var root = Object.FindObjectOfType<CombatFoundationSceneRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.IsInitialized, Is.True);
            Assert.That(root.Model.Snapshot.Phase, Is.EqualTo(PeekPhase.Hidden));

            var scene = SceneManager.GetSceneByPath("Assets/_Project/Scenes/CombatFoundationV1.unity");
            yield return SceneManager.UnloadSceneAsync(scene);
        }

        private static CombatInputController CreateController(CombatFoundationModel model)
        {
            return new CombatInputController(
                model.Execute,
                () => model.Snapshot,
                27.5f,
                51.57f);
        }

        private static CombatFoundationModel CreateModel()
        {
            return new CombatFoundationModel(
                new CombatFoundationConfig(),
                new FixedRandomSource());
        }

        private static CombatSnapshot CreateSnapshot(float progress)
        {
            return new CombatSnapshot(
                PeekMode.TrueAim,
                PeekPhase.Peeking,
                progress,
                false,
                false,
                0,
                RecoilPhase.Idle,
                0f,
                0f,
                0f,
                0f,
                0,
                new LastSeenIntel(false, false, false, -1, 0f, 0f));
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            public float Next01()
            {
                return 0.5f;
            }
        }
    }
}
