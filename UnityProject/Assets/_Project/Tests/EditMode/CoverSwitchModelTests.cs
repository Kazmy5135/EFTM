using System;
using System.Collections.Generic;
using EFTM.Combat.Foundation;
using EFTM.Combat.Input;
using NUnit.Framework;

namespace EFTM.Tests.EditMode
{
    public sealed class CoverSwitchModelTests
    {
        private sealed class Random : IRandomSource
        {
            public int Calls;
            public float Value = .9f;
            public float Next01() { Calls++; return Value; }
        }
        private CombatFoundationModel model;
        private Random random;
        [SetUp] public void Setup()
        { random = new Random(); model = new CombatFoundationModel(new CombatFoundationConfig(), random); }
        private void Switch() => model.Execute(new CombatCommand(CombatCommandType.SwitchCoverRequested));
        private void ReachEnd()
        { for (var i = 0; i < 60; i++) model.Tick(1f / 60); }
        private bool Observe(float ratio, int? id = null) => model.ObserveEnemy(ObservationSource.CoverSwitch,
            id ?? model.Snapshot.CoverSwitch.ActionId, model.Snapshot.CurrentEnemyPosition, ratio, 2, 1,
            new IntelWorldPose(.4f, 0, 18, 0, 0, 0, 1, .4f, 1.7f, 18, 1));

        [TestCase(CombatCommandType.ToggleTrueAim)]
        [TestCase(CombatCommandType.FakePeekPressed)]
        public void OnlyFullyHiddenCanSwitch(CombatCommandType first)
        {
            model.Execute(new CombatCommand(first)); model.Tick(.15f);
            var action = model.Snapshot.CoverSwitch.ActionId;
            Switch();
            Assert.That(model.Snapshot.CoverSwitch.IsSwitching, Is.False);
            Assert.That(model.Snapshot.CoverSwitch.ActionId, Is.EqualTo(action));
            Assert.That(random.Calls, Is.Zero);
        }

        [TestCase(0f, CoverSwitchPhase.Traversing)]
        [TestCase(.25f, CoverSwitchPhase.Traversing)]
        [TestCase(.8f, CoverSwitchPhase.Landing)]
        [TestCase(1f, CoverSwitchPhase.Landing)]
        public void EverySwitchPhaseRejectsQueuedActionsAndFiring(float elapsed, CoverSwitchPhase phase)
        {
            Switch(); for (var i=0;i<Math.Round(elapsed*100);i++) model.Tick(.01f);
            var action = model.Snapshot.CoverSwitch.ActionId;
            foreach (CombatCommandType command in Enum.GetValues(typeof(CombatCommandType)))
                model.Execute(new CombatCommand(command, valueA: 3, valueB: 2));
            Assert.That(model.Snapshot.CoverSwitch.Phase, Is.EqualTo(phase));
            Assert.That(model.Snapshot.CoverSwitch.ActionId, Is.EqualTo(action));
            Assert.That(model.Snapshot.Mode, Is.EqualTo(PeekMode.None));
            Assert.That(model.Snapshot.FireHeld, Is.False);
            Assert.That(model.Snapshot.AimYawDegrees, Is.Zero);
            Assert.That(random.Calls, Is.Zero);
        }

        [TestCase(30)] [TestCase(60)]
        public void EndpointNeedsConfirmationThenIsHiddenWithoutAutomaticPeek(int hz)
        {
            Switch();
            var frames=0;
            while (!model.Snapshot.CoverSwitch.AwaitingArrivalConfirmation && frames++ < hz*2) model.Tick(1f/hz);
            Assert.That(frames/(float)hz, Is.EqualTo(.80f).Within(.0001f));
            Assert.That(model.Snapshot.CoverSwitch.CurrentSide, Is.EqualTo(CoverSide.Right));
            Assert.That(random.Calls, Is.Zero);
            var action=model.Snapshot.CoverSwitch.ActionId;
            Assert.That(model.ConfirmCoverArrival(action-1), Is.False);
            Assert.That(model.ConfirmCoverArrival(action), Is.True);
            Assert.That(model.ConfirmCoverArrival(action), Is.False);
            ReachEnd();
            Assert.That(random.Calls, Is.EqualTo(1));
            Assert.That(model.Snapshot.CoverSwitch.CurrentSide, Is.EqualTo(CoverSide.Left));
            Assert.That(model.Snapshot.Mode, Is.EqualTo(PeekMode.None));
            Assert.That(model.Snapshot.Phase, Is.EqualTo(PeekPhase.Hidden));
            Switch(); ReachEnd(); model.ConfirmCoverArrival(model.Snapshot.CoverSwitch.ActionId);
            Assert.That(model.Snapshot.CoverSwitch.CurrentSide, Is.EqualTo(CoverSide.Right));
        }

        [Test]
        public void ObservationRequiresCurrentActionMovingPhaseAndTenPercent()
        {
            Assert.That(Observe(1), Is.False);
            Switch();
            Assert.That(model.Snapshot.CoverSwitch.Phase, Is.EqualTo(CoverSwitchPhase.Traversing));
            model.Tick(.2f);
            Assert.That(Observe(.099f), Is.False);
            Assert.That(Observe(.1f, model.Snapshot.CoverSwitch.ActionId-1), Is.False);
            Assert.That(Observe(.1f), Is.True);
            Assert.That(model.Snapshot.Intel.Revision, Is.EqualTo(1));
            Assert.That(Observe(0), Is.False);
            Assert.That(random.Calls, Is.Zero);
            ReachEnd(); model.ConfirmCoverArrival(model.Snapshot.CoverSwitch.ActionId);
            Assert.That(model.Snapshot.Intel.GhostVisible, Is.True);
            Assert.That(Observe(1), Is.False);
            Switch(); Assert.That(model.Snapshot.Intel.GhostVisible, Is.False);
            ReachEnd(); model.ConfirmCoverArrival(model.Snapshot.CoverSwitch.ActionId);
            Assert.That(model.Snapshot.Intel.GhostVisible, Is.False, "No stale ghost on a failed new observation.");
        }

        [TestCase(.249f, 2)] [TestCase(.25f, 1)]
        public void EnemyRandomOnlyRunsOnceAtSafeArrival(float chance, int expectedCalls)
        {
            random.Value=chance; Switch(); ReachEnd();
            Assert.That(random.Calls, Is.Zero);
            var action=model.Snapshot.CoverSwitch.ActionId;
            for(var i=0;i<5;i++) model.ConfirmCoverArrival(action);
            Assert.That(random.Calls, Is.EqualTo(expectedCalls));
            Assert.That(model.Snapshot.CurrentEnemyPosition != 0, Is.EqualTo(chance<.25f));
        }

        [Test]
        public void SuspendedActionDoesNotAdvanceObserveConfirmOrReplayFire()
        {
            Switch(); model.Tick(.25f); var elapsed=model.Snapshot.CoverSwitch.ElapsedSeconds;
            model.SetSuspended(true); ReachEnd();
            Assert.That(model.Snapshot.CoverSwitch.ElapsedSeconds, Is.EqualTo(elapsed));
            Assert.That(Observe(1), Is.False);
            Assert.That(model.ConfirmCoverArrival(model.Snapshot.CoverSwitch.ActionId), Is.False);
            model.Execute(new CombatCommand(CombatCommandType.FirePressed));
            model.SetSuspended(false); ReachEnd();
            Assert.That(model.Snapshot.FireHeld, Is.False);
            Assert.That(model.ConfirmCoverArrival(model.Snapshot.CoverSwitch.ActionId), Is.True);
        }

        [Test]
        public void PreAimIsAtomicAndRejectsWrongSideRevisionNonfiniteOrOutOfRange()
        {
            Switch(); model.Tick(.25f); Observe(1); ReachEnd();
            model.ConfirmCoverArrival(model.Snapshot.CoverSwitch.ActionId);
            var revision=model.Snapshot.Intel.Revision;
            foreach(var solution in new[] { default(PreAimSolution), new PreAimSolution(CoverSide.Right,revision,1,1),
                new PreAimSolution(CoverSide.Left,revision-1,1,1), new PreAimSolution(CoverSide.Left,revision,float.NaN,1),
                new PreAimSolution(CoverSide.Left,revision,9,1) })
            {
                model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim, preAim:solution));
                Assert.That(model.Snapshot.Mode, Is.EqualTo(PeekMode.None));
                Assert.That(model.Snapshot.Intel.HasPendingSnap, Is.True);
            }
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim,
                preAim: new PreAimSolution(CoverSide.Left, revision, -3, 2)));
            Assert.That(model.Snapshot.AimYawDegrees, Is.EqualTo(-3));
            Assert.That(model.Snapshot.Intel.HasPendingSnap, Is.False);
            model.Execute(new CombatCommand(CombatCommandType.AimDelta, valueA:1));
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim)); model.Tick(.25f);
            Switch(); ReachEnd(); model.ConfirmCoverArrival(model.Snapshot.CoverSwitch.ActionId);
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));
            Assert.That(model.Snapshot.AimYawDegrees, Is.EqualTo(-2));
        }

        [Test]
        public void InputControllerUsesSameSwitchPermissionAndDoesNotCaptureIllegalPointers()
        {
            var input = new CombatInputController(model.Execute, () => model.Snapshot, 27.5f, 51.57f);
            Assert.That(input.TrySwitchCover(1), Is.True);
            Assert.That(input.TrySwitchCover(2), Is.False);
            Assert.That(input.TryToggleTrueAim(3), Is.False);
            Assert.That(input.TryBeginFakePeek(3), Is.False);
            Assert.That(input.TryBeginFire(3), Is.False);
            Assert.That(input.TryBeginAim(3), Is.False);
            input.ReleaseAll(); Assert.That(model.Snapshot.CoverSwitch.IsSwitching, Is.True);
        }

        [Test]
        public void InvalidMotionConfigurationIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CombatFoundationConfig(coverMoveSeconds:0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CombatFoundationConfig(coverLandingSeconds:.8f));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CombatFoundationConfig(coverLandingSeconds:0));
        }
    }
}
