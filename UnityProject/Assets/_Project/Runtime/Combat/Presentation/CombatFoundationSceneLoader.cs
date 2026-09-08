using UnityEngine;
using UnityEngine.SceneManagement;

namespace EFTM.Combat.Presentation
{
    public static class CombatFoundationSceneLoader
    {
        private const int CombatSceneBuildIndex = 1;
        private static bool loadRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => loadRequested = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LoadCombatSceneAfterBootstrap()
        {
            if (loadRequested || SceneManager.GetActiveScene().buildIndex != 0)
            {
                return;
            }

            if (SceneManager.sceneCountInBuildSettings <= CombatSceneBuildIndex)
            {
                Debug.LogError("[EFTM] CombatFoundationV1 must be the second enabled Build Settings scene.");
                return;
            }

            loadRequested = true;
            SceneManager.LoadSceneAsync(CombatSceneBuildIndex, LoadSceneMode.Single);
        }
    }
}
