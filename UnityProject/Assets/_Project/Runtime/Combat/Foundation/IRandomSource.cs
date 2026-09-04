namespace EFTM.Combat.Foundation
{
    /// <summary>
    /// Injectable random source. Runtime adapters may wrap UnityEngine.Random while
    /// tests provide a fixed sequence.
    /// </summary>
    public interface IRandomSource
    {
        float Next01();
    }
}
