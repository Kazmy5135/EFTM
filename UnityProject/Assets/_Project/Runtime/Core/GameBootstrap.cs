using System;
using UnityEngine;

namespace EFTM.Core
{
    /// <summary>
    /// Persistent composition root created before the first scene loads.
    /// Derive from this class only when a project-specific composition root is needed.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class GameBootstrap : MonoBehaviour
    {
        private static GameBootstrap instance;
        private ServiceRegistry services;

        public static GameBootstrap Instance => instance;

        public ServiceRegistry Services => services;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateDefaultInstance()
        {
            if (FindObjectOfType<GameBootstrap>() != null)
            {
                return;
            }

            var bootstrapObject = new GameObject("[GameBootstrap]");
            bootstrapObject.AddComponent<GameBootstrap>();
        }

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            services = new ServiceRegistry();
            ConfigureServices(services);
            services.InitializeAll();
        }

        protected virtual void ConfigureServices(ServiceRegistry registry)
        {
        }

        protected virtual void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            try
            {
                services?.ShutdownAll();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                services = null;
                instance = null;
            }
        }
    }
}
