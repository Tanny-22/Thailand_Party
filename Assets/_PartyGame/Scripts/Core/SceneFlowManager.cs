using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PartyGame.Core
{
    public sealed class SceneFlowManager : PersistentService<SceneFlowManager>
    {
        public async Task LoadLocalAsync(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return;
            var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (operation != null && !operation.isDone) await Task.Yield();
        }

        public bool LoadNetworked(string sceneName)
        {
            var network = NetworkManager.Singleton;
            if (!network || !network.IsServer) return false;
            return network.SceneManager.LoadScene(sceneName, LoadSceneMode.Single) == SceneEventProgressStatus.Started;
        }
    }
}
