namespace EFTM.Combat.Foundation
{
    public enum PeekMode
    {
        None,
        Fake,
        TrueAim
    }

    public enum PeekPhase
    {
        Hidden,
        Peeking,
        Holding,
        Returning
    }

    public enum RecoilPhase
    {
        Idle,
        Kick,
        Climb,
        Stable
    }

    public enum CombatCommandType
    {
        ToggleTrueAim,
        FakePeekPressed,
        FakePeekReleased,
        FirePressed,
        FireReleased,
        AimDelta
    }

    public readonly struct CombatCommand
    {
        public CombatCommand(CombatCommandType type, int pointerId = -1, float valueA = 0f, float valueB = 0f)
        {
            Type = type;
            PointerId = pointerId;
            ValueA = valueA;
            ValueB = valueB;
        }

        public CombatCommandType Type { get; }

        public int PointerId { get; }

        public float ValueA { get; }

        public float ValueB { get; }
    }

    public enum CombatEventType
    {
        PeekStarted,
        PeekFullyExposed,
        PeekFullyHidden,
        IntelAcquired,
        IntelGhostShown,
        IntelGhostHidden,
        PreAimConsumed,
        EnemyRelocated,
        ShotRequested,
        BurstEnded
    }

    public readonly struct CombatEvent
    {
        public CombatEvent(CombatEventType type, int intValue = 0, float valueA = 0f, float valueB = 0f,
            float shotYaw = 0f, float shotPitch = 0f)
        {
            Type = type;
            IntValue = intValue;
            ValueA = valueA;
            ValueB = valueB;
            ShotYaw = shotYaw;
            ShotPitch = shotPitch;
        }

        public CombatEventType Type { get; }
        // Pre-impulse aim for this individual shot, including previous recoil.
        public float ShotYaw { get; }
        public float ShotPitch { get; }

        public int IntValue { get; }

        public float ValueA { get; }

        public float ValueB { get; }
    }

    // Value-only world snapshot: no live Transform can leak into historical intel.
    public readonly struct IntelWorldPose
    {
        public IntelWorldPose(float x, float y, float z, float qx, float qy, float qz, float qw,
            float anchorX, float anchorY, float anchorZ, double observedAt)
        {
            X = x; Y = y; Z = z; Qx = qx; Qy = qy; Qz = qz; Qw = qw;
            AnchorX = anchorX; AnchorY = anchorY; AnchorZ = anchorZ; ObservedAt = observedAt;
        }
        public readonly float X, Y, Z, Qx, Qy, Qz, Qw, AnchorX, AnchorY, AnchorZ;
        public readonly double ObservedAt;
    }

    public readonly struct LastSeenIntel
    {
        public LastSeenIntel(
            bool hasIntel,
            bool hasPendingSnap,
            bool ghostVisible,
            int positionIndex,
            float aimYawDegrees,
            float aimPitchDegrees,
            IntelWorldPose worldPose = default)
        {
            HasIntel = hasIntel;
            HasPendingSnap = hasPendingSnap;
            GhostVisible = ghostVisible;
            PositionIndex = positionIndex;
            AimYawDegrees = aimYawDegrees;
            AimPitchDegrees = aimPitchDegrees;
            WorldPose = worldPose;
        }

        public bool HasIntel { get; }
        public IntelWorldPose WorldPose { get; }

        public bool HasPendingSnap { get; }

        public bool GhostVisible { get; }

        public int PositionIndex { get; }

        public float AimYawDegrees { get; }

        public float AimPitchDegrees { get; }
    }

    public readonly struct CombatSnapshot
    {
        public CombatSnapshot(
            PeekMode mode,
            PeekPhase phase,
            float peekProgress,
            bool fireHeld,
            bool fireArmed,
            int burstShotCount,
            RecoilPhase recoilPhase,
            float aimYawDegrees,
            float aimPitchDegrees,
            float recoilYawDegrees,
            float recoilPitchDegrees,
            int currentEnemyPosition,
            LastSeenIntel intel)
        {
            Mode = mode;
            Phase = phase;
            PeekProgress = peekProgress;
            FireHeld = fireHeld;
            FireArmed = fireArmed;
            BurstShotCount = burstShotCount;
            RecoilPhase = recoilPhase;
            AimYawDegrees = aimYawDegrees;
            AimPitchDegrees = aimPitchDegrees;
            RecoilYawDegrees = recoilYawDegrees;
            RecoilPitchDegrees = recoilPitchDegrees;
            CurrentEnemyPosition = currentEnemyPosition;
            Intel = intel;
        }

        public PeekMode Mode { get; }

        public PeekPhase Phase { get; }

        public float PeekProgress { get; }

        public bool FireHeld { get; }

        public bool FireArmed { get; }

        public int BurstShotCount { get; }

        public RecoilPhase RecoilPhase { get; }

        public float AimYawDegrees { get; }

        public float AimPitchDegrees { get; }

        public float RecoilYawDegrees { get; }

        public float RecoilPitchDegrees { get; }

        public int CurrentEnemyPosition { get; }

        public LastSeenIntel Intel { get; }
    }
}
