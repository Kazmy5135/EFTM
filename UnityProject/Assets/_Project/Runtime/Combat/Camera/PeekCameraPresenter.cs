using EFTM.Combat.Foundation;
using UnityEngine;

namespace EFTM.Combat.Camera
{
    [DisallowMultipleComponent]
    public sealed class PeekCameraPresenter : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform hiddenPose;
        [SerializeField] private Transform exposedPose;
        [SerializeField] private CoverTransitionPresenter transition;
        private CoverSide currentSide;
        public CoverTransitionPresenter Transition => transition;

        public bool IsConfigured => cameraTransform != null && hiddenPose != null && exposedPose != null;
        public Transform ViewTransform => cameraTransform;

        public Vector2 AimAtWorldPoint(Vector3 point)
        {
            return AimAtWorldPoint(point, currentSide);
        }

        public Vector2 AimAtWorldPoint(Vector3 point, CoverSide side)
        {
            var pose = transition == null ? exposedPose : transition.Rig.Get(side).exposedPose;
            var basis = transition == null ? pose.rotation : transition.Rig.ForwardReference;
            var local = Quaternion.Inverse(basis) * (point - pose.position);
            return new Vector2(-Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg,
                Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg);
        }

        public Ray ShotRay(float yaw, float pitch)
        {
            var pose = transition == null ? exposedPose : transition.Rig.Get(currentSide).exposedPose;
            var basis = transition == null ? pose.rotation : transition.Rig.ForwardReference;
            return new Ray(pose.position, basis * Quaternion.Euler(-pitch, -yaw, 0f) * Vector3.forward);
        }

        public void ConfigureCoverSwitch(CoverTransitionPresenter presenter) { transition = presenter; currentSide = CoverSide.Right; }

        public void Configure(Transform targetCamera, Transform hidden, Transform exposed)
        {
            cameraTransform = targetCamera;
            hiddenPose = hidden;
            exposedPose = exposed;
            ResetToHidden();
        }

        public void Apply(CombatSnapshot snapshot)
        {
            if (!IsConfigured)
            {
                return;
            }

            var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(snapshot.PeekProgress));
            currentSide = snapshot.CoverSwitch.CurrentSide;
            if (transition != null)
            {
                if (snapshot.CoverSwitch.IsSwitching)
                {
                    cameraTransform.SetPositionAndRotation(transition.CameraPose.position, transition.CameraPose.rotation);
                    return;
                }
                var side = transition.Rig.Get(currentSide);
                var direction = transition.Rig.ForwardReference * Quaternion.Euler(
                    -(snapshot.AimPitchDegrees + snapshot.RecoilPitchDegrees),
                    -(snapshot.AimYawDegrees + snapshot.RecoilYawDegrees), 0f);
                // Shared forward stores manual aim; side roll is applied around that direction,
                // so changing cover cannot mirror the retained yaw/pitch or bend a shot off the reticle.
                var roll = Mathf.DeltaAngle(0f, side.exposedPose.eulerAngles.z);
                var end = direction * Quaternion.Euler(0f, 0f, roll);
                cameraTransform.SetPositionAndRotation(Vector3.Lerp(side.hiddenPose.position, side.exposedPose.position, progress),
                    Quaternion.Slerp(side.hiddenPose.rotation, end, progress));
                return;
            }
            var position = Vector3.LerpUnclamped(hiddenPose.position, exposedPose.position, progress);
            var baseRotation = Quaternion.SlerpUnclamped(hiddenPose.rotation, exposedPose.rotation, progress);
            var aimAndRecoil = Quaternion.Euler(
                -(snapshot.AimPitchDegrees + snapshot.RecoilPitchDegrees) * progress,
                -(snapshot.AimYawDegrees + snapshot.RecoilYawDegrees) * progress,
                0f);

            cameraTransform.SetPositionAndRotation(position, baseRotation * aimAndRecoil);
        }

        public void ResetToHidden()
        {
            if (!IsConfigured)
            {
                return;
            }

            var pose = transition == null ? hiddenPose : transition.Rig.Get(currentSide).hiddenPose;
            cameraTransform.SetPositionAndRotation(pose.position, pose.rotation);
        }
    }
}
