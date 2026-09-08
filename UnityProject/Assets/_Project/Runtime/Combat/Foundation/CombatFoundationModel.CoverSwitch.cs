using System;

namespace EFTM.Combat.Foundation
{
    public sealed partial class CombatFoundationModel
    {
        private CoverSide currentSide = CoverSide.Right;
        private CoverSide sourceSide = CoverSide.Right;
        private CoverSide targetSide = CoverSide.Left;
        private CoverSwitchPhase switchPhase;
        private float switchElapsed;
        private bool awaitingArrival, suspended;
        private int actionId, intelRevision;

        private CoverSwitchSnapshot SwitchSnapshot => new CoverSwitchSnapshot(currentSide, sourceSide, targetSide,
            switchPhase, switchElapsed, Clamp(switchElapsed / config.CoverMoveSeconds, 0f, 1f),
            actionId, awaitingArrival, suspended, observedThisPeek);

        public void SetSuspended(bool value)
        {
            suspended = value;
            if (value) ReleaseFire();
        }

        private void BeginCoverSwitch()
        {
            if (!Snapshot.CanSwitchCover) return;
            actionId++;
            sourceSide = currentSide;
            targetSide = currentSide == CoverSide.Right ? CoverSide.Left : CoverSide.Right;
            switchElapsed = 0f;
            awaitingArrival = false;
            observedThisPeek = false;
            ReleaseFire();
            if (ghostVisible)
            {
                ghostVisible = false;
                pendingEvents.Add(new CombatEvent(CombatEventType.IntelGhostHidden, lastSeenPosition));
            }
            switchPhase = CoverSwitchPhase.Traversing;
            pendingEvents.Add(new CombatEvent(CombatEventType.CoverSwitchStarted, actionId));
        }

        private void AdvanceCoverSwitch(float delta)
        {
            if (awaitingArrival) return;
            var total = config.CoverMoveSeconds;
            switchElapsed = Math.Min(total, switchElapsed + delta);
            // Avoid an additional frame at exact 30/60 Hz boundaries due to float accumulation.
            if (total - switchElapsed < 0.000001f) switchElapsed = total;
            var next = switchElapsed < total - config.CoverLandingSeconds ? CoverSwitchPhase.Traversing : CoverSwitchPhase.Landing;
            if (next != switchPhase)
            {
                switchPhase = next;
                pendingEvents.Add(new CombatEvent(CombatEventType.CoverSwitchPhaseChanged, (int)next));
            }
            awaitingArrival = switchElapsed >= total;
        }

        // Scene adapter calls only after applying and checking the final camera/player pose.
        public bool ConfirmCoverArrival(int confirmedActionId)
        {
            if (suspended || !awaitingArrival || switchPhase != CoverSwitchPhase.Landing || confirmedActionId != actionId)
                return false;
            currentSide = targetSide;
            awaitingArrival = false;
            switchPhase = CoverSwitchPhase.Idle;
            pendingEvents.Add(new CombatEvent(CombatEventType.CoverSwitchArrived, actionId));
            ResolveEnemyRelocation();
            ghostVisible = observedThisPeek && hasIntel;
            observedThisPeek = false;
            if (ghostVisible) pendingEvents.Add(new CombatEvent(CombatEventType.IntelGhostShown, lastSeenPosition));
            return true;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
