using System;
using System.Collections.Generic;
using EFTM.Combat.Foundation;
using NUnit.Framework;

namespace EFTM.Tests.EditMode
{
    public sealed class CombatFoundationModelTests
    {
        [Test]
        public void InitialSnapshot_IsFullyHiddenAndHasNoIntel()
        {
            var model = CreateModel();

            var snapshot = model.Snapshot;

            Assert.That(snapshot.Mode, Is.EqualTo(PeekMode.None));
            Assert.That(snapshot.Phase, Is.EqualTo(PeekPhase.Hidden));
            Assert.That(snapshot.PeekProgress, Is.Zero);
            Assert.That(snapshot.FireArmed, Is.False);
            Assert.That(snapshot.Intel.HasIntel, Is.False);
            Assert.That(snapshot.Intel.PositionIndex, Is.EqualTo(-1));
        }

        [Test]
        public void FakePeek_HoldsAtThreeHundredMillisecondsAndReturnsInTwoHundredForty()
        {
            var model = CreateModel();

            model.Execute(new CombatCommand(CombatCommandType.FakePeekPressed, pointerId: 7));
            AdvanceToHolding(model);

            Assert.That(model.Snapshot.Mode, Is.EqualTo(PeekMode.Fake));
            Assert.That(model.Snapshot.Phase, Is.EqualTo(PeekPhase.Holding));
            Assert.That(model.Snapshot.PeekProgress, Is.EqualTo(1f).Within(0.0001f));

            model.Execute(new CombatCommand(CombatCommandType.FakePeekReleased, pointerId: 7));
            model.Tick(0.240f);

            Assert.That(model.Snapshot.Mode, Is.EqualTo(PeekMode.None));
            Assert.That(model.Snapshot.Phase, Is.EqualTo(PeekPhase.Hidden));
            Assert.That(model.Snapshot.PeekProgress, Is.Zero);
        }

        [Test]
        public void FakePeek_RepressDuringReturnReversesWithoutTeleporting()
        {
            var model = CreateModel();
            model.Execute(new CombatCommand(CombatCommandType.FakePeekPressed));
            model.Tick(0.150f);
            model.Execute(new CombatCommand(CombatCommandType.FakePeekReleased));
            model.Tick(0.060f);
            var returningProgress = model.Snapshot.PeekProgress;

            model.Execute(new CombatCommand(CombatCommandType.FakePeekPressed));
            model.Tick(0.075f);

            Assert.That(returningProgress, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(model.Snapshot.Phase, Is.EqualTo(PeekPhase.Peeking));
            Assert.That(model.Snapshot.PeekProgress, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void TrueAim_CanArmImmediatelyButDefersFirstShotUntilAfterExposureTick()
        {
            var model = CreateModel(new[] { 0.5f });
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));
            model.Execute(new CombatCommand(CombatCommandType.FirePressed, pointerId: 3));

            Assert.That(model.Snapshot.FireArmed, Is.True);
            AdvanceToHolding(model);

            Assert.That(model.Snapshot.Phase, Is.EqualTo(PeekPhase.Holding));
            Assert.That(model.Snapshot.BurstShotCount, Is.Zero, "The exposure-complete tick must render before the first shot.");

            model.Tick(0f);

            Assert.That(model.Snapshot.BurstShotCount, Is.EqualTo(1));
            Assert.That(model.Snapshot.RecoilPhase, Is.EqualTo(RecoilPhase.Kick));
        }

        [Test]
        public void FakePeek_CannotArmOrFire()
        {
            var model = CreateModel();
            model.Execute(new CombatCommand(CombatCommandType.FakePeekPressed));
            model.Execute(new CombatCommand(CombatCommandType.FirePressed));
            AdvanceToHolding(model);
            model.Tick(0.200f);

            Assert.That(model.Snapshot.FireHeld, Is.False);
            Assert.That(model.Snapshot.BurstShotCount, Is.Zero);
            Assert.That(EventsOfType(model, CombatEventType.ShotRequested), Is.Empty);
        }

        [Test]
        public void FakePeek_AcquiresIntelAtTenPercentAndShowsGhostAfterReturn()
        {
            var model = CreateModel(new[] { 0.9f }, initialEnemyPosition: 2);
            model.Execute(new CombatCommand(CombatCommandType.FakePeekPressed));
            model.Tick(0.150f);

            Assert.That(model.ObserveEnemy(0.099f, -2f, 3f), Is.False);
            Assert.That(model.Snapshot.Intel.HasIntel, Is.False);
            Assert.That(model.ObserveEnemy(0.10f, -2f, 3f), Is.True);

            model.Execute(new CombatCommand(CombatCommandType.FakePeekReleased));
            model.Tick(0.240f);

            Assert.That(model.Snapshot.Intel.HasIntel, Is.True);
            Assert.That(model.Snapshot.Intel.HasPendingSnap, Is.True);
            Assert.That(model.Snapshot.Intel.GhostVisible, Is.True);
            Assert.That(model.Snapshot.Intel.PositionIndex, Is.EqualTo(2));
        }

        [Test]
        public void NewPeek_HidesGhostAndFailedObservationDoesNotRestoreIt()
        {
            var model = CreateModel(new[] { 0.9f, 0.9f }, initialEnemyPosition: 1);
            AcquireIntelAndReturn(model, 0.2f, 1f, 2f);
            Assert.That(model.Snapshot.Intel.GhostVisible, Is.True);

            model.Execute(new CombatCommand(CombatCommandType.FakePeekPressed));
            Assert.That(model.Snapshot.Intel.GhostVisible, Is.False);
            model.Tick(0.100f);
            Assert.That(model.ObserveEnemy(0.09f, 4f, 5f), Is.False);
            model.Execute(new CombatCommand(CombatCommandType.FakePeekReleased));
            model.Tick(0.240f);

            Assert.That(model.Snapshot.Intel.GhostVisible, Is.False);
            Assert.That(model.Snapshot.Intel.PositionIndex, Is.EqualTo(1), "Failed observation must not replace old intel.");
        }

        [Test]
        public void HiddenReturn_RepositionsOnlyBelowTwentyFivePercentAndKeepsOldIntel()
        {
            var model = CreateModel(new[] { 0.249f, 0.999f }, initialEnemyPosition: 2);
            AcquireIntelAndReturn(model, 0.3f, -3f, 1.5f);

            Assert.That(model.Snapshot.CurrentEnemyPosition, Is.EqualTo(4));
            Assert.That(model.Snapshot.Intel.PositionIndex, Is.EqualTo(2));

            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));

            Assert.That(model.Snapshot.Intel.HasPendingSnap, Is.False);
            Assert.That(model.Snapshot.AimYawDegrees, Is.EqualTo(-3f).Within(0.0001f));
            Assert.That(model.Snapshot.AimPitchDegrees, Is.EqualTo(1.5f).Within(0.0001f));
        }

        [Test]
        public void HiddenReturn_DoesNotRepositionAtExactlyTwentyFivePercent()
        {
            var model = CreateModel(new[] { 0.25f }, initialEnemyPosition: 3);
            model.Execute(new CombatCommand(CombatCommandType.FakePeekPressed));
            model.Tick(0.1f);
            model.Execute(new CombatCommand(CombatCommandType.FakePeekReleased));
            model.Tick(0.240f);

            Assert.That(model.Snapshot.CurrentEnemyPosition, Is.EqualTo(3));
            Assert.That(EventsOfType(model, CombatEventType.EnemyRelocated), Is.Empty);
        }

        [Test]
        public void TrueAimWithoutFreshIntel_PreservesManualAim()
        {
            var model = CreateModel(new[] { 0.9f });
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));
            model.Execute(new CombatCommand(CombatCommandType.AimDelta, valueA: 2.25f, valueB: -1.75f));
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));
            model.Tick(0.240f);

            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));

            Assert.That(model.Snapshot.AimYawDegrees, Is.EqualTo(2.25f).Within(0.0001f));
            Assert.That(model.Snapshot.AimPitchDegrees, Is.EqualTo(-1.75f).Within(0.0001f));
            Assert.That(EventsOfType(model, CombatEventType.PreAimConsumed), Is.Empty);
        }

        [Test]
        public void Burst_UsesKickClimbAndStablePhasesAtOneHundredEightMilliseconds()
        {
            var random = new[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
            var model = CreateModel(random);
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));
            AdvanceToHolding(model);
            model.Execute(new CombatCommand(CombatCommandType.FirePressed));
            model.Tick(0f);
            model.Tick(0.108f);
            model.Tick(0.108f);
            model.Tick(0.108f);
            model.Tick(0.108f);

            var shots = EventsOfType(model, CombatEventType.ShotRequested);

            Assert.That(shots.Count, Is.EqualTo(5));
            Assert.That(shots[0].ValueA, Is.EqualTo(1.03f).Within(0.001f));
            Assert.That(shots[1].ValueA, Is.GreaterThan(shots[0].ValueA));
            Assert.That(shots[2].ValueA, Is.GreaterThan(shots[1].ValueA));
            Assert.That(shots[3].ValueA, Is.GreaterThan(shots[2].ValueA));
            Assert.That(shots[4].ValueA, Is.LessThan(shots[0].ValueA));
            Assert.That(model.Snapshot.RecoilPhase, Is.EqualTo(RecoilPhase.Stable));
        }

        [Test]
        public void FireRelease_EndsBurstAndNextPressStartsAtKick()
        {
            var model = CreateModel(new[] { 0.5f, 0.5f });
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));
            AdvanceToHolding(model);
            model.Execute(new CombatCommand(CombatCommandType.FirePressed));
            model.Tick(0f);
            Assert.That(model.Snapshot.BurstShotCount, Is.EqualTo(1));

            model.Execute(new CombatCommand(CombatCommandType.FireReleased));
            Assert.That(model.Snapshot.BurstShotCount, Is.Zero);
            Assert.That(model.Snapshot.RecoilPhase, Is.EqualTo(RecoilPhase.Idle));

            model.Execute(new CombatCommand(CombatCommandType.FirePressed));
            model.Tick(0f);
            Assert.That(model.Snapshot.BurstShotCount, Is.EqualTo(1));
            Assert.That(model.Snapshot.RecoilPhase, Is.EqualTo(RecoilPhase.Kick));
        }

        [Test]
        public void AimDelta_IsClampedAndIgnoredOutsideTrueAim()
        {
            var model = CreateModel();
            model.Execute(new CombatCommand(CombatCommandType.AimDelta, valueA: 100f, valueB: -100f));
            Assert.That(model.Snapshot.AimYawDegrees, Is.Zero);

            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));
            model.Execute(new CombatCommand(CombatCommandType.AimDelta, valueA: 100f, valueB: -100f));

            Assert.That(model.Snapshot.AimYawDegrees, Is.EqualTo(6.88f).Within(0.001f));
            Assert.That(model.Snapshot.AimPitchDegrees, Is.EqualTo(-8.02f).Within(0.001f));
        }

        [Test]
        public void Tick_ClampsLargeFrameTimeAndKeepsProgressInRange()
        {
            var model = CreateModel();
            model.Execute(new CombatCommand(CombatCommandType.FakePeekPressed));

            model.Tick(10f);

            Assert.That(model.Snapshot.PeekProgress, Is.EqualTo(0.25f / 0.3f).Within(0.0001f));
            Assert.That(model.Snapshot.PeekProgress, Is.InRange(0f, 1f));
        }

        [Test]
        public void FixedRandomSequence_ReplaysHorizontalRecoil()
        {
            var first = CreateModel(new[] { 0.8f });
            var second = CreateModel(new[] { 0.8f });

            FireFirstShot(first);
            FireFirstShot(second);

            Assert.That(first.Snapshot.RecoilYawDegrees, Is.EqualTo(second.Snapshot.RecoilYawDegrees).Within(0.0001f));
            Assert.That(first.Snapshot.RecoilYawDegrees, Is.GreaterThan(0f));
        }

        [Test]
        public void Config_RejectsInvalidProbabilitiesAndPositionCounts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CombatFoundationConfig(visibilityThreshold: 1.1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CombatFoundationConfig(enemyRepositionChance: -0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new CombatFoundationConfig(enemyPositionCount: 1));
        }

        private static CombatFoundationModel CreateModel(
            IEnumerable<float> randomValues = null,
            int initialEnemyPosition = 0)
        {
            return new CombatFoundationModel(
                new CombatFoundationConfig(),
                new SequenceRandomSource(randomValues ?? new[] { 0.9f }),
                initialEnemyPosition);
        }

        private static void AcquireIntelAndReturn(
            CombatFoundationModel model,
            float visibility,
            float aimYaw,
            float aimPitch)
        {
            model.Execute(new CombatCommand(CombatCommandType.FakePeekPressed));
            model.Tick(0.150f);
            Assert.That(model.ObserveEnemy(visibility, aimYaw, aimPitch), Is.True);
            model.Execute(new CombatCommand(CombatCommandType.FakePeekReleased));
            model.Tick(0.240f);
        }

        private static void FireFirstShot(CombatFoundationModel model)
        {
            model.Execute(new CombatCommand(CombatCommandType.ToggleTrueAim));
            AdvanceToHolding(model);
            model.Execute(new CombatCommand(CombatCommandType.FirePressed));
            model.Tick(0f);
        }

        private static void AdvanceToHolding(CombatFoundationModel model)
        {
            model.Tick(0.150f);
            model.Tick(0.150f);
        }

        private static List<CombatEvent> EventsOfType(
            CombatFoundationModel model,
            CombatEventType eventType)
        {
            var allEvents = new List<CombatEvent>();
            model.CopyPendingEventsTo(allEvents);
            return allEvents.FindAll(item => item.Type == eventType);
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly float[] values;
            private int index;

            public SequenceRandomSource(IEnumerable<float> values)
            {
                this.values = new List<float>(values).ToArray();
                if (this.values.Length == 0)
                {
                    throw new ArgumentException("At least one random value is required.", nameof(values));
                }
            }

            public float Next01()
            {
                var selected = values[Math.Min(index, values.Length - 1)];
                index++;
                return selected;
            }
        }
    }
}
