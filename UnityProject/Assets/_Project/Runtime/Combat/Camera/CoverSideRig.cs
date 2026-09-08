using System;
using System.Collections.Generic;
using EFTM.Combat.Foundation;
using UnityEngine;

namespace EFTM.Combat.Camera
{
    [DisallowMultipleComponent]
    public sealed class CoverSideRig : MonoBehaviour
    {
        [Serializable]
        public sealed class Side
        {
            public CoverSide side;
            public Transform playerAnchor, hiddenPose, exposedPose;
            public Vector3[] path;
            [NonSerialized] private float[] lengths;
            public Vector3 Position(float progress)
            {
                if (lengths == null)
                {
                    lengths = new float[65];
                    var previous = Bezier(0f);
                    for (var i = 1; i < lengths.Length; i++)
                    {
                        var next = Bezier(i / 64f);
                        lengths[i] = lengths[i - 1] + Vector3.Distance(previous, next);
                        previous = next;
                    }
                }
                var distance = Mathf.SmoothStep(0f, 1f, progress) * lengths[64];
                for (var i = 1; i < lengths.Length; i++)
                    if (distance <= lengths[i]) return Bezier((i - 1 +
                        Mathf.InverseLerp(lengths[i - 1], lengths[i], distance)) / 64f);
                return path[3];
            }
            private Vector3 Bezier(float t)
            {
                var u = 1f - t;
                return u*u*u*path[0] + 3f*u*u*t*path[1] + 3f*u*t*t*path[2] + t*t*t*path[3];
            }
        }

        [SerializeField] private string sourceRevision;
        [SerializeField] private Side right, left;
        public string SourceRevision => sourceRevision;
        public Quaternion ForwardReference => transform.rotation;
        public Side Get(CoverSide side) => side == CoverSide.Right ? right : left;
        public void Configure(string revision, Side rightSide, Side leftSide)
        { sourceRevision = revision; right = rightSide; left = leftSide; }

        public void Validate(List<string> failures)
        {
            if (string.IsNullOrEmpty(sourceRevision)) failures.Add("Cover rig source revision missing.");
            foreach (var side in new[] { CoverSide.Right, CoverSide.Left })
            {
                var data = Get(side);
                if (data == null || data.side != side || data.playerAnchor == null ||
                    data.hiddenPose == null || data.exposedPose == null || data.path == null || data.path.Length != 4)
                { failures.Add("Missing cover anchors/path for " + side); continue; }
                if (!data.playerAnchor.IsChildOf(transform) || !data.hiddenPose.IsChildOf(transform) ||
                    !data.exposedPose.IsChildOf(transform)) failures.Add("Cover anchors must stay in the static rig: " + side);
                foreach (var point in data.path)
                    if (float.IsNaN(point.sqrMagnitude) || float.IsInfinity(point.sqrMagnitude))
                        failures.Add("Nonfinite cover path: " + side);
                var other = Get(side == CoverSide.Right ? CoverSide.Left : CoverSide.Right);
                if (Vector3.Distance(data.path[0], data.playerAnchor.position) > .02f ||
                    other?.playerAnchor != null && Vector3.Distance(data.path[3], other.playerAnchor.position) > .02f)
                    failures.Add("Cover path endpoints disconnected: " + side);
                if (Quaternion.Angle(data.hiddenPose.rotation, ForwardReference) > .5f ||
                    Quaternion.Angle(data.playerAnchor.rotation, ForwardReference) > .5f)
                    failures.Add("Hidden camera and player must face the fixed corridor heading: " + side);
                data.Position(0f); // Warm arc-length storage outside the frame loop.
            }
        }
    }
}
