using System.Collections.Generic;
using EFTM.Combat.Foundation;
using UnityEngine;

namespace EFTM.Combat.Camera
{
    [DisallowMultipleComponent]
    public sealed class CoverTransitionPresenter : MonoBehaviour
    {
        [SerializeField] private CoverSideRig rig;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private LayerMask occluders;
        private bool hasApplied;
        private Vector3 lastPlayer, lastCamera;
        public CoverSideRig Rig => rig;
        public Transform PlayerRoot => playerRoot;
        public Pose CameraPose { get; private set; }
        public string Failure { get; private set; }
        public void Configure(CoverSideRig coverRig, Transform player, int mask)
        { rig = coverRig; playerRoot = player; occluders = mask; ResetMotion(); }
        public void ResetMotion() { hasApplied = false; Failure = null; }
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
                player = data.Position(state.MoveProgress);
                var target = rig.Get(state.TargetSide);
                var offset = Vector3.Lerp(data.hiddenPose.position - data.playerAnchor.position,
                    target.hiddenPose.position - target.playerAnchor.position, Mathf.SmoothStep(0f, 1f, state.MoveProgress));
                // Fixed corridor heading: no pre-turn, look-back or live/remembered target tracking.
                camera = new Pose(player + offset, rig.ForwardReference);
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
