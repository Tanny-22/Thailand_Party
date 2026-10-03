using UnityEngine;

namespace PartyGame.MiniGames
{
    [CreateAssetMenu(menuName = "Party Game/Mini Game Definition", fileName = "MiniGame_")]
    public sealed class MiniGameDefinition : ScriptableObject
    {
        [SerializeField] string id = "template";
        [SerializeField] string displayName = "Template MiniGame";
        [SerializeField, TextArea] string description;
        [SerializeField] Sprite thumbnail;
        [SerializeField] string sceneName = "MiniGame_Template";
        [SerializeField] GameObject gameplayPrefab;
        [SerializeField] bool enabledForSelection = true;
        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Thumbnail => thumbnail;
        public string SceneName => sceneName;
        public GameObject GameplayPrefab => gameplayPrefab;
        public bool EnabledForSelection => enabledForSelection;
    }
}
