namespace EFTM.Core
{
    /// <summary>
    /// Defines a service with an explicit application lifetime.
    /// </summary>
    public interface IGameService
    {
        void Initialize();

        void Shutdown();
    }
}
