using PartyGame.Core;
using UnityEngine;

namespace PartyGame.Bootstrap
{
    public sealed class PartyGameBootstrap : MonoBehaviour
    {
        [SerializeField] GameConfig gameConfig;
        [SerializeField] bool loadMainMenuOnStart = true;
        static bool initialized;
        async void Start()
        {
            if (initialized) return;
            initialized = true;
            DontDestroyOnLoad(transform.root.gameObject);
            if (loadMainMenuOnStart && SceneFlowManager.Instance)
                await SceneFlowManager.Instance.LoadLocalAsync(gameConfig ? gameConfig.MainMenuScene : "01_MainMenu");
        }
    }
}
