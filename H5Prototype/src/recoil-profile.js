export const RECOIL_PROFILE = Object.freeze({
  shotIntervalMs: 108,
  baseVerticalRadians: 0.018,
  climbMultipliers: Object.freeze([1, 1.35, 1.65, 1.9]),
  stableFromShot: 5,
  stableVerticalMultiplier: 0.52,
  baseHorizontalRadians: 0.012,
  climbHorizontalMultipliers: Object.freeze([0.65, 0.85, 1, 1.1]),
  stableHorizontalMultiplier: 1.15,
  climbReturnMs: 250,
  stableReturnMs: 145,
  maxVerticalRadians: 0.09,
  maxHorizontalRadians: 0.045
});

const clamp01 = (value) => Math.max(0, Math.min(1, Number(value) || 0));

export const recoilPhaseForShot = (shotNumber, profile = RECOIL_PROFILE) => {
  const shot = Math.max(1, Math.floor(Number(shotNumber) || 1));
  if (shot === 1) return "kick";
  if (shot < profile.stableFromShot) return "climb";
  return "stable";
};

export const recoilImpulseForShot = (shotNumber, randomValue = Math.random(), profile = RECOIL_PROFILE) => {
  const shot = Math.max(1, Math.floor(Number(shotNumber) || 1));
  const phase = recoilPhaseForShot(shot, profile);
  const profileIndex = Math.min(shot - 1, profile.climbMultipliers.length - 1);
  const verticalMultiplier = phase === "stable"
    ? profile.stableVerticalMultiplier
    : profile.climbMultipliers[profileIndex];
  const horizontalMultiplier = phase === "stable"
    ? profile.stableHorizontalMultiplier
    : profile.climbHorizontalMultipliers[profileIndex];
  const signedRandom = clamp01(randomValue) * 2 - 1;

  return {
    shot,
    phase,
    vertical: profile.baseVerticalRadians * verticalMultiplier,
    horizontal: profile.baseHorizontalRadians * horizontalMultiplier * signedRandom,
    returnMs: phase === "stable" ? profile.stableReturnMs : profile.climbReturnMs
  };
};
