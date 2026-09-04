using System;
using System.Collections.Generic;

namespace EFTM.Combat.Foundation
{
    /// <summary>
    /// Deterministic, scene-independent owner of the combat-foundation rules.
    /// Unity adapters submit commands and observations, tick time, then consume
    /// snapshots and queued events.
    /// </summary>
    public sealed class CombatFoundationModel
    {
        private readonly CombatFoundationConfig config;
        private readonly IRandomSource randomSource;
        private readonly List<CombatEvent> pendingEvents = new List<CombatEvent>(8);

        private PeekMode mode = PeekMode.None;
        private PeekPhase phase = PeekPhase.Hidden;
        private float peekProgress;

        private bool hasIntel;
        private bool hasPendingSnap;
        private bool ghostVisible;
        private bool observedThisPeek;
        private int lastSeenPosition = -1;
        private float lastSeenAimYaw;
        private float lastSeenAimPitch;
        private int currentEnemyPosition;

        private bool fireHeld;
        private int burstShotCount;
        private float shotElapsedSeconds;
        private float aimYaw;
        private float aimPitch;
        private float recoilYaw;
        private float recoilPitch;

        public CombatFoundationModel(
            CombatFoundationConfig config,
            IRandomSource randomSource,
            int initialEnemyPosition = 0)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            if (initialEnemyPosition < 0 || initialEnemyPosition >= config.EnemyPositionCount)
            {
                throw new ArgumentOutOfRangeException(nameof(initialEnemyPosition));
            }

            currentEnemyPosition = initialEnemyPosition;
        }

        public CombatSnapshot Snapshot => new CombatSnapshot(
            mode,
            phase,
            peekProgress,
            fireHeld,
            fireHeld && mode == PeekMode.TrueAim && phase != PeekPhase.Returning,
            burstShotCount,
            RecoilPhaseForShot(burstShotCount),
            aimYaw,
            aimPitch,
            recoilYaw,
            recoilPitch,
            currentEnemyPosition,
            new LastSeenIntel(
                hasIntel,
                hasPendingSnap,
                ghostVisible,
                lastSeenPosition,
                lastSeenAimYaw,
                lastSeenAimPitch));

        public void Execute(CombatCommand command)
        {
            switch (command.Type)
            {
                case CombatCommandType.ToggleTrueAim:
                    ToggleTrueAim();
                    break;
                case CombatCommandType.FakePeekPressed:
                    PressFakePeek();
                    break;
                case CombatCommandType.FakePeekReleased:
                    ReleaseFakePeek();
                    break;
                case CombatCommandType.FirePressed:
                    PressFire();
                    break;
                case CombatCommandType.FireReleased:
                    ReleaseFire();
                    break;
                case CombatCommandType.AimDelta:
                    ApplyAimDelta(command.ValueA, command.ValueB);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command));
            }
        }

        public bool ObserveEnemy(float visibilityRatio, float aimYawDegrees, float aimPitchDegrees)
        {
            if (mode != PeekMode.Fake || phase == PeekPhase.Hidden || phase == PeekPhase.Returning)
            {
                return false;
            }

            var visibility = Clamp(visibilityRatio, 0f, 1f);
            if (visibility < config.VisibilityThreshold)
            {
                return false;
            }

            var changed = !observedThisPeek || lastSeenPosition != currentEnemyPosition;
            hasIntel = true;
            hasPendingSnap = true;
            observedThisPeek = true;
            lastSeenPosition = currentEnemyPosition;
            lastSeenAimYaw = Clamp(aimYawDegrees, -config.AimYawLimitDegrees, config.AimYawLimitDegrees);
            lastSeenAimPitch = Clamp(aimPitchDegrees, -config.AimPitchLimitDegrees, config.AimPitchLimitDegrees);
            if (changed)
            {
                pendingEvents.Add(new CombatEvent(
                    CombatEventType.IntelAcquired,
                    currentEnemyPosition,
                    lastSeenAimYaw,
                    lastSeenAimPitch));
            }

            return true;
        }

        public void Tick(float deltaSeconds)
        {
            var delta = Clamp(deltaSeconds, 0f, config.MaxTickSeconds);
            var becameFullyExposed = AdvancePeek(delta);
            AdvanceRecoilReturn(delta);
            AdvanceShooting(delta, becameFullyExposed);
        }

        public int CopyPendingEventsTo(ICollection<CombatEvent> destination, bool clear = true)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            for (var index = 0; index < pendingEvents.Count; index++)
            {
                destination.Add(pendingEvents[index]);
            }

            var count = pendingEvents.Count;
            if (clear)
            {
                pendingEvents.Clear();
            }

            return count;
        }

        public void ClearPendingEvents()
        {
            pendingEvents.Clear();
        }

        public RecoilPhase RecoilPhaseForShot(int shotNumber)
        {
            if (shotNumber <= 0)
            {
                return RecoilPhase.Idle;
            }

            if (shotNumber == 1)
            {
                return RecoilPhase.Kick;
            }

            return shotNumber < config.StabilizeFromShot
                ? RecoilPhase.Climb
                : RecoilPhase.Stable;
        }

        private void ToggleTrueAim()
        {
            if (mode == PeekMode.None && phase == PeekPhase.Hidden)
            {
                StartPeek(PeekMode.TrueAim);
                if (hasPendingSnap && hasIntel)
                {
                    aimYaw = lastSeenAimYaw;
                    aimPitch = lastSeenAimPitch;
                    hasPendingSnap = false;
                    pendingEvents.Add(new CombatEvent(
                        CombatEventType.PreAimConsumed,
                        lastSeenPosition,
                        aimYaw,
                        aimPitch));
                }

                return;
            }

            if (mode == PeekMode.TrueAim && phase != PeekPhase.Returning)
            {
                BeginReturn();
            }
        }

        private void PressFakePeek()
        {
            if (mode == PeekMode.None && phase == PeekPhase.Hidden)
            {
                StartPeek(PeekMode.Fake);
                return;
            }

            if (mode == PeekMode.Fake && phase == PeekPhase.Returning && peekProgress > 0f)
            {
                phase = PeekPhase.Peeking;
            }
        }

        private void ReleaseFakePeek()
        {
            if (mode == PeekMode.Fake && phase != PeekPhase.Hidden && phase != PeekPhase.Returning)
            {
                BeginReturn();
            }
        }

        private void PressFire()
        {
            if (mode != PeekMode.TrueAim || phase == PeekPhase.Hidden || phase == PeekPhase.Returning)
            {
                return;
            }

            fireHeld = true;
        }

        private void ReleaseFire()
        {
            if (!fireHeld && burstShotCount == 0)
            {
                return;
            }

            fireHeld = false;
            EndBurst();
        }

        private void ApplyAimDelta(float yawDeltaDegrees, float pitchDeltaDegrees)
        {
            if (mode != PeekMode.TrueAim || phase == PeekPhase.Hidden || phase == PeekPhase.Returning)
            {
                return;
            }

            aimYaw = Clamp(aimYaw + yawDeltaDegrees, -config.AimYawLimitDegrees, config.AimYawLimitDegrees);
            aimPitch = Clamp(aimPitch + pitchDeltaDegrees, -config.AimPitchLimitDegrees, config.AimPitchLimitDegrees);
        }

        private void StartPeek(PeekMode requestedMode)
        {
            if (ghostVisible)
            {
                ghostVisible = false;
                pendingEvents.Add(new CombatEvent(CombatEventType.IntelGhostHidden, lastSeenPosition));
            }

            observedThisPeek = false;
            mode = requestedMode;
            phase = PeekPhase.Peeking;
            pendingEvents.Add(new CombatEvent(CombatEventType.PeekStarted, (int)requestedMode));
        }

        private void BeginReturn()
        {
            phase = PeekPhase.Returning;
            fireHeld = false;
            EndBurst();
        }

        private bool AdvancePeek(float deltaSeconds)
        {
            if (phase == PeekPhase.Peeking)
            {
                peekProgress = Clamp(
                    peekProgress + deltaSeconds / config.PeekOutDurationSeconds,
                    0f,
                    1f);
                if (peekProgress >= 1f)
                {
                    phase = PeekPhase.Holding;
                    pendingEvents.Add(new CombatEvent(CombatEventType.PeekFullyExposed));
                    return true;
                }
            }
            else if (phase == PeekPhase.Returning)
            {
                peekProgress = Clamp(
                    peekProgress - deltaSeconds / config.PeekReturnDurationSeconds,
                    0f,
                    1f);
                if (peekProgress <= 0f)
                {
                    FinishReturn();
                }
            }

            return false;
        }

        private void FinishReturn()
        {
            var returningFromFake = mode == PeekMode.Fake;
            mode = PeekMode.None;
            phase = PeekPhase.Hidden;
            peekProgress = 0f;
            fireHeld = false;
            EndBurst();
            pendingEvents.Add(new CombatEvent(CombatEventType.PeekFullyHidden));

            ResolveEnemyRelocation();

            ghostVisible = returningFromFake && observedThisPeek && hasIntel;
            observedThisPeek = false;
            if (ghostVisible)
            {
                pendingEvents.Add(new CombatEvent(CombatEventType.IntelGhostShown, lastSeenPosition));
            }
        }

        private void ResolveEnemyRelocation()
        {
            if (Clamp(randomSource.Next01(), 0f, 1f) >= config.EnemyRepositionChance)
            {
                return;
            }

            var alternatives = config.EnemyPositionCount - 1;
            var selection = (int)(Clamp(randomSource.Next01(), 0f, 0.999999f) * alternatives);
            var nextPosition = selection >= currentEnemyPosition ? selection + 1 : selection;
            currentEnemyPosition = nextPosition;
            pendingEvents.Add(new CombatEvent(CombatEventType.EnemyRelocated, nextPosition));
        }

        private void AdvanceShooting(float deltaSeconds, bool becameFullyExposed)
        {
            if (!fireHeld || mode != PeekMode.TrueAim || phase != PeekPhase.Holding)
            {
                return;
            }

            if (becameFullyExposed)
            {
                return;
            }

            if (burstShotCount == 0)
            {
                FireShot();
                return;
            }

            shotElapsedSeconds += deltaSeconds;
            var emitted = 0;
            while (shotElapsedSeconds >= config.ShotIntervalSeconds && emitted < config.MaximumShotsPerTick)
            {
                shotElapsedSeconds -= config.ShotIntervalSeconds;
                FireShot();
                emitted++;
            }
        }

        private void FireShot()
        {
            burstShotCount++;
            var recoilPhase = RecoilPhaseForShot(burstShotCount);
            var verticalMultiplier = VerticalMultiplierForShot(burstShotCount, recoilPhase);
            var horizontalMultiplier = recoilPhase == RecoilPhase.Stable ? 1.15f : 1f;
            var verticalImpulse = config.BaseVerticalRecoilDegrees * verticalMultiplier;
            var horizontalImpulse = ((Clamp(randomSource.Next01(), 0f, 1f) * 2f) - 1f)
                                    * config.BaseHorizontalRecoilDegrees
                                    * horizontalMultiplier;

            recoilPitch = Clamp(
                recoilPitch + verticalImpulse,
                0f,
                config.MaximumVerticalRecoilDegrees);
            recoilYaw = Clamp(
                recoilYaw + horizontalImpulse,
                -config.MaximumHorizontalRecoilDegrees,
                config.MaximumHorizontalRecoilDegrees);
            pendingEvents.Add(new CombatEvent(
                CombatEventType.ShotRequested,
                burstShotCount,
                verticalImpulse,
                horizontalImpulse));
        }

        private static float VerticalMultiplierForShot(int shotNumber, RecoilPhase recoilPhase)
        {
            if (recoilPhase == RecoilPhase.Stable)
            {
                return 0.52f;
            }

            switch (shotNumber)
            {
                case 1:
                    return 1f;
                case 2:
                    return 1.35f;
                case 3:
                    return 1.65f;
                default:
                    return 1.90f;
            }
        }

        private void AdvanceRecoilReturn(float deltaSeconds)
        {
            if (recoilPitch == 0f && recoilYaw == 0f)
            {
                return;
            }

            var returnSeconds = RecoilPhaseForShot(burstShotCount) == RecoilPhase.Stable
                ? config.StableReturnSeconds
                : config.ClimbReturnSeconds;
            var decay = (float)Math.Exp(-deltaSeconds / returnSeconds);
            recoilPitch *= decay;
            recoilYaw *= decay;
            if (Math.Abs(recoilPitch) < 0.0001f)
            {
                recoilPitch = 0f;
            }

            if (Math.Abs(recoilYaw) < 0.0001f)
            {
                recoilYaw = 0f;
            }
        }

        private void EndBurst()
        {
            if (burstShotCount > 0)
            {
                pendingEvents.Add(new CombatEvent(CombatEventType.BurstEnded, burstShotCount));
            }

            burstShotCount = 0;
            shotElapsedSeconds = 0f;
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (float.IsNaN(value))
            {
                return minimum;
            }

            if (value < minimum)
            {
                return minimum;
            }

            return value > maximum ? maximum : value;
        }
    }
}
