using System.Collections.Generic;
using EFTM.Combat.Camera;
using EFTM.Combat.Foundation;
using UnityEngine;

namespace EFTM.Combat.Targeting
{
    [DisallowMultipleComponent]
    public sealed class CombatTargetingPresenter : MonoBehaviour
    {
        [SerializeField] private Transform actor;
        [SerializeField] private Transform aimAnchor;
        [SerializeField] private Transform[] positions;
        [SerializeField] private Vector3[] localSamples;
        [SerializeField] private Transform ghost;
        [SerializeField] private LayerMask occluders;
        private int appliedPosition = -1;
        public Transform Actor => actor;
        public Transform AimAnchor => aimAnchor;
        public Transform Ghost => ghost;
        public float LastVisibility { get; private set; }
        public int PositionCount => positions == null ? 0 : positions.Length;
        public int OccluderMask => occluders.value;

        public void Configure(Transform target, Transform anchor, Transform[] slots,
            Vector3[] samples, Transform intelGhost, int mask)
        {
            actor = target; aimAnchor = anchor; positions = slots;
            localSamples = samples; ghost = intelGhost; occluders = mask;
        }

        public void Validate(List<string> failures)
        {
            if (actor == null || aimAnchor == null || !aimAnchor.IsChildOf(actor))
                failures.Add("Enemy actor / aim anchor is missing or disconnected.");
            if (positions == null || positions.Length != 5) failures.Add("Exactly five enemy positions required.");
            else for (var i = 0; i < positions.Length; i++)
            {
                if (positions[i] == null) failures.Add("Enemy position " + i + " missing.");
                else for (var j = 0; j < i; j++)
                    if (positions[j] != null && (positions[i] == positions[j] ||
                        Vector3.SqrMagnitude(positions[i].position - positions[j].position) < .001f))
                        failures.Add("Enemy positions must be distinct.");
            }
            if (localSamples == null || localSamples.Length != 20) failures.Add("Visibility rig requires 20 samples.");
            if (occluders.value == 0 || occluders.value != LayerMask.GetMask("CombatOccluder"))
                failures.Add("Visibility mask must contain only CombatOccluder.");
            if (ghost == null) failures.Add("Pre-created intel ghost missing.");
            else
            {
                var renderers = ghost.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) failures.Add("Intel ghost has no renderers.");
                foreach (var renderer in renderers)
                    if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null ||
                        renderer.sharedMaterial.shader.name != "EFTM/IntelGhost" ||
                        !renderer.sharedMaterial.shader.isSupported ||
                        renderer.gameObject.layer != LayerMask.NameToLayer("CombatPresentation"))
                        failures.Add("Ghost requires supported EFTM/IntelGhost shader and presentation layer.");
                if (ghost.GetComponentsInChildren<Collider>(true).Length != 0)
                    failures.Add("Intel ghost must not have colliders.");
            }
        }

        public void ResetPresentation(CombatSnapshot snapshot)
        {
            appliedPosition = -1;
            LastVisibility = 0f;
            Apply(snapshot);
        }

        public void Apply(CombatSnapshot snapshot)
        {
            if (actor != null && appliedPosition != snapshot.CurrentEnemyPosition)
            {
                SetPosition(snapshot.CurrentEnemyPosition);
            }
            if (ghost == null) return;
            var visible = snapshot.Intel.GhostVisible && snapshot.Intel.HasIntel;
            if (visible)
            {
                var old = snapshot.Intel.WorldPose;
                ghost.SetPositionAndRotation(new Vector3(old.X, old.Y, old.Z),
                    new Quaternion(old.Qx, old.Qy, old.Qz, old.Qw));
            }
            if (ghost.gameObject.activeSelf != visible) ghost.gameObject.SetActive(visible);
        }

        // Also used by geometry tests; production position selection belongs to the model.
        public void SetPosition(int index)
        {
            actor.SetPositionAndRotation(positions[index].position, positions[index].rotation);
            appliedPosition = index;
        }

        public float EvaluateVisibility(UnityEngine.Camera view)
        {
            if (actor == null || view == null) return 0f;
            var visible = 0;
            for (var i = 0; i < localSamples.Length; i++)
            {
                var point = actor.TransformPoint(localSamples[i]);
                var viewport = view.WorldToViewportPoint(point);
                if (viewport.z <= view.nearClipPlane || viewport.x < 0f || viewport.x > 1f ||
                    viewport.y < 0f || viewport.y > 1f) continue;
                if (!Physics.Linecast(view.transform.position, point, occluders, QueryTriggerInteraction.Ignore)) visible++;
            }
            return visible / (float)localSamples.Length;
        }

        public void Observe(CombatFoundationModel model, UnityEngine.Camera view, PeekCameraPresenter cameraRig)
        {
            var snapshot = model.Snapshot;
            LastVisibility = 0f;
            var moving = snapshot.CoverSwitch.CanObserve;
            if ((!moving && (snapshot.CoverSwitch.InputLocked || snapshot.Mode != PeekMode.Fake ||
                snapshot.Phase == PeekPhase.Returning || snapshot.Phase == PeekPhase.Hidden)) ||
                actor == null || aimAnchor == null) return;
            LastVisibility = EvaluateVisibility(view);
            var p = actor.position; var q = actor.rotation; var a = aimAnchor.position;
            var aim = cameraRig.AimAtWorldPoint(a);
            model.ObserveEnemy(moving ? ObservationSource.CoverSwitch : ObservationSource.FakePeek,
                snapshot.CoverSwitch.ActionId, snapshot.CurrentEnemyPosition, LastVisibility, aim.x, aim.y,
                new IntelWorldPose(p.x, p.y, p.z, q.x, q.y, q.z, q.w, a.x, a.y, a.z, Time.unscaledTimeAsDouble));
        }

        public void ValidateCoverGeometry(UnityEngine.Camera view, CoverSideRig rig, List<string> failures)
        {
            var oldCamera = new Pose(view.transform.position, view.transform.rotation);
            var oldActor = new Pose(actor.position, actor.rotation);
            var oldIndex = appliedPosition;
            var exposed = new float[2, positions.Length];
            try
            {
                for (var sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    var side = rig.Get((CoverSide)sideIndex);
                    for (var slot = 0; slot < positions.Length; slot++)
                    {
                        SetPosition(slot);
                        // Hidden must be actual near-cover occlusion, not simply offscreen.
                        for (var sample = 0; sample < localSamples.Length; sample++)
                            if (!Physics.Linecast(side.hiddenPose.position, actor.TransformPoint(localSamples[sample]),
                                out var hit, occluders, QueryTriggerInteraction.Ignore) ||
                                hit.collider.name != "NearCover" + side.side)
                            { failures.Add($"{side.side} hidden slot {slot} is not fully blocked by near cover."); break; }
                        view.transform.SetPositionAndRotation(side.exposedPose.position, side.exposedPose.rotation);
                        exposed[sideIndex, slot] = EvaluateVisibility(view);
                    }
                }
                for (var slot = 0; slot < positions.Length; slot++)
                    if (Mathf.Max(exposed[0, slot], exposed[1, slot]) < .1f)
                        failures.Add("Enemy slot has no 10% observation opportunity: " + slot);
            }
            finally
            {
                actor.SetPositionAndRotation(oldActor.position, oldActor.rotation); appliedPosition = oldIndex;
                view.transform.SetPositionAndRotation(oldCamera.position, oldCamera.rotation);
            }
        }
    }
}
