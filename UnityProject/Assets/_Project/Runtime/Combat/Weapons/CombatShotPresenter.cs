using System.Collections.Generic;
using EFTM.Combat.Camera;
using EFTM.Combat.Foundation;
using UnityEngine;

namespace EFTM.Combat.Weapons
{
    [DisallowMultipleComponent]
    public sealed class CombatShotPresenter : MonoBehaviour
    {
        [SerializeField] private LineRenderer[] tracers;
        [SerializeField] private Transform[] impacts;
        [SerializeField] private AudioSource[] audioSources;
        [SerializeField] private LayerMask shotMask;
        private readonly float[] remaining = new float[4];
        private AudioClip shotClip;
        private int cursor;
        public int ShotCount { get; private set; }
        public int TargetHitCount { get; private set; }
        public Vector3 LastHitPoint { get; private set; }
        public bool LastHitTarget { get; private set; }
        public float HitFeedbackRemaining { get; private set; }
        public int PoolCapacity => tracers == null ? 0 : tracers.Length;
        public bool IsPrewarmed => shotClip != null;

        public void Configure(LineRenderer[] lines, Transform[] markers, AudioSource[] sources, int mask)
        { tracers = lines; impacts = markers; audioSources = sources; shotMask = mask; }

        public void Validate(List<string> failures)
        {
            if (tracers == null || impacts == null || audioSources == null ||
                tracers.Length != 4 || impacts.Length != 4 || audioSources.Length != 4)
            { failures.Add("Four pre-created tracer, impact and audio slots required."); return; }
            for (var i = 0; i < 4; i++)
                if (tracers[i] == null || tracers[i].sharedMaterial == null || impacts[i] == null || audioSources[i] == null)
                    failures.Add("Shot pool slot " + i + " is incomplete.");
            if (shotMask.value == 0 || shotMask.value != LayerMask.GetMask("CombatOccluder", "CombatTarget"))
                failures.Add("Shot mask must contain occluders and targets only.");
        }

        public void Prewarm()
        {
            if (shotClip == null)
            {
                // Small prototype shot sound; generated once, never in the firing path.
                const int rate = 22050;
                var samples = new float[2205];
                uint noise = 0x9e3779b9;
                for (var i = 0; i < samples.Length; i++)
                {
                    noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                    var t = i / (float)rate;
                    samples[i] = ((noise & 65535) / 32767.5f - 1f) * Mathf.Exp(-t * 65f) * .35f;
                }
                shotClip = AudioClip.Create("V1 pooled shot", samples.Length, 1, rate, false);
                shotClip.SetData(samples, 0);
            }
            for (var i = 0; i < 4; i++)
            {
                tracers[i].positionCount = 2;
                audioSources[i].clip = shotClip;
                audioSources[i].playOnAwake = false;
                audioSources[i].spatialBlend = 0f;
                audioSources[i].volume = .35f;
            }
            ShotCount = TargetHitCount = cursor = 0;
            ResetFeedback();
        }

        public void Fire(CombatEvent shot, PeekCameraPresenter cameraRig, bool targetAvailable)
        {
            if (!targetAvailable || !IsPrewarmed) return;
            var ray = cameraRig.ShotRay(shot.ShotYaw, shot.ShotPitch);
            var hitSomething = Physics.Raycast(ray, out var hit, 60f, shotMask, QueryTriggerInteraction.Ignore);
            LastHitPoint = hitSomething ? hit.point : ray.GetPoint(60f);
            LastHitTarget = hitSomething && hit.collider.gameObject.layer == LayerMask.NameToLayer("CombatTarget");
            ShotCount++;
            if (LastHitTarget) TargetHitCount++;
            HitFeedbackRemaining = .12f;
            var index = cursor++ % 4;
            remaining[index] = .075f;
            // Visual starts slightly below the camera; authoritative ray remains screen center.
            tracers[index].SetPosition(0, ray.origin + cameraRig.ViewTransform.right * .06f - cameraRig.ViewTransform.up * .09f);
            tracers[index].SetPosition(1, LastHitPoint);
            tracers[index].enabled = true;
            impacts[index].position = LastHitPoint - ray.direction * .015f;
            impacts[index].gameObject.SetActive(hitSomething);
            audioSources[index].Play();
        }

        public void Tick(float delta)
        {
            HitFeedbackRemaining = Mathf.Max(0f, HitFeedbackRemaining - delta);
            for (var i = 0; i < 4; i++)
            {
                if (remaining[i] <= 0f) continue;
                remaining[i] -= delta;
                if (remaining[i] > 0f) continue;
                tracers[i].enabled = false;
                impacts[i].gameObject.SetActive(false);
            }
        }

        public void ResetFeedback()
        {
            HitFeedbackRemaining = 0;
            for (var i = 0; i < PoolCapacity; i++)
            {
                remaining[i] = 0f;
                if (tracers[i] != null) tracers[i].enabled = false;
                if (impacts != null && i < impacts.Length && impacts[i] != null) impacts[i].gameObject.SetActive(false);
                if (audioSources != null && i < audioSources.Length && audioSources[i] != null) audioSources[i].Stop();
            }
        }

        private void OnDisable() => ResetFeedback();
        private void OnDestroy() { if (shotClip != null) Destroy(shotClip); }
    }
}
