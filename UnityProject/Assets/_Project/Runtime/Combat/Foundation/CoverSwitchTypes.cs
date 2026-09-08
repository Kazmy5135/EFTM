namespace EFTM.Combat.Foundation
{
    public enum CoverSide { Right, Left }
    public enum CoverSwitchPhase { Idle = 0, Traversing = 2, Landing = 3 }
    public enum ObservationSource { FakePeek, CoverSwitch }

    public readonly struct PreAimSolution
    {
        public PreAimSolution(CoverSide side, int intelRevision, float yaw, float pitch)
        { IsValid = true; Side = side; IntelRevision = intelRevision; Yaw = yaw; Pitch = pitch; }
        public readonly bool IsValid;
        public readonly CoverSide Side;
        public readonly int IntelRevision;
        public readonly float Yaw, Pitch;
    }

    public readonly struct CoverSwitchSnapshot
    {
        public CoverSwitchSnapshot(CoverSide current, CoverSide source, CoverSide target,
            CoverSwitchPhase phase, float elapsed, float moveProgress,
            int actionId, bool awaiting, bool suspended, bool observed)
        {
            CurrentSide = current; SourceSide = source; TargetSide = target; Phase = phase;
            ElapsedSeconds = elapsed; MoveProgress = moveProgress; ActionId = actionId;
            AwaitingArrivalConfirmation = awaiting; Suspended = suspended; ObservedThisAction = observed;
        }
        public readonly CoverSide CurrentSide, SourceSide, TargetSide;
        public readonly CoverSwitchPhase Phase;
        public readonly float ElapsedSeconds, MoveProgress;
        public readonly int ActionId;
        public readonly bool AwaitingArrivalConfirmation, Suspended, ObservedThisAction;
        public bool IsSwitching => Phase != CoverSwitchPhase.Idle;
        public bool InputLocked => IsSwitching || Suspended || AwaitingArrivalConfirmation;
        public bool CanObserve => !Suspended &&
            (Phase == CoverSwitchPhase.Traversing || Phase == CoverSwitchPhase.Landing);
    }
}
