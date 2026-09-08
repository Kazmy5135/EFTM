using EFTM.Combat.Foundation;
using UnityEngine;

namespace EFTM.Combat.Presentation
{
    [CreateAssetMenu(
        fileName = "CombatFoundationSettings",
        menuName = "EFTM/Combat/Foundation Settings")]
    public sealed class CombatFoundationSettings : ScriptableObject
    {
        [Header("Peek")]
        [SerializeField, Min(0.001f)] private float peekOutDurationSeconds = 0.300f;
        [SerializeField, Min(0.001f)] private float peekReturnDurationSeconds = 0.240f;
        [SerializeField, Range(0.01f, 0.5f)] private float maxTickSeconds = 0.250f;

        [Header("Intel")]
        [SerializeField, Range(0f, 1f)] private float visibilityThreshold = 0.10f;
        [SerializeField, Range(0f, 1f)] private float enemyRepositionChance = 0.25f;
        [SerializeField, Min(2)] private int enemyPositionCount = 5;

        [Header("Fire")]
        [SerializeField, Min(0.001f)] private float shotIntervalSeconds = 0.108f;
        [SerializeField, Min(1)] private int maxShotsPerTick = 3;
        [SerializeField, Min(2)] private int stableFromShot = 5;

        [Header("Aim")]
        [SerializeField, Min(0.01f)] private float aimYawLimitDegrees = 6.88f;
        [SerializeField, Min(0.01f)] private float aimPitchLimitDegrees = 8.02f;
        [SerializeField, Min(0.01f)] private float yawDegreesPerReferenceWidth = 27.50f;
        [SerializeField, Min(0.01f)] private float pitchDegreesPerReferenceHeight = 51.57f;

        [Header("Recoil")]
        [SerializeField, Min(0f)] private float baseVerticalRecoilDegrees = 1.03f;
        [SerializeField, Min(0f)] private float baseHorizontalRecoilDegrees = 0.69f;
        [SerializeField, Min(0f)] private float maxVerticalRecoilDegrees = 5.16f;
        [SerializeField, Min(0f)] private float maxHorizontalRecoilDegrees = 2.58f;
        [SerializeField, Min(0.001f)] private float climbReturnSeconds = 0.250f;
        [SerializeField, Min(0.001f)] private float stableReturnSeconds = 0.145f;

        [Header("Moving Lean")]
        [SerializeField, Range(0f, .12f)] private float coverLeanDistance = .08f;
        [SerializeField, Range(0f, .04f)] private float coverLeanDrop = .02f;
        [SerializeField, Range(0f, 7f)] private float coverLeanRollDegrees = 4f;
        [SerializeField] private AnimationCurve coverHeadCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(.15f, 1f), new Keyframe(.425f, 1f),
            new Keyframe(.75f, .4f), new Keyframe(1f, 0f));
        [SerializeField] private AnimationCurve coverRollCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(.20f, 1f), new Keyframe(.425f, 1f),
            new Keyframe(.75f, .4f), new Keyframe(1f, 0f));

        public Vector2 CoverHeadOffset(float progress)
        {
            var weight = MotionWeight(coverHeadCurve, progress);
            return new Vector2(Mathf.Clamp(coverLeanDistance, 0f, .12f) * weight,
                Mathf.Clamp(coverLeanDrop, 0f, .04f) * weight);
        }

        public float CoverRollDegrees(float progress)
            => Mathf.Clamp(coverLeanRollDegrees, 0f, 7f) * MotionWeight(coverRollCurve, progress);

        private static float MotionWeight(AnimationCurve curve, float progress)
            => progress <= 0f || progress >= 1f || curve == null ? 0f : Mathf.Clamp01(curve.Evaluate(progress));

        [Header("Runtime")]
        [SerializeField, Min(0.001f)] private float coverMoveSeconds = 0.80f;
        [SerializeField, Min(0.001f)] private float coverLandingSeconds = 0.20f;
        [SerializeField] private int randomSeed = 20260904;

        public float YawDegreesPerReferenceWidth => yawDegreesPerReferenceWidth;

        public float PitchDegreesPerReferenceHeight => pitchDegreesPerReferenceHeight;

        public int RandomSeed => randomSeed;

        public CombatFoundationConfig CreateConfig()
        {
            return new CombatFoundationConfig(
                peekOutDurationSeconds,
                peekReturnDurationSeconds,
                maxTickSeconds,
                visibilityThreshold,
                enemyRepositionChance,
                enemyPositionCount,
                shotIntervalSeconds,
                maxShotsPerTick,
                stableFromShot,
                aimYawLimitDegrees,
                aimPitchLimitDegrees,
                baseVerticalRecoilDegrees,
                baseHorizontalRecoilDegrees,
                maxVerticalRecoilDegrees,
                maxHorizontalRecoilDegrees,
                climbReturnSeconds,
                stableReturnSeconds, coverMoveSeconds, coverLandingSeconds);
        }
    }
}
