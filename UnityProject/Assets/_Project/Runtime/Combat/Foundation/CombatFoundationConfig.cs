using System;

namespace EFTM.Combat.Foundation
{
    /// <summary>
    /// Immutable runtime values for the combat-foundation rules model.
    /// Scene assets convert their serialized values into this validated snapshot.
    /// </summary>
    public sealed class CombatFoundationConfig
    {
        public CombatFoundationConfig(
            float peekOutDurationSeconds = 0.300f,
            float peekReturnDurationSeconds = 0.240f,
            float maxTickSeconds = 0.250f,
            float visibilityThreshold = 0.10f,
            float enemyRepositionChance = 0.25f,
            int enemyPositionCount = 5,
            float shotIntervalSeconds = 0.108f,
            int maximumShotsPerTick = 3,
            int stabilizeFromShot = 5,
            float aimYawLimitDegrees = 6.88f,
            float aimPitchLimitDegrees = 8.02f,
            float baseVerticalRecoilDegrees = 1.03f,
            float baseHorizontalRecoilDegrees = 0.69f,
            float maximumVerticalRecoilDegrees = 5.16f,
            float maximumHorizontalRecoilDegrees = 2.58f,
            float climbReturnSeconds = 0.250f,
            float stableReturnSeconds = 0.145f,
            float coverMoveSeconds = 0.80f, float coverLandingSeconds = 0.20f)
        {
            RequirePositive(peekOutDurationSeconds, nameof(peekOutDurationSeconds));
            RequirePositive(peekReturnDurationSeconds, nameof(peekReturnDurationSeconds));
            RequirePositive(maxTickSeconds, nameof(maxTickSeconds));
            RequireProbability(visibilityThreshold, nameof(visibilityThreshold));
            RequireProbability(enemyRepositionChance, nameof(enemyRepositionChance));
            if (enemyPositionCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyPositionCount), "At least two enemy positions are required.");
            }

            RequirePositive(shotIntervalSeconds, nameof(shotIntervalSeconds));
            if (maximumShotsPerTick < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumShotsPerTick));
            }

            if (stabilizeFromShot < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(stabilizeFromShot));
            }

            RequirePositive(aimYawLimitDegrees, nameof(aimYawLimitDegrees));
            RequirePositive(aimPitchLimitDegrees, nameof(aimPitchLimitDegrees));
            RequireNonNegative(baseVerticalRecoilDegrees, nameof(baseVerticalRecoilDegrees));
            RequireNonNegative(baseHorizontalRecoilDegrees, nameof(baseHorizontalRecoilDegrees));
            RequirePositive(maximumVerticalRecoilDegrees, nameof(maximumVerticalRecoilDegrees));
            RequirePositive(maximumHorizontalRecoilDegrees, nameof(maximumHorizontalRecoilDegrees));
            RequirePositive(climbReturnSeconds, nameof(climbReturnSeconds));
            RequirePositive(stableReturnSeconds, nameof(stableReturnSeconds));
            RequirePositive(coverMoveSeconds, nameof(coverMoveSeconds));
            RequirePositive(coverLandingSeconds, nameof(coverLandingSeconds));
            if (coverLandingSeconds >= coverMoveSeconds)
                throw new ArgumentOutOfRangeException(nameof(coverLandingSeconds), "Landing is part of movement.");
            CoverMoveSeconds = coverMoveSeconds; CoverLandingSeconds = coverLandingSeconds;

            PeekOutDurationSeconds = peekOutDurationSeconds;
            PeekReturnDurationSeconds = peekReturnDurationSeconds;
            MaxTickSeconds = maxTickSeconds;
            VisibilityThreshold = visibilityThreshold;
            EnemyRepositionChance = enemyRepositionChance;
            EnemyPositionCount = enemyPositionCount;
            ShotIntervalSeconds = shotIntervalSeconds;
            MaximumShotsPerTick = maximumShotsPerTick;
            StabilizeFromShot = stabilizeFromShot;
            AimYawLimitDegrees = aimYawLimitDegrees;
            AimPitchLimitDegrees = aimPitchLimitDegrees;
            BaseVerticalRecoilDegrees = baseVerticalRecoilDegrees;
            BaseHorizontalRecoilDegrees = baseHorizontalRecoilDegrees;
            MaximumVerticalRecoilDegrees = maximumVerticalRecoilDegrees;
            MaximumHorizontalRecoilDegrees = maximumHorizontalRecoilDegrees;
            ClimbReturnSeconds = climbReturnSeconds;
            StableReturnSeconds = stableReturnSeconds;
        }

        public float PeekOutDurationSeconds { get; }

        public float PeekReturnDurationSeconds { get; }

        public float MaxTickSeconds { get; }

        public float VisibilityThreshold { get; }

        public float EnemyRepositionChance { get; }

        public int EnemyPositionCount { get; }

        public float ShotIntervalSeconds { get; }

        public int MaximumShotsPerTick { get; }

        public int StabilizeFromShot { get; }

        public float AimYawLimitDegrees { get; }

        public float AimPitchLimitDegrees { get; }

        public float BaseVerticalRecoilDegrees { get; }

        public float BaseHorizontalRecoilDegrees { get; }

        public float MaximumVerticalRecoilDegrees { get; }

        public float MaximumHorizontalRecoilDegrees { get; }

        public float ClimbReturnSeconds { get; }

        public float StableReturnSeconds { get; }
        public float CoverMoveSeconds { get; }
        public float CoverLandingSeconds { get; }

        private static void RequireProbability(float value, string parameterName)
        {
            if (!IsFinite(value) || value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Probability must be finite and between zero and one.");
            }
        }

        private static void RequirePositive(float value, string parameterName)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and greater than zero.");
            }
        }

        private static void RequireNonNegative(float value, string parameterName)
        {
            if (!IsFinite(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and non-negative.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
