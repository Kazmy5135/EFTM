using System;
using EFTM.Combat.Foundation;

namespace EFTM.Combat.Input
{
    public sealed class CombatInputController
    {
        private const int NoPointer = int.MinValue;

        private readonly Action<CombatCommand> dispatch;
        private readonly Func<CombatSnapshot> getSnapshot;
        private readonly float yawDegreesPerReferenceWidth;
        private readonly float pitchDegreesPerReferenceHeight;

        private int fakePointerId = NoPointer;
        private int firePointerId = NoPointer;
        private int aimPointerId = NoPointer;

        public CombatInputController(
            Action<CombatCommand> dispatch,
            Func<CombatSnapshot> getSnapshot,
            float yawDegreesPerReferenceWidth,
            float pitchDegreesPerReferenceHeight)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.getSnapshot = getSnapshot ?? throw new ArgumentNullException(nameof(getSnapshot));

            if (yawDegreesPerReferenceWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(yawDegreesPerReferenceWidth));
            }

            if (pitchDegreesPerReferenceHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pitchDegreesPerReferenceHeight));
            }

            this.yawDegreesPerReferenceWidth = yawDegreesPerReferenceWidth;
            this.pitchDegreesPerReferenceHeight = pitchDegreesPerReferenceHeight;
        }

        public bool HasFakePointer => fakePointerId != NoPointer;

        public bool HasFirePointer => firePointerId != NoPointer;

        public bool HasAimPointer => aimPointerId != NoPointer;

        public bool TryToggleTrueAim(int pointerId)
        {
            var snapshot = getSnapshot();
            var canStart = snapshot.Mode == PeekMode.None && snapshot.Phase == PeekPhase.Hidden;
            var canReturn = snapshot.Mode == PeekMode.TrueAim && snapshot.Phase != PeekPhase.Returning;
            if (!canStart && !canReturn)
            {
                return false;
            }

            dispatch(new CombatCommand(CombatCommandType.ToggleTrueAim, pointerId));
            return true;
        }

        public bool TryBeginFakePeek(int pointerId)
        {
            if (fakePointerId != NoPointer)
            {
                return false;
            }

            var snapshot = getSnapshot();
            var canStart = snapshot.Mode == PeekMode.None && snapshot.Phase == PeekPhase.Hidden;
            var canReverse = snapshot.Mode == PeekMode.Fake && snapshot.Phase == PeekPhase.Returning;
            if (!canStart && !canReverse)
            {
                return false;
            }

            fakePointerId = pointerId;
            dispatch(new CombatCommand(CombatCommandType.FakePeekPressed, pointerId));
            return true;
        }

        public bool TryEndFakePeek(int pointerId)
        {
            if (fakePointerId != pointerId)
            {
                return false;
            }

            fakePointerId = NoPointer;
            dispatch(new CombatCommand(CombatCommandType.FakePeekReleased, pointerId));
            return true;
        }

        public bool TryBeginFire(int pointerId)
        {
            if (firePointerId != NoPointer)
            {
                return false;
            }

            var snapshot = getSnapshot();
            if (snapshot.Mode != PeekMode.TrueAim ||
                snapshot.Phase == PeekPhase.Hidden ||
                snapshot.Phase == PeekPhase.Returning)
            {
                return false;
            }

            firePointerId = pointerId;
            dispatch(new CombatCommand(CombatCommandType.FirePressed, pointerId));
            return true;
        }

        public bool TryEndFire(int pointerId)
        {
            if (firePointerId != pointerId)
            {
                return false;
            }

            firePointerId = NoPointer;
            dispatch(new CombatCommand(CombatCommandType.FireReleased, pointerId));
            return true;
        }

        public bool TryBeginAim(int pointerId)
        {
            if (aimPointerId != NoPointer)
            {
                return false;
            }

            var snapshot = getSnapshot();
            if (snapshot.Mode != PeekMode.TrueAim ||
                snapshot.Phase == PeekPhase.Hidden ||
                snapshot.Phase == PeekPhase.Returning)
            {
                return false;
            }

            aimPointerId = pointerId;
            return true;
        }

        public bool TryEndAim(int pointerId)
        {
            if (aimPointerId != pointerId)
            {
                return false;
            }

            aimPointerId = NoPointer;
            return true;
        }

        public bool TryMoveAim(
            int pointerId,
            float deltaX,
            float deltaY,
            float viewportWidth,
            float viewportHeight)
        {
            if (pointerId != aimPointerId && pointerId != firePointerId)
            {
                return false;
            }

            if (viewportWidth <= 0f || viewportHeight <= 0f)
            {
                return false;
            }

            var yaw = -deltaX / viewportWidth * yawDegreesPerReferenceWidth;
            var pitch = -deltaY / viewportHeight * pitchDegreesPerReferenceHeight;
            dispatch(new CombatCommand(CombatCommandType.AimDelta, pointerId, yaw, pitch));
            return true;
        }

        public bool CancelPointer(int pointerId)
        {
            var handled = false;
            handled |= TryEndFakePeek(pointerId);
            handled |= TryEndFire(pointerId);
            handled |= TryEndAim(pointerId);
            return handled;
        }

        public void ReleaseAll()
        {
            if (fakePointerId != NoPointer)
            {
                var pointerId = fakePointerId;
                fakePointerId = NoPointer;
                dispatch(new CombatCommand(CombatCommandType.FakePeekReleased, pointerId));
            }

            if (firePointerId != NoPointer)
            {
                var pointerId = firePointerId;
                firePointerId = NoPointer;
                dispatch(new CombatCommand(CombatCommandType.FireReleased, pointerId));
            }

            aimPointerId = NoPointer;

            var snapshot = getSnapshot();
            if (snapshot.Mode == PeekMode.TrueAim && snapshot.Phase != PeekPhase.Returning)
            {
                dispatch(new CombatCommand(CombatCommandType.ToggleTrueAim));
            }
        }
    }
}
