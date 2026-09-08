using System.Collections.Generic;
using EFTM.Combat.Foundation;
using EFTM.Combat.Presentation;
using UnityEngine;

namespace EFTM.Combat.Camera
{
    [DisallowMultipleComponent]
    public sealed class CoverTransitionPresenter : MonoBehaviour
    {
        [SerializeField] private CoverSideRig rig;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private LayerMask occluders;
        private CombatFoundationSettings motionSettings;
        private bool hasApplied;
        private Vector3 lastPlayer, lastCamera;
        public CoverSideRig Rig => rig;
        public Transform PlayerRoot => playerRoot;
        public Pose CameraPose { get; private set; }
        public string Failure { get; private set; }
        public void Configure(CoverSideRig coverRig, Transform player, int mask)
        { rig = coverRig; playerRoot = player; occluders = mask; ResetMotion(); }
        public void ResetMotion() { hasApplied = false; Failure = null; }
        public void ConfigureMotion(CombatFoundationSettings settings) { motionSettings = settings; }

        // Shared by runtime, startup validation and Editor previews. Progress comes only from Foundation.
        public void EvaluateMotion(CoverSide source, float progress, out Vector3 player, out Pose camera)
        {
            var data = rig.Get(source);
            var target = rig.Get(source == CoverSide.Right ? CoverSide.Left : CoverSide.Right);
            progress = Mathf.Clamp01(progress);
            player = data.Position(progress);
            var offset = Vector3.Lerp(data.hiddenPose.position - data.playerAnchor.position,
                target.hiddenPose.position - target.playerAnchor.position, Mathf.SmoothStep(0f, 1f, progress));
            var head = motionSettings == null ? Vector2.zero : motionSettings.CoverHeadOffset(progress);
            var roll = motionSettings == null ? 0f : motionSettings.CoverRollDegrees(progress);
            var right = rig.ForwardReference * Vector3.right;
            var travelSign = Mathf.Sign(Vector3.Dot(target.playerAnchor.position - data.playerAnchor.position, right));
            var sourceRoll = Mathf.DeltaAngle(0f, (Quaternion.Inverse(rig.ForwardReference) * data.exposedPose.rotation).eulerAngles.z);
            offset += right * (travelSign * head.x) - Vector3.up * head.y;
            // Lean rolls around corridor forward; it never steers towards live or remembered targets.
            camera = new Pose(player + offset, rig.ForwardReference * Quaternion.Euler(0f, 0f, Mathf.Sign(sourceRoll) * roll));
        }

        public void ValidateMotionPath(float radius, List<string> failures)
        {
            foreach (var source in new[] { CoverSide.Right, CoverSide.Left })
            {
                EvaluateMotion(source, 0f, out var previousPlayer, out var previousCamera);
                var target = rig.Get(source == CoverSide.Right ? CoverSide.Left : CoverSide.Right);
                var travel = (target.playerAnchor.position - rig.Get(source).playerAnchor.position).normalized;
                for (var i = 0; i <= 128; i++)
                {
                    EvaluateMotion(source, i / 128f, out var player, out var camera);
                    if (!IsClear(player, camera.position, radius) ||
                        !SegmentClear(previousPlayer, player, previousCamera.position, camera.position, radius))
                    { failures.Add("Moving lean path envelope blocked: " + source + " sample " + i); break; }
                    if (Vector3.Dot(camera.position - previousCamera.position, travel) < -.00001f)
                    { failures.Add("Moving lean camera reverses along path: " + source); break; }
                    previousPlayer = player; previousCamera = camera;
                }
            }
        }
        public void Validate(List<string> failures)
        {
            if (rig == null || playerRoot == null) failures.Add("Cover rig / PlayerRoot missing.");
            else
            {
                rig.Validate(failures);
                if (rig.transform.IsChildOf(playerRoot)) failures.Add("Cover reference poses cannot move with PlayerRoot.");
            }
            if (occluders.value != LayerMask.GetMask("CombatOccluder") || occluders.value == 0)
                failures.Add("Cover movement mask must contain only CombatOccluder.");
        }

        public bool TryApply(CombatSnapshot snapshot, float nearPlaneRadius)
        {
            if (Failure != null) return false;
            var state = snapshot.CoverSwitch;
            var data = rig.Get(state.IsSwitching ? state.SourceSide : state.CurrentSide);
            var player = data.playerAnchor.position;
            var camera = new Pose(data.hiddenPose.position, data.hiddenPose.rotation);
            var body = data.playerAnchor.rotation;
            if (state.IsSwitching)
            {
                EvaluateMotion(state.SourceSide, state.MoveProgress, out player, out camera);
                body = rig.ForwardReference;
            }
            if (!IsClear(player, camera.position, nearPlaneRadius) || hasApplied &&
                !SegmentClear(lastPlayer, player, lastCamera, camera.position, nearPlaneRadius))
            {
                Failure = "Cover path blocked; encounter stopped at last valid pose.";
                return false;
            }
            playerRoot.SetPositionAndRotation(player, body);
            CameraPose = camera;
            lastPlayer = player; lastCamera = camera.position; hasApplied = true;
            return true;
        }

        public bool ArrivalMatches(CoverSwitchSnapshot snapshot, Transform camera)
        {
            var target = rig.Get(snapshot.TargetSide);
            return Vector3.Distance(playerRoot.position, target.playerAnchor.position) <= .02f &&
                Vector3.Distance(camera.position, target.hiddenPose.position) <= .02f &&
                Quaternion.Angle(camera.rotation, target.hiddenPose.rotation) <= .5f;
        }

        public bool IsClear(Vector3 player, Vector3 camera, float radius)
            => !Physics.CheckCapsule(player + Vector3.up * .27f, player + Vector3.up * 1.57f,
                   .25f, occluders, QueryTriggerInteraction.Ignore) &&
               !Physics.CheckSphere(camera, radius, occluders, QueryTriggerInteraction.Ignore);

        private bool SegmentClear(Vector3 fromPlayer, Vector3 toPlayer, Vector3 fromCamera, Vector3 toCamera, float radius)
        {
            var move = toPlayer - fromPlayer; var cameraMove = toCamera - fromCamera;
            return (move.sqrMagnitude < .00000001f || !Physics.CapsuleCast(fromPlayer + Vector3.up * .27f,
                fromPlayer + Vector3.up * 1.57f, .25f, move.normalized, move.magnitude, occluders, QueryTriggerInteraction.Ignore)) &&
                (cameraMove.sqrMagnitude < .00000001f || !Physics.SphereCast(fromCamera, radius, cameraMove.normalized,
                    out _, cameraMove.magnitude, occluders, QueryTriggerInteraction.Ignore));
        }
    }
}
