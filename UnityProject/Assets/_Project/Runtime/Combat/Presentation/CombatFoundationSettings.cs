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

        [Header("Runtime")]
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
                stableReturnSeconds);
        }
    }
}
