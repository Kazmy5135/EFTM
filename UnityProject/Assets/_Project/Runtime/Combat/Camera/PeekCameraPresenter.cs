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

        public bool IsConfigured => cameraTransform != null && hiddenPose != null && exposedPose != null;
        public Transform ViewTransform => cameraTransform;

        public Vector2 AimAtWorldPoint(Vector3 point)
        {
            var local = Quaternion.Inverse(exposedPose.rotation) * (point - exposedPose.position);
            return new Vector2(-Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg,
                Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg);
        }

        public Ray ShotRay(float yaw, float pitch)
        {
            return new Ray(exposedPose.position,
                exposedPose.rotation * Quaternion.Euler(-pitch, -yaw, 0f) * Vector3.forward);
        }

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

            cameraTransform.SetPositionAndRotation(hiddenPose.position, hiddenPose.rotation);
        }
    }
}
