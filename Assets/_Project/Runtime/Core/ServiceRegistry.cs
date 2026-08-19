using System;
using System.Collections.Generic;

namespace EFTM.Core
{
    /// <summary>
    /// Small composition-root registry. Registration order is initialization order;
    /// shutdown runs in reverse order.
    /// </summary>
    public sealed class ServiceRegistry
    {
        private readonly Dictionary<Type, IGameService> services =
            new Dictionary<Type, IGameService>();

        private readonly List<IGameService> orderedServices = new List<IGameService>();
        private int initializedCount;

        public bool IsInitialized { get; private set; }

        public void Register<TService>(TService service) where TService : class, IGameService
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (IsInitialized)
            {
                throw new InvalidOperationException("Services cannot be registered after initialization.");
            }

            var serviceType = typeof(TService);
            if (services.ContainsKey(serviceType))
            {
                throw new InvalidOperationException($"A service is already registered for {serviceType.FullName}.");
            }

            services.Add(serviceType, service);
            orderedServices.Add(service);
        }

        public TService Get<TService>() where TService : class, IGameService
        {
            if (!services.TryGetValue(typeof(TService), out var service))
            {
                throw new KeyNotFoundException($"No service is registered for {typeof(TService).FullName}.");
            }

            return (TService)service;
        }

        public bool TryGet<TService>(out TService service) where TService : class, IGameService
        {
            if (services.TryGetValue(typeof(TService), out var registeredService))
            {
                service = (TService)registeredService;
                return true;
            }

            service = null;
            return false;
        }

        public void InitializeAll()
        {
            if (IsInitialized)
            {
                return;
            }

            try
            {
                for (; initializedCount < orderedServices.Count; initializedCount++)
                {
                    orderedServices[initializedCount].Initialize();
                }

                IsInitialized = true;
            }
            catch
            {
                ShutdownInitializedServices();
                throw;
            }
        }

        public void ShutdownAll()
        {
            ShutdownInitializedServices();
            IsInitialized = false;
        }

        private void ShutdownInitializedServices()
        {
            List<Exception> failures = null;

            while (initializedCount > 0)
            {
                initializedCount--;
                try
                {
                    orderedServices[initializedCount].Shutdown();
                }
                catch (Exception exception)
                {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }

            IsInitialized = false;

            if (failures != null)
            {
                throw new AggregateException("One or more services failed to shut down.", failures);
            }
        }
    }
}
